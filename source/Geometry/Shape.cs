﻿using System.Diagnostics;
using Assimp;
using BrawlLib.Modeling.Triangle_Converter;
using SuperBMD.BMD;
using SuperBMD.Rigging;
using SuperBMD.source.Geometry.Enums;
namespace SuperBMD.Geometry
{
    public class Shape
    {
        [JsonIgnore]
        public VertexData AttributeData { get; private set; }
        [JsonIgnore]
        public ShapeVertexDescriptor Descriptor { get; private set; }

        public MatrixType MatrixType { get; private set; }
        public BoundingSphere BoundingSphere { get; private set; }

        public List<Packet> Packets { get; private set; }
        [JsonConverter(typeof(Vector4Converter))]
        public Vector4[] PositionMatrices { get; set; }
        [JsonConverter(typeof(Vector4Converter))]
        public Vector4[] NormalMatrices { get; set; }

        // The maximum number of unique vertex weights that can be in a single shape packet without causing visual errors.
        private const int MaxMatricesPerPacket = 10;

        public Shape()
        {
            MatrixType = MatrixType.Normal;
            AttributeData = new VertexData();
            Descriptor = new ShapeVertexDescriptor();
            Packets = new List<Packet>();
            BoundingSphere = new BoundingSphere();

            PositionMatrices = new Vector4[64];
            NormalMatrices = new Vector4[32];
        }

        public Shape(MatrixType matrixType) : this()
        {
            MatrixType = matrixType;
        }

        public Shape(ShapeVertexDescriptor desc, BoundingSphere bounds, List<Packet> prims, MatrixType matrixType)
        {
            Descriptor = desc;
            BoundingSphere = bounds;
            Packets = prims;
            MatrixType = matrixType;
        }

        public void SetDescriptorAttributes(Mesh mesh, int jointCount)
        {
            int indexOffset = 0;

            if (jointCount > 1)
                Descriptor.SetAttribute(VertexAttribute.PositionMatrixIdx, VertexInputType.Direct, indexOffset++);

            if (mesh.HasVertices)
                Descriptor.SetAttribute(VertexAttribute.Position, VertexInputType.Index16, indexOffset++);
            if (mesh.HasNormals)
                Descriptor.SetAttribute(VertexAttribute.Normal, VertexInputType.Index16, indexOffset++);
            for (int i = 0; i < 2; i++)
            {
                if (mesh.HasVertexColors(i))
                    Descriptor.SetAttribute(VertexAttribute.ColorChannel0 + i, VertexInputType.Index16, indexOffset++);
            }

            for (int i = 0; i < 8; i++)
            {
                if (mesh.HasTextureCoords(i))
                    Descriptor.SetAttribute(VertexAttribute.TexCoord0 + i, VertexInputType.Index16, indexOffset++);
            }
            Console.Write(".");
        }

        uint[] MakeTriIndexList(Mesh mesh)
        {
            uint[] triindices = new uint[mesh.Faces.Count * 3];

            int i = 0;
            foreach (Face face in mesh.Faces)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (face.Indices.Count < 3)
                    {
                        throw new Exception(
                            string.Format(
                                "A face in mesh {1} has less than 3 vertices (loose vertex or edge). " +
                                "You need to remove it.", i, mesh.Name)
                            );
                    }
                    triindices[i * 3 + j] = (uint)face.Indices[2 - j];
                }

                i += 1;
            }
            return triindices;
        }

        public void ProcessVerticesWithoutWeights(Mesh mesh, VertexData vertData)
        {
            var packet = new Packet();


            List<VertexAttribute> activeAttribs = Descriptor.GetActiveAttributes();
            AttributeData.SetAttributesFromList(activeAttribs);

            //Console.WriteLine("Calculating triangle strips");

            uint[] triindices = MakeTriIndexList(mesh);
            TriStripper stripper = new TriStripper(triindices);
            List<PrimitiveBrawl> primlist = stripper.Strip();

            if (Arguments.ShouldDegenerateTriangles)
            {
                Console.WriteLine("Converting Triangle Lists into Triangle Strips with degenerate triangles.");

                for (int i = 0; i < primlist.Count; i++)
                {
                    PrimitiveBrawl primbrawl = primlist[i];

                    GXPrimitiveType primtype = (GXPrimitiveType)primbrawl.Type;

                    if (primtype == GXPrimitiveType.Triangles)
                    {
                        PrimitiveBrawl newprim = new PrimitiveBrawl(PrimType.TriangleStrip);
                        uint lastVert = 0;
                        for (int j = 0; j < primbrawl.Indices.Count / 3; j++)
                        {
                            if (j > 0)
                            {
                                newprim.Indices.Add(lastVert);
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 0]);
                            }

                            newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 0]);

                            if (j % 2 == 0)
                            {
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 1]);
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 2]);

                                lastVert = primbrawl.Indices[(j) * 3 + 2];
                            }
                            else
                            {
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 2]);
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 1]);


                                lastVert = primbrawl.Indices[(j) * 3 + 1];
                            }
                        }
                        primlist[i] = newprim;
                    }
                }
            }

            //Console.WriteLine(String.Format("Done, {0} primitives", primlist.Count));

            foreach (PrimitiveBrawl primbrawl in primlist)
            {
                //Primitive prim = new Primitive(GXPrimitiveType.TriangleStrip);
                Primitive prim = new Primitive((GXPrimitiveType)primbrawl.Type);
                //Console.WriteLine(String.Format("Primitive type {0}", (GXPrimitiveType)primbrawl.Type));
                foreach (int vertIndex in primbrawl.Indices)
                {
                    Vertex vert = new Vertex();

                    Weight rootWeight = new Weight();
                    rootWeight.AddWeight(1.0f, 0);

                    vert.SetWeight(rootWeight);
                    //int vertIndex = face.Indices[i];

                    foreach (VertexAttribute attrib in activeAttribs)
                    {
                        switch (attrib)
                        {
                            case VertexAttribute.Position:
                                List<Vector3> posData = (List<Vector3>)vertData.GetAttributeData(VertexAttribute.Position);
                                Vector3 vertPos = mesh.Vertices[vertIndex].ToOpenTKVector3();

                                if (!posData.Contains(vertPos))
                                    posData.Add(vertPos);
                                AttributeData.Positions.Add(vertPos);

                                vert.SetAttributeIndex(VertexAttribute.Position, (uint)posData.IndexOf(vertPos));
                                break;
                            case VertexAttribute.Normal:
                                List<Vector3> normData = (List<Vector3>)vertData.GetAttributeData(VertexAttribute.Normal);
                                Vector3 vertNrm = mesh.Normals[vertIndex].ToOpenTKVector3();

                                if (!normData.Contains(vertNrm))
                                    normData.Add(vertNrm);
                                AttributeData.Normals.Add(vertNrm);

                                vert.SetAttributeIndex(VertexAttribute.Normal, (uint)normData.IndexOf(vertNrm));
                                break;
                            case VertexAttribute.ColorChannel0:
                            case VertexAttribute.ColorChannel1:
                                int colNo = (int)attrib - 11;
                                List<Color> colData = (List<Color>)vertData.GetAttributeData(VertexAttribute.ColorChannel0 + colNo);
                                Color vertCol = mesh.VertexColorChannels[colNo][vertIndex].ToSuperBMDColorRGBA();


                                if (colNo == 0)
                                    AttributeData.ColorChannel0.Add(vertCol);
                                else
                                    AttributeData.ColorChannel1.Add(vertCol);


                                vert.SetAttributeIndex(VertexAttribute.ColorChannel0 + colNo, (uint)colData.IndexOf(vertCol));
                                break;
                            case VertexAttribute.TexCoord0:
                            case VertexAttribute.TexCoord1:
                            case VertexAttribute.TexCoord2:
                            case VertexAttribute.TexCoord3:
                            case VertexAttribute.TexCoord4:
                            case VertexAttribute.TexCoord5:
                            case VertexAttribute.TexCoord6:
                            case VertexAttribute.TexCoord7:
                                int texNo = (int)attrib - 13;
                                List<Vector2> texCoordData = (List<Vector2>)vertData.GetAttributeData(VertexAttribute.TexCoord0 + texNo);
                                Vector2 vertTexCoord = mesh.TextureCoordinateChannels[texNo][vertIndex].ToOpenTKVector2();
                                vertTexCoord = new Vector2(vertTexCoord.X, 1.0f - vertTexCoord.Y);


                                switch (texNo)
                                {
                                    case 0:
                                        AttributeData.TexCoord0.Add(vertTexCoord);
                                        break;
                                    case 1:
                                        AttributeData.TexCoord1.Add(vertTexCoord);
                                        break;
                                    case 2:
                                        AttributeData.TexCoord2.Add(vertTexCoord);
                                        break;
                                    case 3:
                                        AttributeData.TexCoord3.Add(vertTexCoord);
                                        break;
                                    case 4:
                                        AttributeData.TexCoord4.Add(vertTexCoord);
                                        break;
                                    case 5:
                                        AttributeData.TexCoord5.Add(vertTexCoord);
                                        break;
                                    case 6:
                                        AttributeData.TexCoord6.Add(vertTexCoord);
                                        break;
                                    case 7:
                                        AttributeData.TexCoord7.Add(vertTexCoord);
                                        break;
                                }

                                vert.SetAttributeIndex(VertexAttribute.TexCoord0 + texNo, (uint)texCoordData.IndexOf(vertTexCoord));
                                break;
                        }
                    }

                    //triindices[vertIndex] = 1;
                    prim.Vertices.Add(vert);
                }

                packet.Primitives.Add(prim);
            }


            packet.MatrixIndices.Add(0);
            Packets.Add(packet);

            BoundingSphere.GetBoundsValues(AttributeData.Positions);
            Console.Write("...✓\n");
        }

        public void ProcessVerticesWithWeights(Mesh mesh, VertexData vertData, Dictionary<string, int> boneNames, EVP1 envelopes, DRW1 partialWeight)
        {
            Weight[] weights = new Weight[mesh.Vertices.Count];

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                int vertexid = i;
                Weight vertWeight = new Weight();

                foreach (Assimp.Bone bone in mesh.Bones)
                {
                    foreach (VertexWeight weight in bone.VertexWeights)
                    {
                        if (weight.VertexID == vertexid)
                            vertWeight.AddWeight(weight.Weight, boneNames[bone.Name]);
                    }
                }
                vertWeight.reorderBones();
                weights[vertexid] = vertWeight;
            }

            //Primitive prim = new Primitive(GXPrimitiveType.Triangles);
            List<VertexAttribute> activeAttribs = Descriptor.GetActiveAttributes();
            AttributeData.SetAttributesFromList(activeAttribs);


            uint[] triindices = MakeTriIndexList(mesh);

            List<PrimitiveBrawl> primlist;

            if (Arguments.TriStripMode == "all")
            {
                //Console.WriteLine("Calculating triangle strips for Weighted");
                TriStripper stripper = new TriStripper(triindices, weights);
                primlist = stripper.Strip();
            }
            else
            {
                //Console.WriteLine("Calculating triangle list for Weighted");
                primlist = new List<PrimitiveBrawl>();
                PrimitiveBrawl prim = new PrimitiveBrawl(PrimType.TriangleList); // Trilist
                foreach (uint index in triindices)
                {
                    prim.Indices.Add(index);
                }
                primlist.Add(prim);
            }

            //Console.WriteLine(String.Format("Done, {0} primitives", primlist.Count));



            Packet pack = new Packet();
            List<Weight> packetWeights = new List<Weight>();
            int numMatrices = 0;

            if (Arguments.ShouldDegenerateTriangles)
            {
                Console.WriteLine("Converting Triangle Lists into Triangle Strips with degenerate triangles.");

                for (int i = 0; i < primlist.Count; i++)
                {
                    PrimitiveBrawl primbrawl = primlist[i];

                    GXPrimitiveType primtype = (GXPrimitiveType)primbrawl.Type;

                    if (primtype == GXPrimitiveType.Triangles)
                    {
                        PrimitiveBrawl newprim = new PrimitiveBrawl(PrimType.TriangleStrip);
                        uint lastVert = 0;
                        for (int j = 0; j < primbrawl.Indices.Count / 3; j++)
                        {
                            if (j > 0)
                            {
                                newprim.Indices.Add(lastVert);
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 0]);
                            }

                            newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 0]);

                            if (j % 2 == 0)
                            {
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 1]);
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 2]);

                                lastVert = primbrawl.Indices[(j) * 3 + 2];
                            }
                            else
                            {
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 2]);
                                newprim.Indices.Add(primbrawl.Indices[(j) * 3 + 1]);


                                lastVert = primbrawl.Indices[(j) * 3 + 1];
                            }
                        }
                        primlist[i] = newprim;
                    }
                }
            }

            foreach (PrimitiveBrawl primbrawl in primlist)
            {
                int numNewMatricesForFirstThreeVerts = 0;
                if (!packetWeights.Contains(weights[primbrawl.Indices[0]]))
                    numNewMatricesForFirstThreeVerts++;
                if (!packetWeights.Contains(weights[primbrawl.Indices[1]]))
                    numNewMatricesForFirstThreeVerts++;
                if (!packetWeights.Contains(weights[primbrawl.Indices[2]]))
                    numNewMatricesForFirstThreeVerts++;
                if (numMatrices + numNewMatricesForFirstThreeVerts > MaxMatricesPerPacket)
                {
                    // We won't be able to fit even the first 3 vertices of this primitive without going over the matrix limit.
                    // So we need to start a new packet.
                    packetWeights.Clear();
                    numMatrices = 0;
                    Packets.Add(pack);
                    pack = new Packet();
                }


                Primitive prim = new Primitive((GXPrimitiveType)primbrawl.Type);
                //Primitive prim = new Primitive(GXPrimitiveType.TriangleStrip);

                int currvert = -1;
                int maxvert = primbrawl.Indices.Count - 1;
                GXPrimitiveType primtype = (GXPrimitiveType)primbrawl.Type;

                if (primtype == GXPrimitiveType.TriangleStrip)
                {
                    //Console.WriteLine("Doing Tristrip");
                    foreach (int vertIndex in primbrawl.Indices)
                    {
                        currvert++;
                        Weight vertWeight = weights[vertIndex];

                        int oldmat = numMatrices;
                        if (!packetWeights.Contains(vertWeight))
                        {
                            packetWeights.Add(vertWeight);
                            numMatrices++;
                        }

                        //Console.WriteLine(String.Format("Added {0} matrices, is now {1}", numMatrices - oldmat, numMatrices));

                        // There are too many matrices, we need to create a new packet
                        if (numMatrices > MaxMatricesPerPacket)
                        {
                            // If we break up and the resulting TriStrip becomes invalid,
                            // then we need to handle those cases.

                            //Console.WriteLine(String.Format("Breaking up because over the limit: {0}", numMatrices));

                            if (prim.PrimitiveType == GXPrimitiveType.TriangleStrip)
                            {
                                Debug.Assert(prim.Vertices.Count >= 3);
                            }
                            else if (prim.PrimitiveType == GXPrimitiveType.Triangles)
                            {
                                Debug.Assert(prim.Vertices.Count % 3 == 0);
                            }
                            pack.Primitives.Add(prim);


                            Primitive newprim = new Primitive(GXPrimitiveType.TriangleStrip);
                            Vertex prev3 = new Vertex(prim.Vertices[prim.Vertices.Count - 3]);
                            Vertex prev2 = new Vertex(prim.Vertices[prim.Vertices.Count - 2]);
                            Vertex prev = new Vertex(prim.Vertices[prim.Vertices.Count - 1]);
                            bool isOdd = currvert % 2 != 0;
                            if (isOdd)
                            {
                                // Need to preserve whether each vertex is even or odd inside the triangle strip.
                                // Do this by adding an extra vertex from the previous packet to the start of this one.
                                newprim.Vertices.Add(prev3);
                            }
                            newprim.Vertices.Add(prev2);
                            newprim.Vertices.Add(prev);

                            prim = newprim;

                            packetWeights.Clear();
                            numMatrices = 0;
                            Packets.Add(pack);
                            Packet oldPack = pack;
                            pack = new Packet();

                            // Calculate matrices for current packet in case we added vertices
                            foreach (Vertex vertex in prim.Vertices)
                            {
                                if (!packetWeights.Contains(vertex.VertexWeight))
                                {
                                    packetWeights.Add(vertex.VertexWeight);
                                    numMatrices++;
                                }

                                // Re-add the matrix index for the duplicated verts to the new packet.
                                // And recalculate the matrix index index in each vert's attribute data.
                                uint oldMatrixIndexIndex = vertex.GetAttributeIndex(VertexAttribute.PositionMatrixIdx);
                                int matrixIndex = oldPack.MatrixIndices[(int)oldMatrixIndexIndex];

                                if (!pack.MatrixIndices.Contains(matrixIndex))
                                    pack.MatrixIndices.Add(matrixIndex);
                                vertex.SetAttributeIndex(VertexAttribute.PositionMatrixIdx, (uint)pack.MatrixIndices.IndexOf(matrixIndex));
                            }

                            if (!packetWeights.Contains(vertWeight))
                            {
                                packetWeights.Add(vertWeight);
                                numMatrices++;
                            }
                        }

                        Vertex vert = new Vertex();
                        Weight curWeight = vertWeight;

                        vert.SetWeight(curWeight);

                        foreach (VertexAttribute attrib in activeAttribs)
                        {
                            switch (attrib)
                            {
                                case VertexAttribute.PositionMatrixIdx:
                                    int newMatrixIndex = -1;

                                    if (curWeight.WeightCount == 1)
                                    {
                                        newMatrixIndex = partialWeight.MeshWeights.IndexOf(curWeight);
                                    }
                                    else
                                    {
                                        if (!envelopes.Weights.Contains(curWeight))
                                            envelopes.Weights.Add(curWeight);

                                        int envIndex = envelopes.Weights.IndexOf(curWeight);
                                        int drwIndex = partialWeight.MeshWeights.IndexOf(curWeight);

                                        if (drwIndex == -1)
                                        {
                                            throw new System.Exception($"Model has unweighted vertices in mesh \"{mesh.Name}\". Please weight all vertices to at least one bone.");
                                        }

                                        newMatrixIndex = drwIndex;
                                        partialWeight.Indices[drwIndex] = envIndex;
                                    }

                                    if (!pack.MatrixIndices.Contains(newMatrixIndex))
                                        pack.MatrixIndices.Add(newMatrixIndex);

                                    vert.SetAttributeIndex(VertexAttribute.PositionMatrixIdx, (uint)pack.MatrixIndices.IndexOf(newMatrixIndex));
                                    break;
                                case VertexAttribute.Position:
                                    List<Vector3> posData = (List<Vector3>)vertData.GetAttributeData(VertexAttribute.Position);
                                    Vector3 vertPos = mesh.Vertices[vertIndex].ToOpenTKVector3();

                                    if (curWeight.WeightCount == 1)
                                    {
                                        Matrix4 ibm = envelopes.InverseBindMatrices[curWeight.BoneIndices[0]];

                                        Vector3 transVec = Vector3.TransformPosition(vertPos, ibm);
                                        if (!posData.Contains(transVec))
                                            posData.Add(transVec);
                                        AttributeData.Positions.Add(transVec);
                                        vert.SetAttributeIndex(VertexAttribute.Position, (uint)posData.IndexOf(transVec));
                                    }
                                    else
                                    {
                                        if (!posData.Contains(vertPos))
                                            posData.Add(vertPos);
                                        AttributeData.Positions.Add(vertPos);

                                        vert.SetAttributeIndex(VertexAttribute.Position, (uint)posData.IndexOf(vertPos));
                                    }
                                    break;
                                case VertexAttribute.Normal:
                                    List<Vector3> normData = (List<Vector3>)vertData.GetAttributeData(VertexAttribute.Normal);
                                    Vector3 vertNrm = mesh.Normals[vertIndex].ToOpenTKVector3();

                                    if (curWeight.WeightCount == 1)
                                    {
                                        Matrix4 ibm = envelopes.InverseBindMatrices[curWeight.BoneIndices[0]];
                                        vertNrm = Vector3.TransformNormal(vertNrm, ibm);
                                        if (!normData.Contains(vertNrm))
                                            normData.Add(vertNrm);
                                    }
                                    else
                                    {
                                        if (!normData.Contains(vertNrm))
                                            normData.Add(vertNrm);
                                    }

                                    AttributeData.Normals.Add(vertNrm);
                                    vert.SetAttributeIndex(VertexAttribute.Normal, (uint)normData.IndexOf(vertNrm));
                                    break;
                                case VertexAttribute.ColorChannel0:
                                case VertexAttribute.ColorChannel1:
                                    int colNo = (int)attrib - 11;
                                    List<Color> colData = (List<Color>)vertData.GetAttributeData(VertexAttribute.ColorChannel0 + colNo);
                                    Color vertCol = mesh.VertexColorChannels[colNo][vertIndex].ToSuperBMDColorRGBA();

                                    if (colNo == 0)
                                        AttributeData.ColorChannel0.Add(vertCol);
                                    else
                                        AttributeData.ColorChannel1.Add(vertCol);

                                    vert.SetAttributeIndex(VertexAttribute.ColorChannel0 + colNo, (uint)colData.IndexOf(vertCol));
                                    break;
                                case VertexAttribute.TexCoord0:
                                case VertexAttribute.TexCoord1:
                                case VertexAttribute.TexCoord2:
                                case VertexAttribute.TexCoord3:
                                case VertexAttribute.TexCoord4:
                                case VertexAttribute.TexCoord5:
                                case VertexAttribute.TexCoord6:
                                case VertexAttribute.TexCoord7:
                                    int texNo = (int)attrib - 13;
                                    List<Vector2> texCoordData = (List<Vector2>)vertData.GetAttributeData(VertexAttribute.TexCoord0 + texNo);
                                    Vector2 vertTexCoord = mesh.TextureCoordinateChannels[texNo][vertIndex].ToOpenTKVector2();
                                    vertTexCoord = new Vector2(vertTexCoord.X, 1.0f - vertTexCoord.Y);

                                    switch (texNo)
                                    {
                                        case 0:
                                            AttributeData.TexCoord0.Add(vertTexCoord);
                                            break;
                                        case 1:
                                            AttributeData.TexCoord1.Add(vertTexCoord);
                                            break;
                                        case 2:
                                            AttributeData.TexCoord2.Add(vertTexCoord);
                                            break;
                                        case 3:
                                            AttributeData.TexCoord3.Add(vertTexCoord);
                                            break;
                                        case 4:
                                            AttributeData.TexCoord4.Add(vertTexCoord);
                                            break;
                                        case 5:
                                            AttributeData.TexCoord5.Add(vertTexCoord);
                                            break;
                                        case 6:
                                            AttributeData.TexCoord6.Add(vertTexCoord);
                                            break;
                                        case 7:
                                            AttributeData.TexCoord7.Add(vertTexCoord);
                                            break;
                                    }

                                    vert.SetAttributeIndex(VertexAttribute.TexCoord0 + texNo, (uint)texCoordData.IndexOf(vertTexCoord));
                                    break;
                            }
                        }
                        prim.Vertices.Add(vert);
                    }
                }
                else if (primtype == GXPrimitiveType.Triangles)
                {
                    for (int j = 0; j < primbrawl.Indices.Count / 3; j++)
                    {
                        int vert1Index = (int)primbrawl.Indices[j * 3 + 0];
                        int vert2Index = (int)primbrawl.Indices[j * 3 + 1];
                        int vert3Index = (int)primbrawl.Indices[j * 3 + 2];
                        Weight vert1Weight = weights[vert1Index];
                        Weight vert2Weight = weights[vert2Index];
                        Weight vert3Weight = weights[vert3Index];
                        int oldcount = numMatrices;
                        if (!packetWeights.Contains(vert1Weight))
                        {
                            packetWeights.Add(vert1Weight);
                            numMatrices++;
                        }
                        if (!packetWeights.Contains(vert2Weight))
                        {
                            packetWeights.Add(vert2Weight);
                            numMatrices++;
                        }
                        if (!packetWeights.Contains(vert3Weight))
                        {
                            packetWeights.Add(vert3Weight);
                            numMatrices++;
                        }

                        // There are too many matrices, we need to create a new packet
                        if (numMatrices > MaxMatricesPerPacket)
                        {
                            //Console.WriteLine(String.Format("Making new packet because previous one would have {0}", numMatrices));
                            //Console.WriteLine(oldcount);
                            pack.Primitives.Add(prim);
                            Packets.Add(pack);

                            prim = new Primitive(GXPrimitiveType.Triangles);
                            pack = new Packet();

                            packetWeights.Clear();
                            numMatrices = 0;

                            if (!packetWeights.Contains(vert1Weight))
                            {
                                packetWeights.Add(vert1Weight);
                                numMatrices++;
                            }
                            if (!packetWeights.Contains(vert2Weight))
                            {
                                packetWeights.Add(vert2Weight);
                                numMatrices++;
                            }
                            if (!packetWeights.Contains(vert3Weight))
                            {
                                packetWeights.Add(vert3Weight);
                                numMatrices++;
                            }
                        }

                        int[] vertexIndexArray = new int[] { vert1Index, vert2Index, vert3Index };
                        Weight[] vertWeightArray = new Weight[] { vert1Weight, vert2Weight, vert3Weight };

                        for (int i = 0; i < 3; i++)
                        {
                            Vertex vert = new Vertex();
                            int vertIndex = vertexIndexArray[i];
                            Weight curWeight = vertWeightArray[i];

                            vert.SetWeight(curWeight);

                            foreach (VertexAttribute attrib in activeAttribs)
                            {
                                switch (attrib)
                                {
                                    case VertexAttribute.PositionMatrixIdx:
                                        int newMatrixIndex = -1;

                                        if (curWeight.WeightCount == 1)
                                        {
                                            newMatrixIndex = partialWeight.MeshWeights.IndexOf(curWeight);
                                        }
                                        else
                                        {
                                            if (!envelopes.Weights.Contains(curWeight))
                                                envelopes.Weights.Add(curWeight);

                                            int envIndex = envelopes.Weights.IndexOf(curWeight);
                                            int drwIndex = partialWeight.MeshWeights.IndexOf(curWeight);

                                            if (drwIndex == -1)
                                            {
                                                throw new System.Exception($"Model has unweighted vertices in mesh \"{mesh.Name}\". Please weight all vertices to at least one bone.");
                                            }

                                            newMatrixIndex = drwIndex;
                                            partialWeight.Indices[drwIndex] = envIndex;
                                        }

                                        if (!pack.MatrixIndices.Contains(newMatrixIndex))
                                            pack.MatrixIndices.Add(newMatrixIndex);

                                        vert.SetAttributeIndex(VertexAttribute.PositionMatrixIdx, (uint)pack.MatrixIndices.IndexOf(newMatrixIndex));
                                        break;
                                    case VertexAttribute.Position:
                                        List<Vector3> posData = (List<Vector3>)vertData.GetAttributeData(VertexAttribute.Position);
                                        Vector3 vertPos = mesh.Vertices[vertIndex].ToOpenTKVector3();

                                        if (curWeight.WeightCount == 1)
                                        {
                                            Matrix4 ibm = envelopes.InverseBindMatrices[curWeight.BoneIndices[0]];

                                            Vector3 transVec = Vector3.TransformPosition(vertPos, ibm);
                                            if (!posData.Contains(transVec))
                                                posData.Add(transVec);
                                            AttributeData.Positions.Add(transVec);
                                            vert.SetAttributeIndex(VertexAttribute.Position, (uint)posData.IndexOf(transVec));
                                        }
                                        else
                                        {
                                            if (!posData.Contains(vertPos))
                                                posData.Add(vertPos);
                                            AttributeData.Positions.Add(vertPos);

                                            vert.SetAttributeIndex(VertexAttribute.Position, (uint)posData.IndexOf(vertPos));
                                        }
                                        break;
                                    case VertexAttribute.Normal:
                                        List<Vector3> normData = (List<Vector3>)vertData.GetAttributeData(VertexAttribute.Normal);
                                        Vector3 vertNrm = mesh.Normals[vertIndex].ToOpenTKVector3();

                                        if (curWeight.WeightCount == 1)
                                        {
                                            Matrix4 ibm = envelopes.InverseBindMatrices[curWeight.BoneIndices[0]];
                                            vertNrm = Vector3.TransformNormal(vertNrm, ibm);
                                            if (!normData.Contains(vertNrm))
                                                normData.Add(vertNrm);
                                        }
                                        else
                                        {
                                            if (!normData.Contains(vertNrm))
                                                normData.Add(vertNrm);
                                        }

                                        AttributeData.Normals.Add(vertNrm);
                                        vert.SetAttributeIndex(VertexAttribute.Normal, (uint)normData.IndexOf(vertNrm));
                                        break;
                                    case VertexAttribute.ColorChannel0:
                                    case VertexAttribute.ColorChannel1:
                                        int colNo = (int)attrib - 11;
                                        List<Color> colData = (List<Color>)vertData.GetAttributeData(VertexAttribute.ColorChannel0 + colNo);
                                        Color vertCol = mesh.VertexColorChannels[colNo][vertIndex].ToSuperBMDColorRGBA();

                                        if (colNo == 0)
                                            AttributeData.ColorChannel0.Add(vertCol);
                                        else
                                            AttributeData.ColorChannel1.Add(vertCol);

                                        vert.SetAttributeIndex(VertexAttribute.ColorChannel0 + colNo, (uint)colData.IndexOf(vertCol));
                                        break;
                                    case VertexAttribute.TexCoord0:
                                    case VertexAttribute.TexCoord1:
                                    case VertexAttribute.TexCoord2:
                                    case VertexAttribute.TexCoord3:
                                    case VertexAttribute.TexCoord4:
                                    case VertexAttribute.TexCoord5:
                                    case VertexAttribute.TexCoord6:
                                    case VertexAttribute.TexCoord7:
                                        int texNo = (int)attrib - 13;
                                        List<Vector2> texCoordData = (List<Vector2>)vertData.GetAttributeData(VertexAttribute.TexCoord0 + texNo);
                                        Vector2 vertTexCoord = mesh.TextureCoordinateChannels[texNo][vertIndex].ToOpenTKVector2();
                                        vertTexCoord = new Vector2(vertTexCoord.X, 1.0f - vertTexCoord.Y);

                                        switch (texNo)
                                        {
                                            case 0:
                                                AttributeData.TexCoord0.Add(vertTexCoord);
                                                break;
                                            case 1:
                                                AttributeData.TexCoord1.Add(vertTexCoord);
                                                break;
                                            case 2:
                                                AttributeData.TexCoord2.Add(vertTexCoord);
                                                break;
                                            case 3:
                                                AttributeData.TexCoord3.Add(vertTexCoord);
                                                break;
                                            case 4:
                                                AttributeData.TexCoord4.Add(vertTexCoord);
                                                break;
                                            case 5:
                                                AttributeData.TexCoord5.Add(vertTexCoord);
                                                break;
                                            case 6:
                                                AttributeData.TexCoord6.Add(vertTexCoord);
                                                break;
                                            case 7:
                                                AttributeData.TexCoord7.Add(vertTexCoord);
                                                break;
                                        }

                                        vert.SetAttributeIndex(VertexAttribute.TexCoord0 + texNo, (uint)texCoordData.IndexOf(vertTexCoord));
                                        break;
                                }
                            }

                            prim.Vertices.Add(vert);
                        }
                    }
                }

                /*
                if (prim.PrimitiveType == GXPrimitiveType.TriangleStrip) {
                    Debug.Assert(prim.Vertices.Count >= 3);
                }
                else if (prim.PrimitiveType == GXPrimitiveType.Triangles) {
                    Debug.Assert(prim.Vertices.Count % 3 == 0);
                }*/
                //Console.WriteLine(String.Format("We had this many matrices: {0}", numMatrices));
                pack.Primitives.Add(prim);
            }
            Packets.Add(pack);

            int mostmatrices = 0;
            if (true)
            {
                List<Weight> packWeights = new List<Weight>();
                foreach (Packet packet in Packets)
                {

                    int matrices = 0;

                    foreach (Primitive prim in packet.Primitives)
                    {
                        foreach (Vertex vert in prim.Vertices)
                        {
                            if (!packWeights.Contains(vert.VertexWeight))
                            {
                                packWeights.Add(vert.VertexWeight);
                                matrices++;
                            }
                        }


                        if (prim.PrimitiveType == GXPrimitiveType.TriangleStrip)
                        {
                            Debug.Assert(prim.Vertices.Count >= 3);
                        }
                        else if (prim.PrimitiveType == GXPrimitiveType.Triangles)
                        {
                            Debug.Assert(prim.Vertices.Count % 3 == 0);
                        }
                    }
                    if (matrices > mostmatrices) mostmatrices = matrices;
                    //Debug.Assert(matrices <= MaxMatricesPerPacket);
                    //Console.WriteLine(matrices);
                    packWeights.Clear();
                }
            }
            //Console.WriteLine(String.Format("Most matrices: {0}", mostmatrices));
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write((byte)MatrixType);
            writer.Write((sbyte)-1);
            writer.Write((short)Packets.Count);
            writer.Write((short)0); // Placeholder for descriptor offset
            writer.Write((short)0); // Placeholder for starting packet index
            writer.Write((short)0); // Placeholder for starting packet matrix index offset
            writer.Write((short)-1);
            writer.Write(BoundingSphere);
        }
    }
}
