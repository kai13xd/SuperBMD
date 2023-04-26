
namespace SuperBMD.BMD
{

    public class JNT1
    {
        public List<Rigging.Bone> FlatSkeleton { get; private set; }
        public Dictionary<string, int> BoneNameIndices { get; private set; }
        public Rigging.Bone SkeletonRoot { get; private set; }

        public JNT1(ref EndianBinaryReader reader, BMDInfo modelstats = null)
        {
            BoneNameIndices = new Dictionary<string, int>();
            FlatSkeleton = new List<Rigging.Bone>();

            var offset = reader.Position;
            if (reader.ReadString(4) != "JNT1")
                throw new Exception("SuperBMD is lost! JNT1 header is malformed!");

            int jnt1Size = reader.ReadInt();
            int jointCount = reader.ReadShort();
            reader.Skip(2);

            if (modelstats != null)
            {
                modelstats.JNT1Size = jnt1Size;
            }

            int jointDataOffset = reader.ReadInt();
            int internTableOffset = reader.ReadInt();
            int nameTableOffset = reader.ReadInt();

            List<string> names = NameTableIO.Load(ref reader, offset + nameTableOffset);

            int highestRemap = 0;
            List<int> remapTable = new List<int>();
            reader.Seek(offset + internTableOffset);
            for (int i = 0; i < jointCount; i++)
            {
                int test = reader.ReadShort();
                remapTable.Add(test);

                if (test > highestRemap)
                    highestRemap = test;
            }

            List<Rigging.Bone> tempList = new List<Rigging.Bone>();
            reader.Seek(offset + jointDataOffset);
            for (int i = 0; i <= highestRemap; i++)
            {
                tempList.Add(new Rigging.Bone(ref reader, names[i]));
            }

            for (int i = 0; i < jointCount; i++)
            {
                FlatSkeleton.Add(tempList[remapTable[i]]);
            }

            foreach (Rigging.Bone bone in FlatSkeleton)
                BoneNameIndices.Add(bone.Name, FlatSkeleton.IndexOf(bone));

            reader.Seek(offset + jnt1Size);
            reader.Align(32);
        }

        public void SetInverseBindMatrices(List<Matrix4> matrices)
        {
            /*for (int i = 0; i < FlatSkeleton.Count; i++)
            {
                FlatSkeleton[i].SetInverseBindMatrix(matrices[i]);
            }*/
        }

        public JNT1(Assimp.Scene scene, VTX1 vertexData)
        {
            BoneNameIndices = new Dictionary<string, int>();
            FlatSkeleton = new List<Rigging.Bone>();
            Assimp.Node root = null;

            for (int i = 0; i < scene.RootNode.ChildCount; i++)
            {
                if (scene.RootNode.Children[i].Name.ToLowerInvariant() == "skeleton_root")
                {
                    root = scene.RootNode.Children[i].Children[0];
                    break;
                }
                Console.Write(".");
            }

            if (root is null)
            {
                SkeletonRoot = new Rigging.Bone("root");
                SkeletonRoot.Bounds.GetBoundsValues(vertexData.Attributes.Positions);

                FlatSkeleton.Add(SkeletonRoot);
                BoneNameIndices.Add("root", 0);
            }

            else
            {
                SkeletonRoot = AssimpNodesToBonesRecursive(root, null, FlatSkeleton);
                foreach (Rigging.Bone bone in FlatSkeleton)
                {
                    //bone.m_MatrixType = 1;
                    //bone.m_UnknownIndex = 1;
                    BoneNameIndices.Add(bone.Name, FlatSkeleton.IndexOf(bone));
                }
                //FlatSkeleton[0].m_MatrixType = 0;
                //FlatSkeleton[0].m_UnknownIndex = 0;
            }
            Console.Write("✓\n");

        }

        public void UpdateBoundingBoxes(VTX1 vertexData)
        {
            FlatSkeleton[0].Bounds.GetBoundsValues(vertexData.Attributes.Positions);
            for (int i = 1; i < FlatSkeleton.Count; i++)
            {
                FlatSkeleton[i].Bounds = FlatSkeleton[0].Bounds;
            }

        }

        private Rigging.Bone AssimpNodesToBonesRecursive(Assimp.Node node, Rigging.Bone parent, List<Rigging.Bone> boneList)
        {
            Rigging.Bone newBone = new(node, parent);
            boneList.Add(newBone);

            for (int i = 0; i < node.ChildCount; i++)
            {
                newBone.Children.Add(AssimpNodesToBonesRecursive(node.Children[i], newBone, boneList));
            }

            return newBone;
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            int start = writer.Position;

            writer.Write("JNT1");
            writer.Write(0); // Placeholder for section size
            writer.Write((short)FlatSkeleton.Count);
            writer.Write((short)-1);

            writer.Write(24); // Offset to joint data, always 24
            writer.Write(0); // Placeholder for remap data offset
            writer.Write(0); // Placeholder for name table offset

            List<string> names = new();
            foreach (Rigging.Bone bone in FlatSkeleton)
            {
                writer.Write(bone);
                names.Add(bone.Name);
            }

            //Write remap data offset
            int curOffset = writer.Position;
            writer.Seek(start + 16);
            writer.Write(curOffset - start);
            writer.Seek(curOffset);

            for (int i = 0; i < FlatSkeleton.Count; i++)
                writer.Write((short)i);
            writer.PadAlign(4);

            //Write name table offset
            curOffset = writer.Position;
            writer.Seek(start + 20);
            writer.Write(curOffset - start);
            writer.Seek(curOffset);

            NameTableIO.Write(ref writer, names);

            writer.PadAlign(32);



            //Write section length
            int end = writer.Position;
            int length = (end - start);
            writer.Seek(start + 4);
            writer.Write(length);
            writer.Seek(end);
        }

        public void DumpJson(string path)
        {
            File.WriteAllText(path, this.JsonSerialize());
        }
    }

}
