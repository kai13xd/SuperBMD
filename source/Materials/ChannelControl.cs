using SuperBMD.Materials;

namespace SuperBMD.Materials
{
    public struct ChannelControl : IEquatable<ChannelControl>
    {
        public bool Enable{ get; set; }
        public ColorSrc MaterialSrcColor{ get; set; }
        public LightId LitMask{ get; set; }
        public DiffuseFn DiffuseFunction{ get; set; }
        public J3DAttenuationFn AttenuationFunction{ get; set; }
        public ColorSrc AmbientSrcColor{ get; set; }

        public ChannelControl(bool enable, ColorSrc matSrcColor, LightId litMask, DiffuseFn diffFn, J3DAttenuationFn attenFn, ColorSrc ambSrcColor)
        {
            Enable = enable;
            MaterialSrcColor = matSrcColor;
            LitMask = litMask;
            DiffuseFunction = diffFn;
            AttenuationFunction = attenFn;
            AmbientSrcColor = ambSrcColor;
        }

        public ChannelControl(ref EndianBinaryReader reader)
        {
            Enable = reader.ReadBool();
            MaterialSrcColor = (ColorSrc)reader.ReadByte();
            LitMask = (LightId)reader.ReadByte();
            DiffuseFunction = (DiffuseFn)reader.ReadByte();
            AttenuationFunction = (J3DAttenuationFn)reader.ReadByte();
            AmbientSrcColor = (ColorSrc)reader.ReadByte();

            reader.Skip(2);
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write(Enable);
            writer.Write((byte)MaterialSrcColor);
            writer.Write((byte)LitMask);
            writer.Write((byte)DiffuseFunction);
            writer.Write((byte)AttenuationFunction);
            writer.Write((byte)AmbientSrcColor);

            writer.Write((short)-1);
        }

        public static bool operator ==(ChannelControl left, ChannelControl right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChannelControl left, ChannelControl right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            int hash = Convert.ToInt32(Enable);
            hash ^= (int)MaterialSrcColor << 4;
            hash ^= (int)LitMask << 4;
            hash ^= (int)DiffuseFunction << 6;
            hash ^= (int)AttenuationFunction << 5;
            hash ^= (int)AmbientSrcColor << 2;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ChannelControl))
                return false;
            else
                return Equals((ChannelControl)obj);
        }

        public bool Equals(ChannelControl other)
        {
            return Enable == other.Enable &&
                MaterialSrcColor == other.MaterialSrcColor &&
                LitMask == other.LitMask &&
                DiffuseFunction == other.DiffuseFunction &&
                AttenuationFunction == other.AttenuationFunction &&
                AmbientSrcColor == other.AmbientSrcColor;
        }
    }
}
