namespace SuperBMD.Materials.IO
{
    public static class TevSwapModeIO
    {
        public static List<TevSwapMode> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<TevSwapMode> modes = new List<TevSwapMode>();
            int count = size / 4;

            for (int i = 0; i < count; i++)
                modes.Add(new TevSwapMode(ref reader));

            return modes;
        }

        public static void Write(ref EndianBinaryWriter writer, List<TevSwapMode> modes)
        {
            foreach (TevSwapMode mode in modes)
                mode.Write(ref writer);
        }
    }
}
