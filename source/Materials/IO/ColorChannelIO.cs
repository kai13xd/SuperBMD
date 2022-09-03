using SuperBMD.Util;

namespace SuperBMD.Materials.IO
{
    public static class ColorChannelIO
    {
        public static List<ChannelControl> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<ChannelControl> controls = new List<ChannelControl>();
            int count = size / 8;

            for (int i = 0; i < count; i++)
                controls.Add(new ChannelControl(ref reader));

            return controls;
        }

        public static void Write(ref EndianBinaryWriter writer, List<ChannelControl> channels)
        {
            foreach (ChannelControl chan in channels)
                chan.Write(ref writer);

            writer.PadAlign(4);
        }
    }
}
