using SuperBMD.Scenegraph.Enums;

namespace SuperBMD.Scenegraph
{
    public class SceneNode
    {
        [JsonIgnore]
        public SceneNode Parent { get; set; }

        public NodeType Type { get; set; }
        public int Index { get; set; }
        public List<SceneNode> Children { get; set; }

        public SceneNode()
        {
            Parent = null;
            Type = NodeType.Joint;
            Index = 0;
            Children = new List<SceneNode>();

        }

        public SceneNode(ref EndianBinaryReader reader, SceneNode parent)
        {
            Children = new List<SceneNode>();
            Parent = parent;

            Type = (NodeType)reader.ReadShort();
            Index = reader.ReadShort();
        }

        public SceneNode(NodeType type, int index, SceneNode parent)
        {
            Type = type;
            Index = index;
            Parent = parent;

            if (Parent != null)
                Parent.Children.Add(this);

            Children = new List<SceneNode>();
        }

        public void SetParent(SceneNode parent)
        {
            Parent = parent;
        }

        public override string ToString()
        {
            return $"{Type} : {Index}";
        }
    }
}
