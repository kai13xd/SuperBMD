using SuperBMD.Geometry;
using SuperBMD.Rigging;

namespace SuperBMD.Geometry
{
    public class Vertex
    {
        public uint PositionMatrixIDxIndex { get; private set; }
        public uint PositionIndex { get; private set; }
        public uint NormalIndex { get; private set; }
        public uint Color0Index { get; private set; }
        public uint Color1Index { get; private set; }
        public uint TexCoord0Index { get; private set; }
        public uint TexCoord1Index { get; private set; }
        public uint TexCoord2Index { get; private set; }
        public uint TexCoord3Index { get; private set; }
        public uint TexCoord4Index { get; private set; }
        public uint TexCoord5Index { get; private set; }
        public uint TexCoord6Index { get; private set; }
        public uint TexCoord7Index { get; private set; }

        public uint Tex0MtxIndex { get; private set; }
        public uint Tex1MtxIndex { get; private set; }
        public uint Tex2MtxIndex { get; private set; }
        public uint Tex3MtxIndex { get; private set; }
        public uint Tex4MtxIndex { get; private set; }
        public uint Tex5MtxIndex { get; private set; }
        public uint Tex6MtxIndex { get; private set; }
        public uint Tex7MtxIndex { get; private set; }

        public uint PositionMatrixIndex { get; set; }
        public uint NormalMatrixIndex { get; set; }
        public uint NBTIndex { get; set; }

        public Weight VertexWeight { get; private set; }

        public Vertex()
        {
            PositionMatrixIDxIndex = uint.MaxValue;
            PositionIndex = uint.MaxValue;
            NormalIndex = uint.MaxValue;
            Color0Index = uint.MaxValue;
            Color1Index = uint.MaxValue;
            TexCoord0Index = uint.MaxValue;
            TexCoord1Index = uint.MaxValue;
            TexCoord2Index = uint.MaxValue;
            TexCoord3Index = uint.MaxValue;
            TexCoord4Index = uint.MaxValue;
            TexCoord5Index = uint.MaxValue;
            TexCoord6Index = uint.MaxValue;
            TexCoord7Index = uint.MaxValue;

            Tex0MtxIndex = uint.MaxValue;
            Tex1MtxIndex = uint.MaxValue;
            Tex2MtxIndex = uint.MaxValue;
            Tex3MtxIndex = uint.MaxValue;
            Tex4MtxIndex = uint.MaxValue;
            Tex5MtxIndex = uint.MaxValue;
            Tex6MtxIndex = uint.MaxValue;
            Tex7MtxIndex = uint.MaxValue;

            VertexWeight = new Weight();
        }

        public Vertex(Vertex src)
        {
            // The position matrix index index is specific to the packet the vertex is in.
            // So if copying a vertex across different packets, this value will be wrong and it needs to be recalculated manually.
            PositionMatrixIDxIndex = src.PositionMatrixIDxIndex;

            PositionIndex = src.PositionIndex;
            NormalIndex = src.NormalIndex;
            Color0Index = src.Color0Index;
            Color1Index = src.Color1Index;
            TexCoord0Index = src.TexCoord0Index;
            TexCoord1Index = src.TexCoord1Index;
            TexCoord2Index = src.TexCoord2Index;
            TexCoord3Index = src.TexCoord3Index;
            TexCoord4Index = src.TexCoord4Index;
            TexCoord5Index = src.TexCoord5Index;
            TexCoord6Index = src.TexCoord6Index;
            TexCoord7Index = src.TexCoord7Index;

            Tex0MtxIndex = src.Tex0MtxIndex;
            Tex1MtxIndex = src.Tex1MtxIndex;
            Tex2MtxIndex = src.Tex2MtxIndex;
            Tex3MtxIndex = src.Tex3MtxIndex;
            Tex4MtxIndex = src.Tex4MtxIndex;
            Tex5MtxIndex = src.Tex5MtxIndex;
            Tex6MtxIndex = src.Tex6MtxIndex;
            Tex7MtxIndex = src.Tex7MtxIndex;

            VertexWeight = src.VertexWeight;
        }

        public uint GetAttributeIndex(VertexAttribute attribute)
        {
            switch (attribute)
            {
                case VertexAttribute.PositionMatrixIdx:
                    return PositionMatrixIDxIndex;
                case VertexAttribute.Position:
                    return PositionIndex;
                case VertexAttribute.Normal:
                    return NormalIndex;
                case VertexAttribute.ColorChannel0:
                    return Color0Index;
                case VertexAttribute.ColorChannel1:
                    return Color1Index;
                case VertexAttribute.TexCoord0:
                    return TexCoord0Index;
                case VertexAttribute.TexCoord1:
                    return TexCoord1Index;
                case VertexAttribute.TexCoord2:
                    return TexCoord2Index;
                case VertexAttribute.TexCoord3:
                    return TexCoord3Index;
                case VertexAttribute.TexCoord4:
                    return TexCoord4Index;
                case VertexAttribute.TexCoord5:
                    return TexCoord5Index;
                case VertexAttribute.TexCoord6:
                    return TexCoord6Index;
                case VertexAttribute.TexCoord7:
                    return TexCoord7Index;
                case VertexAttribute.Tex0Mtx:
                    return Tex0MtxIndex;
                case VertexAttribute.Tex1Mtx:
                    return Tex1MtxIndex;
                case VertexAttribute.Tex2Mtx:
                    return Tex2MtxIndex;
                case VertexAttribute.Tex3Mtx:
                    return Tex3MtxIndex;
                case VertexAttribute.Tex4Mtx:
                    return Tex4MtxIndex;
                case VertexAttribute.Tex5Mtx:
                    return Tex5MtxIndex;
                case VertexAttribute.Tex6Mtx:
                    return Tex6MtxIndex;
                case VertexAttribute.Tex7Mtx:
                    return Tex7MtxIndex;
                default:
                    throw new ArgumentException(String.Format("attribute {0}", attribute));
            }
        }

        public void SetAttributeIndex(VertexAttribute attribute, uint index)
        {
            switch (attribute)
            {
                case VertexAttribute.PositionMatrixIdx:
                    PositionMatrixIDxIndex = index;
                    break;
                case VertexAttribute.Position:
                    PositionIndex = index;
                    break;
                case VertexAttribute.Normal:
                    NormalIndex = index;
                    break;
                case VertexAttribute.ColorChannel0:
                    Color0Index = index;
                    break;
                case VertexAttribute.ColorChannel1:
                    Color1Index = index;
                    break;
                case VertexAttribute.TexCoord0:
                    TexCoord0Index = index;
                    break;
                case VertexAttribute.TexCoord1:
                    TexCoord1Index = index;
                    break;
                case VertexAttribute.TexCoord2:
                    TexCoord2Index = index;
                    break;
                case VertexAttribute.TexCoord3:
                    TexCoord3Index = index;
                    break;
                case VertexAttribute.TexCoord4:
                    TexCoord4Index = index;
                    break;
                case VertexAttribute.TexCoord5:
                    TexCoord5Index = index;
                    break;
                case VertexAttribute.TexCoord6:
                    TexCoord6Index = index;
                    break;
                case VertexAttribute.TexCoord7:
                    TexCoord7Index = index;
                    break;
                case VertexAttribute.Tex0Mtx:
                    Tex0MtxIndex = index;
                    break;
                case VertexAttribute.Tex1Mtx:
                    Tex1MtxIndex = index;
                    break;
                case VertexAttribute.Tex2Mtx:
                    Tex2MtxIndex = index;
                    break;
                case VertexAttribute.Tex3Mtx:
                    Tex3MtxIndex = index;
                    break;
                case VertexAttribute.Tex4Mtx:
                    Tex4MtxIndex = index;
                    break;
                case VertexAttribute.Tex5Mtx:
                    Tex5MtxIndex = index;
                    break;
                case VertexAttribute.Tex6Mtx:
                    Tex6MtxIndex = index;
                    break;
                case VertexAttribute.Tex7Mtx:
                    Tex7MtxIndex = index;
                    break;
                case VertexAttribute.NBT:
                    NBTIndex = index;
                    break;
                default:
                    throw new ArgumentException(String.Format("attribute {0}", attribute));
            }
        }

        public void SetWeight(Weight weight)
        {
            VertexWeight = weight;
        }

        public void Write(ref EndianBinaryWriter writer, ShapeVertexDescriptor desc)
        {
            if (desc.CheckAttribute(VertexAttribute.PositionMatrixIdx))
            {
                WriteAttributeIndex(ref writer, PositionMatrixIDxIndex * 3, desc.Attributes[VertexAttribute.PositionMatrixIdx].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.Position))
            {
                WriteAttributeIndex(ref writer, PositionIndex, desc.Attributes[VertexAttribute.Position].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.Normal))
            {
                WriteAttributeIndex(ref writer, NormalIndex, desc.Attributes[VertexAttribute.Normal].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.ColorChannel0))
            {
                WriteAttributeIndex(ref writer, Color0Index, desc.Attributes[VertexAttribute.ColorChannel0].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.ColorChannel1))
            {
                WriteAttributeIndex(ref writer, Color1Index, desc.Attributes[VertexAttribute.ColorChannel1].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord0))
            {
                WriteAttributeIndex(ref writer, TexCoord0Index, desc.Attributes[VertexAttribute.TexCoord0].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord1))
            {
                WriteAttributeIndex(ref writer, TexCoord1Index, desc.Attributes[VertexAttribute.TexCoord1].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord2))
            {
                WriteAttributeIndex(ref writer, TexCoord2Index, desc.Attributes[VertexAttribute.TexCoord2].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord3))
            {
                WriteAttributeIndex(ref writer, TexCoord3Index, desc.Attributes[VertexAttribute.TexCoord3].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord4))
            {
                WriteAttributeIndex(ref writer, TexCoord4Index, desc.Attributes[VertexAttribute.TexCoord4].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord5))
            {
                WriteAttributeIndex(ref writer, TexCoord5Index, desc.Attributes[VertexAttribute.TexCoord5].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord6))
            {
                WriteAttributeIndex(ref writer, TexCoord6Index, desc.Attributes[VertexAttribute.TexCoord6].Item1);
            }

            if (desc.CheckAttribute(VertexAttribute.TexCoord7))
            {
                WriteAttributeIndex(ref writer, TexCoord7Index, desc.Attributes[VertexAttribute.TexCoord7].Item1);
            }
        }

        private void WriteAttributeIndex(ref EndianBinaryWriter writer, uint value, VertexInputType type)
        {
            switch (type)
            {
                case VertexInputType.Direct:
                case VertexInputType.Index8:
                    writer.Write((byte)value);
                    break;
                case VertexInputType.Index16:
                    writer.Write((short)value);
                    break;
                case VertexInputType.None:
                default:
                    throw new ArgumentException("vertex input type");
            }
        }
    }
}
