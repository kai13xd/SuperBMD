namespace SuperBMD.Materials.IO
{
    public static class IndirectTexturingIO
    {
        public static List<IndirectTexturing> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<IndirectTexturing> indirects = new List<IndirectTexturing>();
            int count = size / 312;

            for (int i = 0; i < count; i++)
                indirects.Add(new IndirectTexturing(ref reader));

            return indirects;
        }

        public static void Write(ref EndianBinaryWriter writer, List<IndirectTexturing> indTex)
        {
            foreach (IndirectTexturing ind in indTex)
            {
                ind.Write(ref writer);
            }
        }
    }
}
