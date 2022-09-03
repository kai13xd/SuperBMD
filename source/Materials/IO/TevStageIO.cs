namespace SuperBMD.Materials.IO
{
    public static class TevStageIO
    {
        public static List<TevStage> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<TevStage> stages = new List<TevStage>();
            int count = size / 20;

            for (int i = 0; i < count; i++)
                stages.Add(new TevStage(ref reader));

            return stages;
        }

        public static void Write(ref EndianBinaryWriter writer, List<TevStage> stages)
        {
            foreach (TevStage stage in stages)
                stage.Write(ref writer);
        }
    }
}
