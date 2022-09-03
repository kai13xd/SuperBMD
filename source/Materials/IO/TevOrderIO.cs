namespace SuperBMD.Materials.IO
{
    public static class TevOrderIO
    {
        public static List<TevOrder> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<TevOrder> orders = new List<TevOrder>();
            int count = size / 4;

            for (int i = 0; i < count; i++)
                orders.Add(new TevOrder(ref reader));

            return orders;
        }

        public static void Write(ref EndianBinaryWriter writer, List<TevOrder> orders)
        {
            foreach (TevOrder order in orders)
                order.Write(ref writer);
        }
    }
}
