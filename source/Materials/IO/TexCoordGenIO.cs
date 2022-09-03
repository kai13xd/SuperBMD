namespace SuperBMD.Materials.IO
{
    public static class TexCoordGenIO
    {
        public static List<TexCoordGen> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<TexCoordGen> gens = new List<TexCoordGen>();
            int count = size / 4;

            for (int i = 0; i < count; i++)
                gens.Add(new TexCoordGen(ref reader));

            return gens;
        }

        public static void Write(ref EndianBinaryWriter writer, List<TexCoordGen> gens)
        {
            foreach (TexCoordGen gen in gens)
                gen.Write(ref writer);
        }
    }
}
