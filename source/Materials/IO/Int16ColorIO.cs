using SuperBMD.Util;

namespace SuperBMD.Materials.IO
{
    public static class Int16ColorIO
    {
        public static List<Color> Load(ref EndianBinaryReader reader, int offset, int size)
        {
            List<Color> colors = new List<Color>();
            int count = size / 8;

            for (int i = 0; i < count; i++)
            {
                short r = reader.ReadShort();
                short g = reader.ReadShort();
                short b = reader.ReadShort();
                short a = reader.ReadShort();

                colors.Add(new Color((float)r / 255, (float)g / 255, (float)b / 255, (float)a / 255));
            }

            return colors;
        }

        public static void Write(ref EndianBinaryWriter writer, List<Color> colors)
        {
            foreach (Color col in colors)
            {
                writer.Write((short)(col.R * 255));
                writer.Write((short)(col.G * 255));
                writer.Write((short)(col.B * 255));
                writer.Write((short)(col.A * 255));
            }
        }
    }
}
