namespace SuperBMD.Materials.IO
{
    public static class BlendModeIO
    {
        public static List<BlendMode> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<BlendMode> modes = new List<BlendMode>();
            int count = size / 4;

            for (int i = 0; i < count; i++)
                modes.Add(new BlendMode(ref reader));

            return modes;
        }

        public static void Write(ref EndianBinaryWriter writer, List<BlendMode> modes)
        {
            foreach (BlendMode mode in modes)
                mode.Write(ref writer);
        }
    }
}
