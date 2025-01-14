﻿namespace SuperBMD.Materials
{
    public struct IndirectTexMatrix : IEquatable<IndirectTexMatrix>
    {
        /// <summary>
        /// The floats that make up the matrix
        /// </summary>
        [JsonConverter(typeof(Matrix2x3Converter))]
        public Matrix2x3 Matrix { get; set; }
        /// <summary>
        /// The exponent (of 2) to multiply the matrix by
        /// </summary>
        public byte Exponent { get; set; }

        public IndirectTexMatrix(Matrix2x3 matrix, byte exponent)
        {
            Matrix = matrix;

            Exponent = exponent;
        }

        public IndirectTexMatrix(ref EndianBinaryReader reader)
        {
            Matrix = new Matrix2x3(
                reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(),
                reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());

            Exponent = reader.ReadByte();

            reader.Skip(3);
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write(Matrix.M11);
            writer.Write(Matrix.M12);
            writer.Write(Matrix.M13);

            writer.Write(Matrix.M21);
            writer.Write(Matrix.M22);
            writer.Write(Matrix.M23);

            writer.Write((byte)Exponent);
            writer.Write((sbyte)-1);
            writer.Write((short)-1);
        }

        public static bool operator ==(IndirectTexMatrix left, IndirectTexMatrix right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndirectTexMatrix left, IndirectTexMatrix right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            int hash = Exponent;
            hash ^= Matrix.GetHashCode() << 3;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj is not IndirectTexMatrix)
                return false;
            else
                return Equals((IndirectTexMatrix)obj);
        }

        public bool Equals(IndirectTexMatrix other)
        {
            return Exponent == other.Exponent &&
                Matrix == other.Matrix;
        }
    }
}
