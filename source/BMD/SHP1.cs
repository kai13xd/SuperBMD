using SuperBMD.Geometry;
using SuperBMD.Util;
using SuperBMD.Rigging;
using SuperBMD.source.Geometry.Enums;


namespace SuperBMD.BMD
{
    public class SHP1
    {
        public List<Shape> Shapes { get; private set; }
        public List<int> RemapTable { get; private set; }

        private SHP1()
        {
            Shapes = new List<Shape>();
            RemapTable = new List<int>();
        }

        public SHP1(ref EndianBinaryReader reader, BMDInfo modelstats)
        {
            Shapes = new List<Shape>();
            RemapTable = new List<int>();

            var offset = reader.Position;
            if (reader.ReadString(4) != "SHP1")
                throw new Exception("SuperBMD is lost! SHP1 header is malformed!");
            int shp1Size = reader.ReadInt();
            int entryCount = reader.ReadShort();
            reader.Skip(2);
            modelstats.SHP1Size = shp1Size;

            int shapeHeaderDataOffset = reader.ReadInt();
            int shapeRemapTableOffset = reader.ReadInt();
            int unusedOffset = reader.ReadInt();
            int attributeDataOffset = reader.ReadInt();
            int matrixIndexDataOffset = reader.ReadInt();
            int primitiveDataOffset = reader.ReadInt();
            int matrixDataOffset = reader.ReadInt();
            int PacketInfoDataOffset = reader.ReadInt();

            reader.Seek(offset + shapeRemapTableOffset);

            // Remap table
            for (int i = 0; i < entryCount; i++)
                RemapTable.Add(reader.ReadShort());

            int highestIndex = J3DUtility.GetHighestValue(RemapTable);

            // Packet data
            List<Tuple<int, int>> packetData = new(); // <packet size, packet offset>
            int packetDataCount = (shp1Size - PacketInfoDataOffset) / 8;
            reader.Seek(PacketInfoDataOffset + offset);

            for (int i = 0; i < packetDataCount; i++)
            {
                packetData.Add(new Tuple<int, int>(reader.ReadInt(), reader.ReadInt()));
            }

            // Matrix data
            List<Tuple<int, int>> matrixData = new(); // <index count, start index>
            List<int[]> matrixIndices = new();

            int matrixDataCount = (PacketInfoDataOffset - matrixDataOffset) / 8;
            reader.Seek(matrixDataOffset + offset);

            for (int i = 0; i < matrixDataCount; i++)
            {
                reader.Skip(2);
                matrixData.Add(new Tuple<int, int>(reader.ReadShort(), reader.ReadInt()));
            }

            for (int i = 0; i < matrixDataCount; i++)
            {
                reader.Seek(offset + matrixIndexDataOffset + (matrixData[i].Item2 * 2));
                int[] indices = new int[matrixData[i].Item1];

                for (int j = 0; j < matrixData[i].Item1; j++)
                    indices[j] = reader.ReadShort();

                matrixIndices.Add(indices);
            }

            // Shape data
            List<Shape> tempShapeList = new();
            reader.Seek(offset + shapeHeaderDataOffset);

            for (int i = 0; i < highestIndex + 1; i++)
            {
                MatrixType matrixType = (MatrixType)reader.ReadByte();
                reader.Skip();

                int packetCount = reader.ReadShort();
                int shapeAttributeOffset = reader.ReadShort();
                int shapeMatrixDataIndex = reader.ReadShort();
                int firstPacketIndex = reader.ReadShort();
                reader.Skip(2);

                BoundingSphere shapeVol = new BoundingSphere(ref reader);

                int curOffset = reader.Position;

                ShapeVertexDescriptor descriptor = new ShapeVertexDescriptor(ref reader, offset + attributeDataOffset + shapeAttributeOffset);

                List<Packet> shapePackets = new();
                for (int j = 0; j < packetCount; j++)
                {
                    int packetSize = packetData[j + firstPacketIndex].Item1;
                    int packetOffset = packetData[j + firstPacketIndex].Item2;

                    Packet pack;
                    if (j + firstPacketIndex < matrixIndices.Count)
                    {
                        pack = new Packet(packetSize, packetOffset + primitiveDataOffset + offset, matrixIndices[j + firstPacketIndex]);
                    }
                    else
                    {
                        //Fixes the exporting of older models made with tools like obj2bdl
                        pack = new Packet(packetSize, packetOffset + primitiveDataOffset + offset, matrixIndices[0]);
                    }
                    pack.ReadPrimitives(ref reader, descriptor);

                    shapePackets.Add(pack);
                }

                tempShapeList.Add(new Shape(descriptor, shapeVol, shapePackets, matrixType));

                reader.Seek(curOffset);
            }

            for (int i = 0; i < entryCount; i++)
                Shapes.Add(tempShapeList[RemapTable[i]]);

            reader.Seek(offset + shp1Size);
            reader.Align(32);
        }

        private SHP1(Assimp.Scene scene, VertexData vertData, Dictionary<string, int> boneNames, EVP1 envelopes, DRW1 partialWeight,
            string tristripMode = "static", bool degenerateTriangles = false)
        {
            Shapes = new List<Shape>();
            RemapTable = new List<int>();

            foreach (Assimp.Mesh mesh in scene.Meshes)
            {
                Console.Write(mesh.Name + ": ");
                Shape meshShape;

                /*if (mesh.Name.Contains("Bill0")) {
                    meshShape = new Shape(0); // Matrix Type 0, unknown
                }*/
                if (mesh.Name.Contains("BillXY"))
                {
                    meshShape = new Shape(MatrixType.BillboardXY); // Matrix Type 1, XY Billboard
                    Console.Write("Billboarding on the X & Y axis");
                }
                else if (mesh.Name.Contains("BillX"))
                {
                    meshShape = new Shape(MatrixType.BillboardX); // Matrix Type 2, X Billboard, i.e. the X axis is always turned towards camera
                    Console.Write("Billboarding on the X axis");
                }
                else
                {
                    meshShape = new Shape(); // Matrix Type 3, normal
                    Console.Write("Normal Mesh");
                }
                meshShape.SetDescriptorAttributes(mesh, boneNames.Count);

                if (boneNames.Count > 1)
                    meshShape.ProcessVerticesWithWeights(mesh, vertData, boneNames, envelopes, partialWeight, tristripMode == "all", degenerateTriangles);
                else
                {
                    meshShape.ProcessVerticesWithoutWeights(mesh, vertData, degenerateTriangles);
                    partialWeight.WeightTypeCheck.Add(false);
                    partialWeight.Indices.Add(0);
                }

                Shapes.Add(meshShape);

            }
        }

        public static SHP1 Create(Assimp.Scene scene, Dictionary<string, int> boneNames, VertexData vertData, EVP1 evp1, DRW1 drw1,
            string tristrip_mode = "static", bool degenerateTriangles = false)
        {
            SHP1 shp1 = new SHP1(scene, vertData, boneNames, evp1, drw1, tristrip_mode, degenerateTriangles);

            return shp1;
        }

        public void SetVertexWeights(EVP1 envelopes, DRW1 drawList)
        {
            for (int i = 0; i < Shapes.Count; i++)
            {
                for (int j = 0; j < Shapes[i].Packets.Count; j++)
                {
                    foreach (Primitive prim in Shapes[i].Packets[j].Primitives)
                    {
                        foreach (Vertex vert in prim.Vertices)
                        {
                            if (Shapes[i].Descriptor.CheckAttribute(GXVertexAttribute.PositionMatrixIdx))
                            {
                                int drw1Index = Shapes[i].Packets[j].MatrixIndices[(int)vert.PositionMatrixIDxIndex];
                                int curPacketIndex = j;
                                while (drw1Index == -1)
                                {
                                    curPacketIndex--;
                                    drw1Index = Shapes[i].Packets[curPacketIndex].MatrixIndices[(int)vert.PositionMatrixIDxIndex];
                                }

                                if (drawList.WeightTypeCheck[(int)drw1Index])
                                {
                                    int evp1Index = drawList.Indices[(int)drw1Index];
                                    vert.SetWeight(envelopes.Weights[evp1Index]);
                                }
                                else
                                {
                                    Weight vertWeight = new Weight();
                                    vertWeight.AddWeight(1.0f, drawList.Indices[(int)drw1Index]);
                                    vert.SetWeight(vertWeight);
                                }
                            }
                            else
                            {
                                Weight vertWeight = new Weight();
                                vertWeight.AddWeight(1.0f, drawList.Indices[Shapes[i].Packets[j].MatrixIndices[0]]);
                                vert.SetWeight(vertWeight);
                            }
                        }
                    }
                }
            }
        }

        public void FillScene(Assimp.Scene scene, VertexData vertData, List<Rigging.Bone> flatSkeleton, List<Matrix4> inverseBindMatrices)
        {
            for (int i = 0; i < Shapes.Count; i++)
            {


                int vertexID = 0;
                Shape curShape = Shapes[i];

                Console.Write("Mesh " + i + ": ");
                string meshname = $"mesh_{i}";

                switch (curShape.MatrixType)
                {
                    case MatrixType.BillboardX:
                        meshname += "_BillX";
                        Console.Write("X Billboarding Detected! ");
                        break;
                    case MatrixType.BillboardXY:
                        meshname += "_BillXY";
                        Console.Write("XY Billboarding Detected! ");
                        break;
                    default:
                        break;
                }

                Assimp.Mesh mesh = new(meshname, Assimp.PrimitiveType.Triangle);
                mesh.MaterialIndex = i;

                foreach (Packet pack in curShape.Packets)
                {
                    foreach (Primitive prim in pack.Primitives)
                    {
                        List<Vertex> triVertices = J3DUtility.PrimitiveToTriangles(prim);

                        for (int triIndex = 0; triIndex < triVertices.Count; triIndex += 3)
                        {
                            Assimp.Face newFace = new(new int[] { vertexID + 2, vertexID + 1, vertexID });
                            mesh.Faces.Add(newFace);

                            for (int triVertIndex = 0; triVertIndex < 3; triVertIndex++)
                            {
                                Vertex vert = triVertices[triIndex + triVertIndex];

                                for (int j = 0; j < vert.VertexWeight.WeightCount; j++)
                                {
                                    Rigging.Bone curWeightBone = flatSkeleton[vert.VertexWeight.BoneIndices[j]];

                                    int assBoneIndex = mesh.Bones.FindIndex(x => x.Name == curWeightBone.Name);

                                    if (assBoneIndex == -1)
                                    {
                                        Assimp.Bone newBone = new Assimp.Bone();
                                        newBone.Name = curWeightBone.Name;
                                        newBone.OffsetMatrix = curWeightBone.InverseBindMatrix.ToMatrix4x4();
                                        mesh.Bones.Add(newBone);
                                        assBoneIndex = mesh.Bones.IndexOf(newBone);
                                    }

                                    mesh.Bones[assBoneIndex].VertexWeights.Add(new Assimp.VertexWeight(vertexID, vert.VertexWeight.Weights[j]));
                                }

                                Vector3 posVec = vertData.Positions[(int)vert.GetAttributeIndex(GXVertexAttribute.Position)];
                                Vector4 openTKVec = new Vector4(posVec.X, posVec.Y, posVec.Z, 1);

                                Assimp.Vector3D vertVec = new(openTKVec.X, openTKVec.Y, openTKVec.Z);

                                if (vert.VertexWeight.WeightCount == 1)
                                {
                                    if (inverseBindMatrices.Count > vert.VertexWeight.BoneIndices[0])
                                    {
                                        Matrix4 test = inverseBindMatrices[vert.VertexWeight.BoneIndices[0]].Inverted();
                                        test.Transpose();
                                        Vector4 trans = Vector4.Transform(openTKVec, test.ExtractRotation());
                                        vertVec = new Assimp.Vector3D(trans.X, trans.Y, trans.Z);
                                    }
                                    else
                                    {
                                        Vector4 trans = Vector4.Transform(openTKVec, flatSkeleton[vert.VertexWeight.BoneIndices[0]].TransformationMatrix.ExtractRotation());
                                        vertVec = new Assimp.Vector3D(trans.X, trans.Y, trans.Z);
                                    }
                                }

                                mesh.Vertices.Add(vertVec);

                                if (curShape.Descriptor.CheckAttribute(GXVertexAttribute.Normal))
                                {
                                    Vector3 nrmVec = vertData.Normals[(int)vert.NormalIndex];
                                    Vector4 openTKNrm = new Vector4(nrmVec.X, nrmVec.Y, nrmVec.Z, 1);
                                    Assimp.Vector3D vertNrm = new(nrmVec.X, nrmVec.Y, nrmVec.Z);

                                    if (vert.VertexWeight.WeightCount == 1)
                                    {
                                        if (inverseBindMatrices.Count > vert.VertexWeight.BoneIndices[0])
                                        {
                                            Matrix4 test = inverseBindMatrices[vert.VertexWeight.BoneIndices[0]].Inverted();
                                            vertNrm = Vector3.TransformNormalInverse(nrmVec, test).ToVector3D();
                                        }
                                        else
                                        {
                                            Vector4 trans = Vector4.Transform(openTKNrm, flatSkeleton[vert.VertexWeight.BoneIndices[0]].TransformationMatrix.ExtractRotation());
                                            vertNrm = new Assimp.Vector3D(trans.X, trans.Y, trans.Z);
                                        }
                                    }

                                    mesh.Normals.Add(vertNrm);
                                }

                                if (curShape.Descriptor.CheckAttribute(GXVertexAttribute.Color0))
                                    mesh.VertexColorChannels[0].Add(vertData.Color_0[(int)vert.Color0Index].ToColor4D());

                                if (curShape.Descriptor.CheckAttribute(GXVertexAttribute.Color1))
                                    mesh.VertexColorChannels[1].Add(vertData.Color_1[(int)vert.Color1Index].ToColor4D());

                                for (int texCoordNum = 0; texCoordNum < 8; texCoordNum++)
                                {
                                    if (curShape.Descriptor.CheckAttribute(GXVertexAttribute.Tex0 + texCoordNum))
                                    {
                                        Assimp.Vector3D texCoord = new();
                                        switch (texCoordNum)
                                        {
                                            case 0:
                                                texCoord = vertData.TexCoord_0[(int)vert.TexCoord0Index].ToVector2D();
                                                break;
                                            case 1:
                                                texCoord = vertData.TexCoord_1[(int)vert.TexCoord1Index].ToVector2D();
                                                break;
                                            case 2:
                                                texCoord = vertData.TexCoord_2[(int)vert.TexCoord2Index].ToVector2D();
                                                break;
                                            case 3:
                                                texCoord = vertData.TexCoord_3[(int)vert.TexCoord3Index].ToVector2D();
                                                break;
                                            case 4:
                                                texCoord = vertData.TexCoord_4[(int)vert.TexCoord4Index].ToVector2D();
                                                break;
                                            case 5:
                                                texCoord = vertData.TexCoord_5[(int)vert.TexCoord5Index].ToVector2D();
                                                break;
                                            case 6:
                                                texCoord = vertData.TexCoord_6[(int)vert.TexCoord6Index].ToVector2D();
                                                break;
                                            case 7:
                                                texCoord = vertData.TexCoord_7[(int)vert.TexCoord7Index].ToVector2D();
                                                break;
                                        }

                                        mesh.TextureCoordinateChannels[texCoordNum].Add(texCoord);
                                    }
                                }

                                vertexID++;
                            }
                        }
                    }
                    Console.Write("...");
                }

                scene.Meshes.Add(mesh);
                Console.Write("✓");

            }
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            List<Tuple<ShapeVertexDescriptor, int>> descriptorOffsets; // Contains the offsets for each unique vertex descriptor
            List<Tuple<Packet, int>> packetMatrixOffsets; // Contains the offsets for each packet's matrix indices
            List<Tuple<int, int>> packetPrimitiveOffsets; // Contains the offsets for each packet's first primitive

            long start = writer.Position;

            writer.Write("SHP1");
            writer.Write(0); // Placeholder for section offset
            writer.Write((short)Shapes.Count);
            writer.Write((short)-1);

            writer.Write(44); // Offset to shape header data. Always 48

            for (int i = 0; i < 7; i++)
                writer.Write(0);

            foreach (Shape shp in Shapes)
            {
                shp.Write(ref writer);
            }

            // Remap table offset
            writer.Seek((int)(start + 16));
            writer.Write((int)(writer.FileLength - start));
            writer.Seek((int)(writer.FileLength));

            for (int i = 0; i < Shapes.Count; i++)
                writer.Write((short)i);

            writer.PadAlign(32);

            // Attribute descriptor data offset
            writer.Seek((int)(start + 24));
            writer.Write((int)(writer.FileLength - start));
            writer.Seek((int)(writer.FileLength));

            descriptorOffsets = WriteShapeAttributeDescriptors(ref writer);

            // Packet matrix index data offset
            writer.Seek((int)(start + 28));
            writer.Write((int)(writer.FileLength - start));
            writer.Seek((int)(writer.FileLength));

            packetMatrixOffsets = WritePacketMatrixIndices(ref writer);

            writer.PadAlign(32);

            // Primitive data offset
            writer.Seek((int)(start + 32));
            writer.Write((int)(writer.FileLength - start));
            writer.Seek((int)(writer.FileLength));

            packetPrimitiveOffsets = WritePrimitives(ref writer);

            // Packet matrix index metadata offset
            writer.Seek((int)(start + 36));
            writer.Write((int)(writer.FileLength - start));
            writer.Seek((int)(writer.FileLength));

            foreach (Tuple<Packet, int> tup in packetMatrixOffsets)
            {
                writer.Write((short)0); // ???
                writer.Write((short)tup.Item1.MatrixIndices.Count);
                writer.Write(tup.Item2);
            }

            // Packet primitive metadata offset
            writer.Seek((int)(start + 40));
            writer.Write((int)(writer.FileLength - start));
            writer.Seek((int)(writer.FileLength));

            foreach (Tuple<int, int> tup in packetPrimitiveOffsets)
            {
                writer.Write(tup.Item1);
                writer.Write(tup.Item2);
            }

            writer.PadAlign(32);

            writer.Seek((int)(start + 44));

            foreach (Shape shape in Shapes)
            {
                writer.Skip(4);
                writer.Write((short)descriptorOffsets.Find(x => x.Item1 == shape.Descriptor).Item2);
                writer.Write((short)packetMatrixOffsets.IndexOf(packetMatrixOffsets.Find(x => x.Item1 == shape.Packets[0])));
                writer.Write((short)packetMatrixOffsets.IndexOf(packetMatrixOffsets.Find(x => x.Item1 == shape.Packets[0])));
                writer.Skip(30);
            }

            writer.Seek((int)writer.FileLength);

            long end = writer.Position;
            long length = (end - start);

            writer.Seek((int)start + 4);
            writer.Write((int)length);
            writer.Seek((int)end);
        }

        private List<Tuple<ShapeVertexDescriptor, int>> WriteShapeAttributeDescriptors(ref EndianBinaryWriter writer)
        {
            List<Tuple<ShapeVertexDescriptor, int>> outList = new List<Tuple<ShapeVertexDescriptor, int>>();
            List<ShapeVertexDescriptor> written = new List<ShapeVertexDescriptor>();

            long start = writer.Position;

            foreach (Shape shape in Shapes)
            {
                if (written.Contains(shape.Descriptor))
                    continue;
                else
                {
                    outList.Add(new Tuple<ShapeVertexDescriptor, int>(shape.Descriptor, (int)(writer.Position - start)));
                    shape.Descriptor.Write(ref writer);
                    written.Add(shape.Descriptor);
                }
            }

            return outList;
        }

        private List<Tuple<Packet, int>> WritePacketMatrixIndices(ref EndianBinaryWriter writer)
        {
            List<Tuple<Packet, int>> outList = new List<Tuple<Packet, int>>();

            int indexOffset = 0;
            foreach (Shape shape in Shapes)
            {
                foreach (Packet pack in shape.Packets)
                {
                    outList.Add(new Tuple<Packet, int>(pack, indexOffset));

                    foreach (int integer in pack.MatrixIndices)
                    {
                        writer.Write((ushort)integer);
                        indexOffset++;
                    }
                }
            }

            return outList;
        }

        private List<Tuple<int, int>> WritePrimitives(ref EndianBinaryWriter writer)
        {
            List<Tuple<int, int>> outList = new List<Tuple<int, int>>();

            long start = writer.Position;

            foreach (Shape shape in Shapes)
            {
                foreach (Packet pack in shape.Packets)
                {
                    int offset = (int)(writer.Position - start);

                    foreach (Primitive prim in pack.Primitives)
                    {
                        prim.Write(ref writer, shape.Descriptor);
                    }

                    writer.PadAlignZero(32);

                    outList.Add(new Tuple<int, int>((int)((writer.Position - start) - offset), offset));
                }
            }

            return outList;
        }
        public void DumpJson(string path)
        {
            JsonSerializer serial = new JsonSerializer();
            serial.Formatting = Formatting.Indented;
            serial.Converters.Add(new StringEnumConverter());


            using (FileStream strm = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                StreamWriter writer = new StreamWriter(strm);
                writer.AutoFlush = true;
                serial.Serialize(writer, this);
            }
        }
    }
}