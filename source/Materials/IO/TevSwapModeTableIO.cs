namespace SuperBMD.Materials.IO
{
    public static class TevSwapModeTableIO
    {
        public static List<TevSwapModeTable> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<TevSwapModeTable> modes = new List<TevSwapModeTable>();
            int count = size / 4;

            for (int i = 0; i < count; i++)
                modes.Add(new TevSwapModeTable(ref reader));

            return modes;
        }

        public static void Write(ref EndianBinaryWriter writer, List<TevSwapModeTable> tables)
        {
            foreach (TevSwapModeTable table in tables)
                table.Write(ref writer);
        }
    }
}
