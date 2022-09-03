namespace SuperBMD.Util
{
    public static class NameTableIO
    {
        public static List<string> Load(ref EndianBinaryReader reader, int offset)
        {
            List<string> names = new List<string>();

            reader.Seek(offset);

            short stringCount = reader.ReadShort();
            reader.Skip(2);

            for (int i = 0; i < stringCount; i++)
            {
                var hash = reader.ReadShort();
                short nameOffset = reader.ReadShort();
                reader.Remember();
                reader.Seek(offset + nameOffset);
                names.Add(reader.ReadString('\0'));
                reader.Recall();
            }
            return names;
        }

        public static void Write(ref EndianBinaryWriter writer, List<string> names)
        {
            long start = writer.Position;

            writer.Write((short)names.Count);
            writer.Write((short)-1);

            foreach (string st in names)
            {
                writer.Write(HashString(st));
                writer.Write((short)0);
            }

            long curOffset = writer.Position;
            for (int i = 0; i < names.Count; i++)
            {
                writer.Seek((int)(start + (6 + i * 4)));
                writer.Write((short)(curOffset - start));
                writer.Seek((int)curOffset);

                writer.Write(names[i]);
                writer.Write((byte)0);

                curOffset = writer.Position;
            }
        }

        private static ushort HashString(string str)
        {
            ushort hash = 0;

            foreach (char c in str)
            {
                hash *= 3;
                hash += (ushort)c;
            }

            return hash;
        }
    }
}
