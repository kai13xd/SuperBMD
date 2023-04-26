using SuperBMD.Materials;

namespace SuperBMD.Materials
{
    public struct TexCoordGen : IEquatable<TexCoordGen>
    {
        public TexGenType Type { get; set; }
        public TexGenSrc Source { get; set; }
        public TexMatrixType TexMatrixSource { get; set; }

        public TexCoordGen(TexGenType type, TexGenSrc src, TexMatrixType mtrx)
        {
            Type = type;
            Source = src;
            TexMatrixSource = mtrx;
        }

        public TexCoordGen(ref EndianBinaryReader reader)
        {
            Type = (TexGenType)reader.ReadByte();
            Source = (TexGenSrc)reader.ReadByte();
            TexMatrixSource = (TexMatrixType)reader.ReadByte();

            reader.Skip();
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write((byte)Source);
            writer.Write((byte)TexMatrixSource);

            // Pad entry to 4 bytes
            writer.Write((sbyte)-1);
        }

        public static bool operator ==(TexCoordGen left, TexCoordGen right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TexCoordGen left, TexCoordGen right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            int hash = (int)Type;
            hash ^= (int)Source << 3;
            hash ^= (int)TexMatrixSource << 4;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TexCoordGen))
                return false;
            else
                return Equals((TexCoordGen)obj);
        }

        public bool Equals(TexCoordGen other)
        {
            return Type == other.Type &&
                Source == other.Source &&
                TexMatrixSource == other.TexMatrixSource;
        }
    }
}
