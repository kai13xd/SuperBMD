namespace SuperBMD.Materials
{
    public class IndirectTexScale : IEquatable<IndirectTexScale>
    {
        /// <summary>
        /// Scale value for the source texture coordinates' S (U) component
        /// </summary>
        public IndirectScale ScaleS { get; private set; }
        /// <summary>
        /// Scale value for the source texture coordinates' T (V) component
        /// </summary>
        public IndirectScale ScaleT { get; private set; }

        [JsonConstructor]
        public IndirectTexScale()
        {
            ScaleS = IndirectScale.ITS_1;
            ScaleT = IndirectScale.ITS_1;
        }
        public IndirectTexScale(IndirectScale s, IndirectScale t)
        {
            ScaleS = s;
            ScaleT = t;
        }

        public IndirectTexScale(ref EndianBinaryReader reader)
        {
            ScaleS = (IndirectScale)reader.ReadByte();
            ScaleT = (IndirectScale)reader.ReadByte();
            reader.Skip(2);
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write((byte)ScaleS);
            writer.Write((byte)ScaleT);
            writer.Write((short)-1);
        }

        public static bool operator ==(IndirectTexScale left, IndirectTexScale right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndirectTexScale left, IndirectTexScale right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            return ((int)ScaleS << 5) ^ ((int)ScaleT << 2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IndirectTexScale))
                return false;
            else
                return Equals((IndirectTexScale)obj);
        }

        public bool Equals(IndirectTexScale other)
        {
            return ScaleS == other.ScaleS &&
                ScaleT == other.ScaleT;
        }
    }
}
