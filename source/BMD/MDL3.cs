using SuperBMD.Materials;
using SuperBMD.Materials.Mdl;

namespace SuperBMD.BMD
{
    public class MDL3
    {
        List<MdlEntry> Entries;

        public MDL3()
        {
            Entries = new List<MdlEntry>();
        }

        public MDL3(List<BMDMaterial> materials, List<BinaryTextureImage> textures)
        {
            Entries = new List<MdlEntry>();

            foreach (BMDMaterial mat in materials)
            {
                Console.Write(string.Format("Generating for {0} - ", mat.Name));
                Entries.Add(new MdlEntry(mat, textures));
                Console.WriteLine("Completed");
            }
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            long start = writer.Position;

            writer.Write("MDL3".ToCharArray());
            writer.Write(0); // Placeholder for section size
            writer.Write((short)Entries.Count);
            writer.Write((short)-1);

            writer.Write(0x40); // Offset to command data offset/size block
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            writer.PadAlign(32);

            long cmdBlockStart = writer.Position;

            for (int i = 0; i < Entries.Count; i++)
            {
                writer.Write(0);
                writer.Write(0);
            }

            writer.PadAlign(32);

            for (int i = 0; i < Entries.Count; i++)
            {
                long absoluteStartOffset = writer.Position;
                long relativeStartOffset = writer.Position - cmdBlockStart - i * 8;

                Entries[i].Write(ref writer);

                writer.PadAlignZero(32);

                long size = writer.Position - absoluteStartOffset;
                writer.Seek((int)cmdBlockStart + (i * 8));

                writer.Write((int)relativeStartOffset);
                writer.Write((int)size);

                writer.SeekEnd();
            }

            long subsection2StartOffset = writer.Position;
            writer.Seek((int)start + 0x10);
            var x = (int)subsection2StartOffset - start;
            writer.Write((int)(subsection2StartOffset - start));
            writer.Seek((int)subsection2StartOffset);
            for (int i = 0; i < Entries.Count; i++)
            {
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
            }

            long subsection3StartOffset = writer.Position;
            writer.Seek((int)start + 0x14);
            writer.Write((int)(subsection3StartOffset - start));
            writer.Seek((int)subsection3StartOffset);
            for (int i = 0; i < Entries.Count; i++)
            {
                writer.Write(0);
                writer.Write(0);
            }

            long subsection4StartOffset = writer.Position;
            writer.Seek((int)start + 0x18);
            writer.Write((int)(subsection4StartOffset - start));
            writer.Seek((int)subsection4StartOffset);
            for (int i = 0; i < Entries.Count; i++)
            {
                writer.Write((byte)1);
            }
            writer.PadAlign(4);

            long subsection5StartOffset = writer.Position;
            writer.Seek((int)start + 0x1C);
            writer.Write((int)(subsection5StartOffset - start));
            writer.Seek((int)subsection5StartOffset);
            for (int i = 0; i < Entries.Count; i++)
            {
                writer.Write((short)i);
            }
            writer.PadAlign(4);

            long stringTableStartOffset = writer.Position;
            writer.Seek((int)start + 0x20);
            writer.Write((int)(stringTableStartOffset - start));
            writer.Seek((int)stringTableStartOffset);
            writer.Write((short)0);

            writer.PadAlign(32);

            long end = writer.Position;
            long length = (end - start);

            writer.Seek((int)start + 4);
            writer.Write((int)length);
            writer.Seek((int)end);
        }
    }
}
