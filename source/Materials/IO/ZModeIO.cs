namespace SuperBMD.Materials.IO
{
    public static class ZModeIO
    {
        public static List<ZMode> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<ZMode> modes = new List<ZMode>();
            int count = size / 4;

            for (int i = 0; i < count; i++)
                modes.Add(new ZMode(ref reader));

            return modes;
        }

        public static void Write(ref EndianBinaryWriter writer, List<ZMode> modes)
        {
            foreach (ZMode mode in modes)
                mode.Write(ref writer);
        }
    }
}
