namespace SuperBMD.Materials.IO
{
    public static class NBTScaleIO
    {
        public static List<NBTScale> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<NBTScale> scales = new List<NBTScale>();
            int count = size / 16;

            for (int i = 0; i < count; i++)
                scales.Add(new NBTScale(ref reader));

            return scales;
        }

        public static void Write(ref EndianBinaryWriter writer, List<NBTScale> scales)
        {
            foreach (NBTScale scale in scales)
                scale.Write(ref writer);
        }
    }
}
