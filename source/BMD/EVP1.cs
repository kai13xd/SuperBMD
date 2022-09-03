using SuperBMD.Rigging;

namespace SuperBMD.BMD
{
    public class EVP1
    {
        public List<Weight> Weights { get; private set; }
        public List<Matrix4> InverseBindMatrices { get; private set; }

        public EVP1()
        {
            Weights = new List<Weight>();
            InverseBindMatrices = new List<Matrix4>();
        }

        public EVP1(ref EndianBinaryReader reader, BMDInfo modelstats = null)
        {
            Weights = new List<Weight>();
            InverseBindMatrices = new List<Matrix4>();

            var offset = reader.Position;
            if (reader.ReadString(4) != "EVP1")
                throw new Exception("SuperBMD is lost! EVP1 header is malformed!");
            int evp1Size = reader.ReadInt();
            int entryCount = reader.ReadShort();
            if (entryCount == 0)
            {
                reader.Seek(offset + evp1Size);
                return;
            }
            reader.ReadShort();

            if (modelstats is not null)
            {
                modelstats.EVP1Size = evp1Size;
            }

            int weightCountsOffset = reader.ReadInt();
            int boneIndicesOffset = reader.ReadInt();
            int weightDataOffset = reader.ReadInt();
            int inverseBindMatricesOffset = reader.ReadInt();

            List<int> counts = new List<int>();
            List<float> weights = new List<float>();
            List<int> indices = new List<int>();

            for (int i = 0; i < entryCount; i++)
                counts.Add(reader.ReadByte());

            reader.Seek(boneIndicesOffset + offset);

            for (int i = 0; i < entryCount; i++)
            {
                for (int j = 0; j < counts[i]; j++)
                {
                    indices.Add(reader.ReadShort());
                }
            }

            reader.Seek(weightDataOffset + offset);

            for (int i = 0; i < entryCount; i++)
            {
                for (int j = 0; j < counts[i]; j++)
                {
                    weights.Add(reader.ReadFloat());
                }
            }

            int totalRead = 0;
            for (int i = 0; i < entryCount; i++)
            {
                Weight weight = new Weight();

                for (int j = 0; j < counts[i]; j++)
                {
                    weight.AddWeight(weights[totalRead + j], indices[totalRead + j]);
                }

                Weights.Add(weight);
                totalRead += counts[i];
            }

            reader.Seek(inverseBindMatricesOffset + offset);
            int matrixCount = (evp1Size - inverseBindMatricesOffset) / 48;

            for (int i = 0; i < matrixCount; i++)
            {
                Matrix3x4 invBind = new Matrix3x4(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(),
                                                  reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(),
                                                  reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());

                InverseBindMatrices.Add(new Matrix4(invBind.Row0, invBind.Row1, invBind.Row2, Vector4.UnitW));
            }

            reader.Seek(offset + evp1Size);
            reader.Align(32);
        }

        public EVP1(Assimp.Scene scene, List<Rigging.Bone> flatSkeleton)
        {
            Weights = new List<Weight>();

            foreach (Assimp.Mesh mesh in scene.Meshes)
            {
                SortedDictionary<int, Weight> weights = new SortedDictionary<int, Weight>();

                foreach (Assimp.Bone bone in mesh.Bones)
                {
                    Rigging.Bone bmdBone = flatSkeleton.Find(x => x.Name == bone.Name);

                    foreach (Assimp.VertexWeight vertWeight in bone.VertexWeights)
                    {
                        if (vertWeight.Weight > 1.0f)
                        {
                            if (!weights.ContainsKey(vertWeight.VertexID))
                            {
                                weights.Add(vertWeight.VertexID, new Weight());
                            }

                            weights[vertWeight.VertexID].AddWeight(vertWeight.Weight, flatSkeleton.IndexOf(bmdBone));
                        }
                    }

                    Matrix4 invBind = new Matrix4(
                        bone.OffsetMatrix.A1, bone.OffsetMatrix.A2, bone.OffsetMatrix.A3, bone.OffsetMatrix.A4,
                        bone.OffsetMatrix.B1, bone.OffsetMatrix.B2, bone.OffsetMatrix.B3, bone.OffsetMatrix.B4,
                        bone.OffsetMatrix.C1, bone.OffsetMatrix.C2, bone.OffsetMatrix.C3, bone.OffsetMatrix.C4,
                        bone.OffsetMatrix.D1, bone.OffsetMatrix.D2, bone.OffsetMatrix.D3, bone.OffsetMatrix.D4);

                    bmdBone.SetInverseBindMatrix(invBind);
                }

                Weights.AddRange(weights.Values);
                foreach (Weight weight in Weights)
                {
                    weight.reorderBones();
                }
            }
        }

        public void SetInverseBindMatrices(Assimp.Scene scene, List<Rigging.Bone> flatSkel)
        {
            for (int i = 0; i < flatSkel.Count; i++)
                InverseBindMatrices.Add(new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW));

            foreach (Assimp.Mesh mesh in scene.Meshes)
            {
                foreach (Assimp.Bone bone in mesh.Bones)
                {
                    Console.Write(".");
                    Assimp.Matrix4x4 assMat = bone.OffsetMatrix;

                    Matrix4 transposed = new Matrix4(assMat.A1, assMat.B1, assMat.C1, assMat.D1,
                                                     assMat.A2, assMat.B2, assMat.C2, assMat.D2,
                                                     assMat.A3, assMat.B3, assMat.C3, assMat.D3,
                                                     assMat.A4, assMat.B4, assMat.C4, assMat.D4);

                    int index = flatSkel.FindIndex(x => x.Name == bone.Name);
                    if (index == -1)
                    {
                        throw new System.Exception(String.Format("Model uses bone that isn't part of the skeleton: {0}", bone.Name));
                    }
                    InverseBindMatrices[index] = transposed;
                    flatSkel[index].SetInverseBindMatrix(transposed);
                }
            }
            Console.Write(".✓\n");
        }

        public void SetInverseBindMatrices(List<Rigging.Bone> flatSkel)
        {
            if (InverseBindMatrices.Count == 0)
            {
                // If the original file didn't specify any inverse bind matrices, use default values instead of all zeroes.
                // And these must be set both in the skeleton and the EVP1.
                for (int i = 0; i < flatSkel.Count; i++)
                {
                    Matrix4 newMat = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);
                    InverseBindMatrices.Add(newMat);
                    flatSkel[i].SetInverseBindMatrix(newMat);
                }
                return;
            }

            for (int i = 0; i < flatSkel.Count; i++)
            {
                Matrix4 newMat = InverseBindMatrices[i];
                flatSkel[i].SetInverseBindMatrix(newMat);
            }
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            long start = writer.Position;

            writer.Write("EVP1".ToCharArray());
            writer.Write(0); // Placeholder for section size
            writer.Write((short)Weights.Count);
            writer.Write((short)-1);

            if (Weights.Count == 0)
            {
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Write((int)0);
                writer.Seek((int)start + 4);
                writer.Write(32);
                writer.Seek(0);
                writer.PadAlign(8);
                return;
            }

            writer.Write(28); // Offset to weight count data. Always 28
            writer.Write(28 + Weights.Count); // Offset to bone/weight indices. Always 28 + the number of weights
            writer.Write(0); // Placeholder for weight data offset
            writer.Write(0); // Placeholder for inverse bind matrix data offset

            foreach (Weight w in Weights)
                writer.Write((byte)w.WeightCount);

            foreach (Weight w in Weights)
            {
                foreach (int inte in w.BoneIndices)
                    writer.Write((short)inte);
            }

            writer.PadAlign(4);

            long curOffset = writer.Position;

            writer.Seek((int)start + 20);
            writer.Write((int)(curOffset - start));
            writer.Seek((int)curOffset);

            foreach (Weight w in Weights)
            {
                foreach (float fl in w.Weights)
                    writer.Write(fl);
            }

            curOffset = writer.Position;

            writer.Seek((int)start + 24);
            writer.Write((int)(curOffset - start));
            writer.Seek((int)curOffset);

            foreach (Matrix4 mat in InverseBindMatrices)
            {
                writer.Write(mat.Column0);
                writer.Write(mat.Column1);
                writer.Write(mat.Column2);
            }

            writer.PadAlign(32);

            long end = writer.Position;
            long length = (end - start);

            writer.Seek((int)start + 4);
            writer.Write((int)length);
            writer.Seek((int)end);
        }
    }
}
