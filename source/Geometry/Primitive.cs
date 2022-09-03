using SuperBMD.Geometry;

namespace SuperBMD.Geometry
{
    public class Primitive
    {
        public GXPrimitiveType PrimitiveType { get; private set; }
        public List<Vertex> Vertices { get; private set; }

        public Primitive()
        {
            PrimitiveType = GXPrimitiveType.Lines;
            Vertices = new List<Vertex>();
        }

        public Primitive(GXPrimitiveType primType)
        {
            PrimitiveType = primType;
            Vertices = new List<Vertex>();
        }

        public Primitive(ref EndianBinaryReader reader, ShapeVertexDescriptor activeAttribs)
        {
            Vertices = new List<Vertex>();

            PrimitiveType = (GXPrimitiveType)(reader.ReadByte() & 0xF8);
            int vertCount = reader.ReadShort();

            for (int i = 0; i < vertCount; i++)
            {
                Vertex vert = new Vertex();

                foreach (GXVertexAttribute attrib in activeAttribs.Attributes.Keys)
                {
                    switch (activeAttribs.GetAttributeType(attrib))
                    {
                        case VertexInputType.Direct:
                            vert.SetAttributeIndex(attrib, attrib == GXVertexAttribute.PositionMatrixIdx ? (uint)(reader.ReadByte() / 3) : reader.ReadByte());
                            break;
                        case VertexInputType.Index8:
                            vert.SetAttributeIndex(attrib, reader.ReadByte());
                            break;
                        case VertexInputType.Index16:
                            vert.SetAttributeIndex(attrib, reader.ReadUShort());
                            break;
                        case VertexInputType.None:
                            throw new Exception("Found \"None\" as vertex input type in Primitive(ref reader, activeAttribs)!");
                    }
                }

                Vertices.Add(vert);
            }
        }

        public void Write(ref EndianBinaryWriter writer, ShapeVertexDescriptor desc)
        {
            writer.Write((byte)PrimitiveType);
            writer.Write((short)Vertices.Count);

            foreach (Vertex vert in Vertices)
                vert.Write(ref writer, desc);
        }
    }
}
