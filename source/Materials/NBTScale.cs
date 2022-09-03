﻿using SuperBMD.Util;

namespace SuperBMD.Materials
{
    public struct NBTScale : IEquatable<NBTScale>
    {
        public byte Unknown1;

        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Scale;

        public NBTScale(byte unk1, Vector3 scale)
        {
            Unknown1 = unk1;
            Scale = scale;
        }

        public NBTScale(ref EndianBinaryReader reader)
        {
            Unknown1 = reader.ReadByte();
            reader.Skip(3);
            Scale = new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write(Unknown1);
            writer.Write((sbyte)-1);
            writer.Write((short)-1);
            writer.Write(Scale);
        }

        public static bool operator ==(NBTScale left, NBTScale right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NBTScale left, NBTScale right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            int hash = Unknown1;
            hash ^= Scale.GetHashCode() << 4;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NBTScale))
                return false;
            else
                return Equals((NBTScale)obj);
        }

        public bool Equals(NBTScale other)
        {
            return Unknown1 == other.Unknown1 &&
                Scale == other.Scale;
        }
    }
}
