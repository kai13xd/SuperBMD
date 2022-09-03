using SuperBMD.Materials;

namespace SuperBMD.Materials.IO
{
    public static class CullModeIO
    {
        public static List<CullMode> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<CullMode> modes = new List<CullMode>();
            int count = size / 4;

            for (int i = 0; i < count; i++)
                modes.Add((CullMode)reader.ReadInt());

            return modes;
        }

        public static void Write(ref EndianBinaryWriter writer, List<CullMode> modes)
        {
            foreach (CullMode mode in modes)
                writer.Write((int)mode);
        }
    }
}
