namespace SuperBMD.Materials.IO
{
    public static class TexMatrixIO
    {
        public static List<TexMatrix> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<TexMatrix> matrices = new List<TexMatrix>();
            int count = size / 100;

            for (int i = 0; i < count; i++)
                matrices.Add(new TexMatrix(ref reader));

            return matrices;
        }

        public static void Write(ref EndianBinaryWriter writer, List<TexMatrix> mats)
        {
            foreach (TexMatrix mat in mats)
                mat.Write(ref writer);
        }
    }
}
