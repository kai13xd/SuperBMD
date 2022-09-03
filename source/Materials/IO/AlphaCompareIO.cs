namespace SuperBMD.Materials.IO
{
    public static class AlphaCompareIO
    {
        public static List<AlphaCompare> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<AlphaCompare> compares = new List<AlphaCompare>();
            int count = size / 8;

            for (int i = 0; i < count; i++)
                compares.Add(new AlphaCompare(ref reader));

            return compares;
        }

        public static void Write(ref EndianBinaryWriter writer, List<AlphaCompare> comps)
        {
            foreach (AlphaCompare comp in comps)
                comp.Write(ref writer);
        }
    }
}
