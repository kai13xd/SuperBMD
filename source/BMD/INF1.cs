﻿using SuperBMD.Rigging;
using SuperBMD.Scenegraph;
using SuperBMD.Scenegraph.Enums;
namespace SuperBMD.BMD
{
    public class INF1
    {
        [JsonIgnore]
        public List<SceneNode> FlatNodes { get; set; }
        public SceneNode Root { get; set; }

        public INF1()
        {
            FlatNodes = new List<SceneNode>();
            Root = null;
        }

        public INF1(ref EndianBinaryReader reader, BMDInfo modelstats)
        {
            FlatNodes = new List<SceneNode>();

            if (reader.ReadString(4) != "INF1")
                throw new Exception("SuperBMD is lost! INF1 header is malformed!");

            modelstats.INF1Size = reader.ReadInt();
            var unk1 = (MatrixTransformType)reader.ReadShort();
            Console.WriteLine(unk1);
            reader.Skip(2);

            int packetCount = reader.ReadInt();
            int vertexCount = reader.ReadInt();
            int hierarchyOffset = reader.ReadInt();

            SceneNode parent = new SceneNode(ref reader, null);
            SceneNode node = null;

            Root = parent;
            FlatNodes.Add(parent);

            do
            {
                node = new SceneNode(ref reader, parent);

                FlatNodes.Add(node);

                if (node.Type == NodeType.OpenChild)
                {
                    SceneNode newNode = new SceneNode(ref reader, node.Parent);
                    FlatNodes.Add(newNode);
                    parent.Children.Add(newNode);
                    parent = newNode;
                }
                else if (node.Type == NodeType.CloseChild)
                    parent = node.Parent;

            } while (node.Type != NodeType.Terminator);
            reader.Align(32);
        }

        public INF1(Assimp.Scene scene, JNT1 skeleton, bool isMatStrict)
        {
            FlatNodes = new List<SceneNode>();
            Root = new SceneNode(NodeType.Joint, 0, null);
            FlatNodes.Add(Root);

            int downNodeCount = 0;

            // First add objects that should be the direct children of the root bone.
            // This includes any objects that are weighted to multiple bones, as well as objects weighted to only the root bone itself.
            for (int mat_index = 0; mat_index < scene.MaterialCount; mat_index++)
            {
                Console.Write(".");
                for (int i = 0; i < scene.MeshCount; i++)
                {
                    if (scene.Meshes[i].MaterialIndex != mat_index)
                        continue;
                    if (scene.Meshes[i].BoneCount == 1 && scene.Meshes[i].Bones[0].Name != skeleton.FlatSkeleton[0].Name)
                        continue;

                    SceneNode downNode1 = new SceneNode(NodeType.OpenChild, 0, Root);
                    SceneNode matNode;

                    // Sometimes the mesh material index seems to be wrong which results in the wrong material being assigned.
                    // So if mat_strict isn't used we will just use the mesh order for material index.
                    // This also applies to the mat index in GetNodesRecursive.
                    if (isMatStrict)
                    {
                        matNode = new SceneNode(NodeType.Material, scene.Meshes[i].MaterialIndex, Root);
                    }
                    else
                    {
                        matNode = new SceneNode(NodeType.Material, i, Root);
                    }
                    SceneNode downNode2 = new SceneNode(NodeType.OpenChild, 0, Root);
                    SceneNode shapeNode = new SceneNode(NodeType.Shape, i, Root);

                    FlatNodes.Add(downNode1);
                    FlatNodes.Add(matNode);
                    FlatNodes.Add(downNode2);
                    FlatNodes.Add(shapeNode);

                    downNodeCount += 2;
                }
            }

            // Next add objects as children of specific bones, if those objects are weighted to only a single bone.
            if (skeleton.FlatSkeleton.Count > 1)
            {
                SceneNode rootChildDown = new SceneNode(NodeType.OpenChild, 0, Root);
                FlatNodes.Add(rootChildDown);

                foreach (Rigging.Bone bone in skeleton.SkeletonRoot.Children)
                {
                    GetNodesRecursive(bone, skeleton.FlatSkeleton, Root, scene.Meshes, scene.Materials, isMatStrict);
                }

                SceneNode rootChildUp = new SceneNode(NodeType.CloseChild, 0, Root);
                FlatNodes.Add(rootChildUp);
            }

            for (int i = 0; i < downNodeCount; i++)
                FlatNodes.Add(new SceneNode(NodeType.CloseChild, 0, Root));

            FlatNodes.Add(new SceneNode(NodeType.Terminator, 0, Root));
            Console.WriteLine("✓\n");
        }

        private void GetNodesRecursive(Rigging.Bone bone, List<Rigging.Bone> skeleton, SceneNode parent, List<Assimp.Mesh> meshes, List<Assimp.Material> materials, bool isMatStrict)
        {
            SceneNode node = new SceneNode(NodeType.Joint, skeleton.IndexOf(bone), parent);
            FlatNodes.Add(node);

            int downNodeCount = 0;

            for (int mat_index = 0; mat_index < materials.Count; mat_index++)
            {
                foreach (Assimp.Mesh mesh in meshes)
                {
                    if (mesh.MaterialIndex != mat_index)
                        continue;
                    if (mesh.BoneCount != 1 || mesh.Bones[0].Name != bone.Name)
                        continue;

                    SceneNode downNode1 = new SceneNode(NodeType.OpenChild, 0, Root);
                    SceneNode matNode;

                    if (isMatStrict)
                    {
                        matNode = new SceneNode(NodeType.Material, mesh.MaterialIndex, Root);
                    }
                    else
                    {
                        matNode = new SceneNode(NodeType.Material, meshes.IndexOf(mesh), Root);
                    }
                    SceneNode downNode2 = new SceneNode(NodeType.OpenChild, 0, Root);
                    SceneNode shapeNode = new SceneNode(NodeType.Shape, meshes.IndexOf(mesh), Root);

                    FlatNodes.Add(downNode1);
                    FlatNodes.Add(matNode);
                    FlatNodes.Add(downNode2);
                    FlatNodes.Add(shapeNode);

                    downNodeCount += 2;
                }
            }

            if (bone.Children.Count > 0)
            {
                SceneNode downNode = new SceneNode(NodeType.OpenChild, 0, parent);
                FlatNodes.Add(downNode);

                foreach (Rigging.Bone child in bone.Children)
                {
                    GetNodesRecursive(child, skeleton, node, meshes, materials, isMatStrict);
                }

                SceneNode upNode = new SceneNode(NodeType.CloseChild, 0, parent);
                FlatNodes.Add(upNode);
            }

            for (int i = 0; i < downNodeCount; i++)
                FlatNodes.Add(new SceneNode(NodeType.CloseChild, 0, Root));
        }

        public void FillScene(Assimp.Scene scene, List<Rigging.Bone> flatSkeleton, bool useSkeletonRoot)
        {
            Assimp.Node root = scene.RootNode;

            if (useSkeletonRoot)
                root = new Assimp.Node("skeleton_root");

            SceneNode curRoot = Root;
            SceneNode lastNode = Root;

            Assimp.Node curAssRoot = new(flatSkeleton[0].Name, root);
            Assimp.Node lastAssNode = curAssRoot;
            root.Children.Add(curAssRoot);

            for (int i = 1; i < FlatNodes.Count; i++)
            {
                SceneNode curNode = FlatNodes[i];

                if (curNode.Type == NodeType.OpenChild)
                {
                    curRoot = lastNode;
                    curAssRoot = lastAssNode;
                }
                else if (curNode.Type == NodeType.CloseChild)
                {
                    curRoot = curRoot.Parent;
                    curAssRoot = curAssRoot.Parent;
                }
                else if (curNode.Type == NodeType.Joint)
                {
                    Node currentAssimpNode = new(flatSkeleton[curNode.Index].Name, curAssRoot)
                    {
                        Transform = flatSkeleton[curNode.Index].TransformationMatrix.ToMatrix4x4()
                    };
                    curAssRoot.Children.Add(currentAssimpNode);

                    lastNode = curNode;
                    lastAssNode = currentAssimpNode;
                }
                else if (curNode.Type == NodeType.Terminator)
                    break;
                else
                {
                    Node curretnAssimpNode = new($"delete", curAssRoot);
                    curAssRoot.Children.Add(curretnAssimpNode);

                    lastNode = curNode;
                    lastAssNode = curretnAssimpNode;
                }
                Console.Write(".");
            }

            DeleteNodesRecursive(root);

            if (useSkeletonRoot)
            {
                scene.RootNode.Children.Add(root);
            }
            Console.Write("✓\n");
        }

        private void DeleteNodesRecursive(Node assimpNode)
        {
            if (assimpNode.Name == "delete")
            {
                for (int i = 0; i < assimpNode.Children.Count; i++)
                {
                    var newChild = new Node(assimpNode.Children[i].Name, assimpNode.Parent)
                    {
                        Transform = assimpNode.Children[i].Transform
                    };

                    for (int j = 0; j < assimpNode.Children[i].Children.Count; j++)
                        newChild.Children.Add(assimpNode.Children[i].Children[j]);

                    assimpNode.Children[i] = newChild;
                    assimpNode.Parent.Children.Add(assimpNode.Children[i]);
                }

                assimpNode.Parent.Children.Remove(assimpNode);
            }

            for (int i = 0; i < assimpNode.Children.Count; i++)
                DeleteNodesRecursive(assimpNode.Children[i]);
        }

        public void CorrectMaterialIndices(Scene scene, MAT3 materials)
        {
            foreach (SceneNode node in FlatNodes)
            {
                if (node.Type == NodeType.Shape)
                {
                    if (node.Index < scene.Meshes.Count)
                    {
                        int matIndex = node.Parent.Index;
                        scene.Meshes[node.Index].MaterialIndex = matIndex;
                    }
                }
            }
        }

        public void Write(ref EndianBinaryWriter writer, int packetCount, int vertexCount)
        {
            long start = writer.Position;

            writer.Write("INF1");
            writer.Write(0); // Placeholder for section size
            writer.Write((short)2);
            writer.Write((short)-1);

            writer.Write(packetCount); // Number of packets
            writer.Write(vertexCount); // Number of vertex positions
            writer.Write(0x18);

            foreach (SceneNode node in FlatNodes)
            {
                writer.Write((short)node.Type);
                writer.Write((short)node.Index);
            }

            writer.PadAlign(32);

            long end = writer.Position;
            long length = (end - start);

            writer.Seek((int)start + 4);
            writer.Write((int)length);
            writer.Seek((int)end);
        }

        public void DumpJson(string path)
        {
            foreach (SceneNode node in FlatNodes)
            {
                if (node.Parent != null)
                {
                    if (!node.Parent.Children.Contains(node))
                    {
                        node.Parent.Children.Add(node);
                    }
                }
            }

            File.WriteAllText("INF1.json", this.JsonSerialize());
        }

        public void LoadHierarchyFromJson(string path)
        {

            Console.WriteLine("Reading the Materials...");
            INF1 inf1 = JsonSerializer.Deserialize<INF1>(path);

            this.FlatNodes = new List<SceneNode>();
            this.Root = inf1.Root;
            Console.WriteLine("Is null? {0}", this.Root is null);
            Stack<SceneNode> nodestack = new Stack<SceneNode>();
            nodestack.Push(inf1.Root);

            while (nodestack.Count > 0)
            {
                SceneNode top = nodestack.Pop();
                this.FlatNodes.Add(top);
                Console.WriteLine("Node {0}", top is null);
                Console.WriteLine("Node Type {0} index {1}", top.Type, top.Index);
                for (int i = top.Children.Count - 1; i >= 0; i--)
                {
                    SceneNode node = top.Children[i];
                    if (node.Parent is null)
                    {
                        node.Parent = top;
                    }
                    nodestack.Push(node);
                }
            }
        }
    }
}
