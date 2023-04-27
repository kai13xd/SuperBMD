using SuperBMD.Geometry;

namespace SuperBMD.Geometry
{
    public class ShapeVertexDescriptor
    {
        public SortedDictionary<VertexAttribute, Tuple<VertexInputType, int>> Attributes { get; private set; }

        public ShapeVertexDescriptor()
        {
            Attributes = new SortedDictionary<VertexAttribute, Tuple<VertexInputType, int>>();
        }

        public ShapeVertexDescriptor(ref EndianBinaryReader reader, int offset)
        {
            Attributes = new SortedDictionary<VertexAttribute, Tuple<VertexInputType, int>>();
            reader.Seek(offset);

            int index = 0;
            VertexAttribute attrib = (VertexAttribute)reader.ReadInt();

            while (attrib != VertexAttribute.Null)
            {
                Attributes.Add(attrib, new Tuple<VertexInputType, int>((VertexInputType)reader.ReadInt(), index));

                index++;
                attrib = (VertexAttribute)reader.ReadInt();
            }
        }

        public bool CheckAttribute(VertexAttribute attribute)
        {
            return Attributes.ContainsKey(attribute);
        }

        public void SetAttribute(VertexAttribute attribute, VertexInputType inputType, int vertexIndex)
        {
            if (CheckAttribute(attribute))
                throw new Exception($"Attribute \"{attribute}\" is already in the vertex descriptor!");

            Attributes.Add(attribute, new Tuple<VertexInputType, int>(inputType, vertexIndex));
        }

        public List<VertexAttribute> GetActiveAttributes()
        {
            List<VertexAttribute> attribs = new List<VertexAttribute>(Attributes.Keys);
            return attribs;
        }

        public int GetAttributeIndex(VertexAttribute attribute)
        {
            if (CheckAttribute(attribute))
                return Attributes[attribute].Item2;
            else
                throw new ArgumentException("attribute");
        }

        public VertexInputType GetAttributeType(VertexAttribute attribute)
        {
            if (CheckAttribute(attribute))
                return Attributes[attribute].Item1;
            else
                throw new ArgumentException("attribute");
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            if (CheckAttribute(VertexAttribute.PositionMatrixIdx))
            {
                writer.Write((int)VertexAttribute.PositionMatrixIdx);
                writer.Write((int)Attributes[VertexAttribute.PositionMatrixIdx].Item1);
            }

            if (CheckAttribute(VertexAttribute.Position))
            {
                writer.Write((int)VertexAttribute.Position);
                writer.Write((int)Attributes[VertexAttribute.Position].Item1);
            }

            if (CheckAttribute(VertexAttribute.Normal))
            {
                writer.Write((int)VertexAttribute.Normal);
                writer.Write((int)Attributes[VertexAttribute.Normal].Item1);
            }

            if (CheckAttribute(VertexAttribute.ColorChannel0))
            {
                writer.Write((int)VertexAttribute.ColorChannel0);
                writer.Write((int)Attributes[VertexAttribute.ColorChannel0].Item1);
            }

            if (CheckAttribute(VertexAttribute.ColorChannel1))
            {
                writer.Write((int)VertexAttribute.ColorChannel1);
                writer.Write((int)Attributes[VertexAttribute.ColorChannel1].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord0))
            {
                writer.Write((int)VertexAttribute.TexCoord0);
                writer.Write((int)Attributes[VertexAttribute.TexCoord0].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord1))
            {
                writer.Write((int)VertexAttribute.TexCoord1);
                writer.Write((int)Attributes[VertexAttribute.TexCoord1].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord2))
            {
                writer.Write((int)VertexAttribute.TexCoord2);
                writer.Write((int)Attributes[VertexAttribute.TexCoord2].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord3))
            {
                writer.Write((int)VertexAttribute.TexCoord3);
                writer.Write((int)Attributes[VertexAttribute.TexCoord3].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord4))
            {
                writer.Write((int)VertexAttribute.TexCoord4);
                writer.Write((int)Attributes[VertexAttribute.TexCoord4].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord5))
            {
                writer.Write((int)VertexAttribute.TexCoord5);
                writer.Write((int)Attributes[VertexAttribute.TexCoord5].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord6))
            {
                writer.Write((int)VertexAttribute.TexCoord6);
                writer.Write((int)Attributes[VertexAttribute.TexCoord6].Item1);
            }

            if (CheckAttribute(VertexAttribute.TexCoord7))
            {
                writer.Write((int)VertexAttribute.TexCoord7);
                writer.Write((int)Attributes[VertexAttribute.TexCoord7].Item1);
            }
            // Null attribute
            writer.Write(255);
            writer.Write(0);
        }

        public int CalculateStride()
        {
            int stride = 0;

            foreach (Tuple<VertexInputType, int> tup in Attributes.Values)
            {
                switch (tup.Item1)
                {
                    case VertexInputType.Index16:
                        stride += 2;
                        break;
                    case VertexInputType.Index8:
                    case VertexInputType.Direct: // HACK: BMD usually uses this only for PositionMatrixIdx, which uses a byte, but we should really use the VAT/VTX1 to get the actual stride
                        stride += 1;
                        break;
                    case VertexInputType.None:
                        break;
                    default:
                        throw new Exception($"Unknown vertex input type\"{tup.Item1}\"");
                }
            }

            return stride;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(ShapeVertexDescriptor))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            ShapeVertexDescriptor compObj = obj as ShapeVertexDescriptor;

            if (Attributes.Count != compObj.Attributes.Count)
                return false;

            for (int i = 0; i < Attributes.Count; i++)
            {
                KeyValuePair<VertexAttribute, Tuple<VertexInputType, int>> thisPair = Attributes.ElementAt(i);
                KeyValuePair<VertexAttribute, Tuple<VertexInputType, int>> otherPair = compObj.Attributes.ElementAt(i);

                if (thisPair.Key != otherPair.Key)
                    return false;

                if (thisPair.Value.Item1 != otherPair.Value.Item1)
                    return false;

                if (thisPair.Value.Item2 != otherPair.Value.Item2)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int output = 0;

            foreach (KeyValuePair<VertexAttribute, Tuple<VertexInputType, int>> pair in Attributes)
            {
                output = (int)pair.Key + (int)pair.Value.Item1 + pair.Value.Item2;
            }

            return output;
        }

        public static bool operator ==(ShapeVertexDescriptor left, ShapeVertexDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShapeVertexDescriptor left, ShapeVertexDescriptor right)
        {
            return !left.Equals(right);
        }
    }
}
