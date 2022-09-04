namespace SuperBMD.Materials
{
    public struct TevSwapModeTable : IEquatable<TevSwapModeTable>
    {
        public byte R{ get; set; }
        public byte G{ get; set; }
        public byte B{ get; set; }
        public byte A{ get; set; }

        public TevSwapModeTable(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public TevSwapModeTable(ref EndianBinaryReader reader)
        {
            R = reader.ReadByte();
            G = reader.ReadByte();
            B = reader.ReadByte();
            A = reader.ReadByte();
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write(R);
            writer.Write(G);
            writer.Write(B);
            writer.Write(A);
        }

        public static bool operator ==(TevSwapModeTable left, TevSwapModeTable right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TevSwapModeTable left, TevSwapModeTable right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            int hash = R;
            hash ^= G << 3;
            hash ^= B << 2;
            hash ^= A << 6;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TevSwapModeTable))
                return false;
            else
                return Equals((TevSwapModeTable)obj);
        }

        public bool Equals(TevSwapModeTable other)
        {
            return R == other.R &&
                G == other.G &&
                B == other.B &&
                A == other.A;
        }
    }
}
