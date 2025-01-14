﻿using SuperBMD.Materials;

namespace SuperBMD.Materials
{
    public struct TevStageParameters
    {
        public CombineColorInput ColorInA { get; set; }
        public CombineColorInput ColorInB { get; set; }
        public CombineColorInput ColorInC { get; set; }
        public CombineColorInput ColorInD { get; set; }

        public TevOp ColorOp { get; set; }
        public TevBias ColorBias { get; set; }
        public TevScale ColorScale { get; set; }
        public bool ColorClamp { get; set; }
        public TevRegisterId ColorRegId { get; set; }

        public CombineAlphaInput AlphaInA { get; set; }
        public CombineAlphaInput AlphaInB { get; set; }
        public CombineAlphaInput AlphaInC { get; set; }
        public CombineAlphaInput AlphaInD { get; set; }

        public TevOp AlphaOp { get; set; }
        public TevBias AlphaBias { get; set; }
        public TevScale AlphaScale { get; set; }
        public bool AlphaClamp { get; set; }
        public TevRegisterId AlphaRegId { get; set; }
    }

    public struct TevStage : IEquatable<TevStage>
    {
        public CombineColorInput ColorInA { get; set; }
        public CombineColorInput ColorInB { get; set; }
        public CombineColorInput ColorInC { get; set; }
        public CombineColorInput ColorInD { get; set; }

        public TevOp ColorOp { get; set; }
        public TevBias ColorBias { get; set; }
        public TevScale ColorScale { get; set; }
        public bool ColorClamp { get; set; }
        public TevRegisterId ColorRegId { get; set; }

        public CombineAlphaInput AlphaInA { get; set; }
        public CombineAlphaInput AlphaInB { get; set; }
        public CombineAlphaInput AlphaInC { get; set; }
        public CombineAlphaInput AlphaInD { get; set; }

        public TevOp AlphaOp { get; set; }
        public TevBias AlphaBias { get; set; }
        public TevScale AlphaScale { get; set; }
        public bool AlphaClamp { get; set; }
        public TevRegisterId AlphaRegId { get; set; }

        public TevStage(ref EndianBinaryReader reader)
        {
            reader.Skip();

            ColorInA = (CombineColorInput)reader.ReadByte();
            ColorInB = (CombineColorInput)reader.ReadByte();
            ColorInC = (CombineColorInput)reader.ReadByte();
            ColorInD = (CombineColorInput)reader.ReadByte();

            ColorOp = (TevOp)reader.ReadByte();
            ColorBias = (TevBias)reader.ReadByte();
            ColorScale = (TevScale)reader.ReadByte();
            ColorClamp = reader.ReadBool();
            ColorRegId = (TevRegisterId)reader.ReadByte();

            AlphaInA = (CombineAlphaInput)reader.ReadByte();
            AlphaInB = (CombineAlphaInput)reader.ReadByte();
            AlphaInC = (CombineAlphaInput)reader.ReadByte();
            AlphaInD = (CombineAlphaInput)reader.ReadByte();

            AlphaOp = (TevOp)reader.ReadByte();
            AlphaBias = (TevBias)reader.ReadByte();
            AlphaScale = (TevScale)reader.ReadByte();
            AlphaClamp = reader.ReadBool();
            AlphaRegId = (TevRegisterId)reader.ReadByte();

            reader.Skip();
        }

        public TevStage(TevStageParameters parameters)
        {
            ColorInA = parameters.ColorInA;
            ColorInB = parameters.ColorInB;
            ColorInC = parameters.ColorInC;
            ColorInD = parameters.ColorInD;

            ColorOp = parameters.ColorOp;
            ColorBias = parameters.ColorBias;
            ColorScale = parameters.ColorScale;
            ColorClamp = parameters.ColorClamp;
            ColorRegId = parameters.ColorRegId;

            AlphaInA = parameters.AlphaInA;
            AlphaInB = parameters.AlphaInB;
            AlphaInC = parameters.AlphaInC;
            AlphaInD = parameters.AlphaInD;

            AlphaOp = parameters.AlphaOp;
            AlphaBias = parameters.AlphaBias;
            AlphaScale = parameters.AlphaScale;
            AlphaClamp = parameters.AlphaClamp;
            AlphaRegId = parameters.AlphaRegId;
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            writer.Write((sbyte)-1);

            writer.Write((byte)ColorInA);
            writer.Write((byte)ColorInB);
            writer.Write((byte)ColorInC);
            writer.Write((byte)ColorInD);

            writer.Write((byte)ColorOp);
            writer.Write((byte)ColorBias);
            writer.Write((byte)ColorScale);
            writer.Write(ColorClamp);
            writer.Write((byte)ColorRegId);

            writer.Write((byte)AlphaInA);
            writer.Write((byte)AlphaInB);
            writer.Write((byte)AlphaInC);
            writer.Write((byte)AlphaInD);

            writer.Write((byte)AlphaOp);
            writer.Write((byte)AlphaBias);
            writer.Write((byte)AlphaScale);
            writer.Write(AlphaClamp);
            writer.Write((byte)AlphaRegId);

            writer.Write((sbyte)-1);
        }

        public override string ToString()
        {
            string ret = "";

            ret += $"Color In A: {ColorInA}\n";
            ret += $"Color In B: {ColorInB}\n";
            ret += $"Color In C: {ColorInC}\n";
            ret += $"Color In D: {ColorInD}\n";

            ret += '\n';

            ret += $"Color Op: {ColorOp}\n";
            ret += $"Color Bias: {ColorBias}\n";
            ret += $"Color Scale: {ColorScale}\n";
            ret += $"Color Clamp: {ColorClamp}\n";
            ret += $"Color Reg ID: {ColorRegId}\n";

            ret += '\n';

            ret += $"Alpha In A: {AlphaInA}\n";
            ret += $"Alpha In B: {AlphaInB}\n";
            ret += $"Alpha In C: {AlphaInC}\n";
            ret += $"Alpha In D: {AlphaInD}\n";

            ret += '\n';

            ret += $"Alpha Op: {AlphaOp}\n";
            ret += $"Alpha Bias: {AlphaBias}\n";
            ret += $"Alpha Scale: {AlphaScale}\n";
            ret += $"Alpha Clamp: {AlphaClamp}\n";
            ret += $"Alpha Reg ID: {AlphaRegId}\n";

            ret += '\n';

            return ret;
        }

        public static bool operator ==(TevStage left, TevStage right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TevStage left, TevStage right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            int hash = (int)ColorInA;
            hash ^= (int)ColorInB << 3;
            hash ^= (int)ColorInC << 3;
            hash ^= (int)ColorInD << 5;

            hash ^= (int)ColorOp << 2;
            hash ^= (int)ColorBias << 6;
            hash ^= (int)ColorScale << 5;
            hash ^= Convert.ToInt32(ColorClamp);
            hash ^= (int)ColorRegId << 4;

            hash ^= (int)AlphaInA;
            hash ^= (int)AlphaInB << 2;
            hash ^= (int)AlphaInC << 6;
            hash ^= (int)AlphaInD << 5;

            hash ^= (int)AlphaOp << 4;
            hash ^= (int)AlphaBias << 5;
            hash ^= (int)AlphaScale << 4;
            hash ^= Convert.ToInt32(AlphaClamp);
            hash ^= (int)AlphaRegId << 7;

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TevStage))
                return false;
            else
                return Equals((TevStage)obj);
        }

        public bool Equals(TevStage other)
        {
            return ColorInA == other.ColorInA &&
                ColorInB == other.ColorInB &&
                ColorInC == other.ColorInC &&
                ColorInD == other.ColorInD &&
                ColorOp == other.ColorOp &&
                ColorBias == other.ColorBias &&
                ColorScale == other.ColorScale &&
                ColorClamp == other.ColorClamp &&
                ColorRegId == other.ColorRegId &&
                AlphaInA == other.AlphaInA &&
                AlphaInB == other.AlphaInB &&
                AlphaInC == other.AlphaInC &&
                AlphaInD == other.AlphaInD &&
                AlphaOp == other.AlphaOp &&
                AlphaBias == other.AlphaBias &&
                AlphaScale == other.AlphaScale &&
                AlphaClamp == other.AlphaClamp &&
                AlphaRegId == other.AlphaRegId;
        }
    }
}
