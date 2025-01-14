﻿namespace SuperBMD.Materials
{
    public struct Fog : IEquatable<Fog>
    {
        public byte Type { get; set; }
        public bool Enable { get; set; }
        public ushort Center { get; set; }
        public float StartZ { get; set; }
        public float EndZ { get; set; }
        public float NearZ { get; set; }
        public float FarZ { get; set; }
        public Color Color { get; set; }
        public float[] RangeAdjustmentTable { get; set; }

        public Fog(byte type, bool enable, ushort center, float startZ, float endZ, float nearZ, float farZ, Color color, float[] rangeAdjust)
        {
            Type = type;
            Enable = enable;
            Center = center;
            StartZ = startZ;
            EndZ = endZ;
            NearZ = nearZ;
            FarZ = farZ;
            Color = color;
            RangeAdjustmentTable = rangeAdjust;
        }

        public Fog(ref EndianBinaryReader reader)
        {
            RangeAdjustmentTable = new float[10];

            Type = reader.ReadByte();
            Enable = reader.ReadBool();
            Center = reader.ReadUShort();
            StartZ = reader.ReadFloat();
            EndZ = reader.ReadFloat();
            NearZ = reader.ReadFloat();
            FarZ = reader.ReadFloat();
            Color = new Color((float)reader.ReadByte() / 255, (float)reader.ReadByte() / 255, (float)reader.ReadByte() / 255, (float)reader.ReadByte() / 255);

            for (int i = 0; i < 10; i++)
            {
                ushort inVal = reader.ReadUShort();
                RangeAdjustmentTable[i] = (float)inVal / 256;
            }
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(Enable);
            writer.Write(Center);
            writer.Write(StartZ);
            writer.Write(EndZ);
            writer.Write(NearZ);
            writer.Write(FarZ);
            writer.Write(Color);

            for (int i = 0; i < 10; i++)
                writer.Write((ushort)(RangeAdjustmentTable[i] * 256));
        }

        public static bool operator ==(Fog left, Fog right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Fog left, Fog right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            int hash = Type;
            hash ^= Convert.ToInt32(Enable);
            hash ^= Center << 7;
            hash ^= StartZ.GetHashCode() << 4;
            hash ^= EndZ.GetHashCode() << 4;
            hash ^= NearZ.GetHashCode() << 3;
            hash ^= FarZ.GetHashCode() << 5;
            hash ^= Color.GetHashCode() << 6;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fog))
                return false;
            else
                return Equals((Fog)obj);
        }

        public bool Equals(Fog other)
        {
            return Type == other.Type &&
                Enable == other.Enable &&
                Center == other.Center &&
                StartZ == other.StartZ &&
                EndZ == other.EndZ &&
                NearZ == other.NearZ &&
                FarZ == other.FarZ &&
                Color == other.Color;
        }
    }
}
