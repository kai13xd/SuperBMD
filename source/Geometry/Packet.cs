namespace SuperBMD.Geometry
{
    public class Packet
    {
        public List<Primitive> Primitives { get; private set; }
        public List<int> MatrixIndices { get; private set; }

        private int m_Size;
        private int m_Offset;

        public Packet()
        {
            Primitives = new List<Primitive>();
            MatrixIndices = new List<int>();
        }

        public Packet(int size, int offset, int[] matrixIndices)
        {
            m_Size = size;
            m_Offset = offset;
            Primitives = new List<Primitive>();
            MatrixIndices = new List<int>();
            MatrixIndices.AddRange(matrixIndices);
        }

        public void ReadPrimitives(ref EndianBinaryReader reader, ShapeVertexDescriptor desc)
        {
            reader.Seek(m_Offset);

            while (true)
            {
                Primitive prim = new Primitive(ref reader, desc);
                Primitives.Add(prim);

                if (reader.PeekByte() == 0 || reader.Position >= m_Size + m_Offset)
                    break;
            }
        }
    }
}
