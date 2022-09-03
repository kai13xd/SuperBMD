namespace SuperBMD.Materials.IO
{
    public static class FogIO
    {
        public static List<Fog> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<Fog> fogs = new List<Fog>();
            int count = size / 44;

            for (int i = 0; i < count; i++)
                fogs.Add(new Fog(ref reader));

            return fogs;
        }

        public static void Write(ref EndianBinaryWriter writer, List<Fog> fogs)
        {
            foreach (Fog fog in fogs)
                fog.Write(ref writer);
        }
    }
}
