using SuperBMD.Materials;
using SuperBMD.Util;

namespace SuperBMD.BMD
{
    public class TEX1
    {
        public List<BinaryTextureImage> Textures { get; private set; }

        public TEX1(ref EndianBinaryReader reader, BMDInfo modelstats = null)
        {
            Textures = new List<BinaryTextureImage>();

            var offset = reader.Position;
            if (reader.ReadString(4) != "TEX1")
            {
                throw new Exception("SuperBMD is lost! TEX1 header is malformed!");
            }
            int tex1Size = reader.ReadInt();
            short texCount = reader.ReadShort();
            reader.Skip(2);

            if (modelstats != null)
            {
                modelstats.TEX1Size = tex1Size;
            }

            int textureHeaderOffset = reader.ReadInt();
            int textureNameTableOffset = reader.ReadInt();

            List<string> names = NameTableIO.Load(ref reader, offset + textureNameTableOffset);

            reader.Seek(textureHeaderOffset + offset);

            for (int i = 0; i < texCount; i++)
            {
                reader.Seek((offset + 0x20 + (0x20 * i)));

                BinaryTextureImage img = new BinaryTextureImage(names[i]);
                img.Load(ref reader, (offset + 0x20 + (0x20 * i)));
                Textures.Add(img);
            }
        }

        public TEX1(Assimp.Scene scene)
        {
            Textures = new List<BinaryTextureImage>();

            if (Arguments.TexHeaderPath != "")
            {
                string dir_path = Path.GetDirectoryName(Arguments.TexHeaderPath);
                LoadTexturesFromJson(Arguments.TexHeaderPath, dir_path);
            }
            else
                LoadTexturesFromScene(scene, Path.GetDirectoryName(Arguments.InputPath));
        }

        private void LoadTexturesFromJson(string headers_path, string directory_path)
        {
            Textures = File.ReadAllText(headers_path).JsonDeserialize<List<BinaryTextureImage>>();

            foreach (BinaryTextureImage tex in Textures)
            {
                
                // We'll search for duplicate texture names.
                BinaryTextureImage duplicate_search = Textures.Find(x => x.Name == tex.Name);

                // Otherwise we have to load the image from disk
                string nameWithoutExt = Path.Combine(directory_path, tex.Name);
                string fullImgPath = FindImagePath(nameWithoutExt);

                if (fullImgPath.IsEmpty())
                {
                    throw new Exception($"Could not find texture \"{nameWithoutExt}\".");
                }
                tex.LoadImageDataFromDisk(fullImgPath, Arguments.ShouldReadMipmaps);
            }
        }

        private void LoadTexturesFromScene(Assimp.Scene scene, string model_directory)
        {
            foreach (Assimp.Mesh mesh in scene.Meshes)
            {
                Console.Write(mesh.Name);
                Assimp.Material mat = scene.Materials[mesh.MaterialIndex];

                if (mat.HasTextureDiffuse)
                {
                    string texName = System.IO.Path.GetFileNameWithoutExtension(mat.TextureDiffuse.FilePath);
                    bool isEmbedded = false;
                    int embeddedIndex = -1;

                    if (mat.TextureDiffuse.FilePath.StartsWith("*"))
                    {
                        string index = mat.TextureDiffuse.FilePath.Substring(1, mat.TextureDiffuse.FilePath.Length); ;
                        isEmbedded = int.TryParse(index, out embeddedIndex);
                        texName = String.Format("embedded_tex{0}", embeddedIndex);
                    }

                    bool alreadyExists = false;

                    foreach (BinaryTextureImage image in Textures)
                    {
                        if (image.Name == texName)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (alreadyExists)
                    {
                        continue;
                    }

                    BinaryTextureImage img = new BinaryTextureImage();

                    if (isEmbedded)
                    {
                        Assimp.EmbeddedTexture embeddedTexture = scene.Textures[embeddedIndex];
                        img.Load(mat.TextureDiffuse, embeddedTexture);
                    }
                    else
                    {
                        img.Load(mat.TextureDiffuse, model_directory);
                    }
                    Textures.Add(img);
                }
                else
                    Console.WriteLine(" -> Has No Textures");
            }
        }

        public void AddTextureFromPath(string path)
        {
            string modelDirectory = System.IO.Path.GetDirectoryName(path);
            BinaryTextureImage img = new BinaryTextureImage();

            // Only the path and the wrap mode are relevant, the rest doesn't matter for img.Load
            Assimp.TextureSlot tex = new(path, 0, 0, 0, 0, (float)0.0, 0, Assimp.TextureWrapMode.Clamp, Assimp.TextureWrapMode.Clamp, 0);

            img.Load(tex, modelDirectory);

            Textures.Add(img);
        }

        private string FindImagePath(string name_without_ext)
        {
            if (File.Exists(name_without_ext + ".png"))
                return name_without_ext + ".png";
            if (File.Exists(name_without_ext + ".jpg"))
                return name_without_ext + ".jpg";
            if (File.Exists(name_without_ext + ".tga"))
                return name_without_ext + ".tga";
            if (File.Exists(name_without_ext + ".bmp"))
                return name_without_ext + ".bmp";
            return "";
        }

        public void DumpTextures(string directory, string filename, bool list = false, bool writeMipmaps = true)
        {
            Directory.CreateDirectory(directory + "/textures/");
            foreach (BinaryTextureImage tex in Textures)
            {
                tex.SaveImageToDisk(directory + "/textures/", writeMipmaps);
                if (list)
                    Console.WriteLine($"Saved \"{tex.Name}\" to Disk");
            }

            var jsonString = Textures.JsonSerialize();
            File.WriteAllText(Path.Combine(directory, filename), jsonString);

            if (list)
                Console.WriteLine("Texture Headers have been saved!");
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            long start = writer.Position;

            writer.Write("TEX1");
            writer.Write(0); // Placeholder for section size
            writer.Write((short)Textures.Count);
            writer.Write((short)-1);
            writer.Write(32); // Offset to the start of the texture data. Always 32
            writer.Write(0); // Placeholder for string table offset

            writer.PadAlign(32);

            List<string> names = new List<string>();
            Dictionary<string, Tuple<byte[], ushort[]>> image_palette_Data = new Dictionary<string, Tuple<byte[], ushort[]>>();
            Dictionary<string, int> imageDataOffsets = new Dictionary<string, int>();
            Dictionary<string, int> paletteDataOffsets = new Dictionary<string, int>();

            foreach (BinaryTextureImage img in Textures)
            {
                if (image_palette_Data.ContainsKey(img.Name))
                {
                    img.PaletteCount = (ushort)image_palette_Data[img.Name].Item2.Length;
                    img.PalettesEnabled = (image_palette_Data[img.Name].Item2.Length > 0);
                }
                else
                {
                    image_palette_Data.Add(img.Name, img.EncodeData());
                    imageDataOffsets.Add(img.Name, 0);
                    paletteDataOffsets.Add(img.Name, 0);
                }
                names.Add(img.Name);
                img.WriteHeader(ref writer);
            }

            long curOffset = writer.Position;

            // Write the palette data and note the offset in paletteDataOffsets
            foreach (string key in image_palette_Data.Keys)
            {
                paletteDataOffsets[key] = (int)(curOffset - start);
                if (image_palette_Data[key].Item2.Length > 0)
                {
                    foreach (ushort st in image_palette_Data[key].Item2)
                        writer.Write(st);

                    writer.PadAlign(32);
                }
                curOffset = writer.Position;
            }

            // Write the image data and note the offset in imageDataOffsets
            foreach (string key in image_palette_Data.Keys)
            {
                // Avoid writing duplicate image data
                if (imageDataOffsets[key] == 0)
                {
                    imageDataOffsets[key] = (int)(curOffset - start);

                    writer.Write(image_palette_Data[key].Item1);

                    curOffset = writer.Position;
                }
            }

            // Write texture name table offset
            writer.Seek((int)start + 16);
            writer.Write((int)(curOffset - start));
            writer.Seek((int)curOffset);
            NameTableIO.Write(ref writer, names);

            writer.PadAlign(32);

            long end = writer.Position;
            long length = (end - start);

            // Write TEX1 size
            writer.Seek((int)start + 4);
            writer.Write((int)length);
            writer.Seek((int)end);

            writer.Seek((int)start + 32);

            // Write palette and image data offsets to headers
            for (int i = 0; i < Textures.Count; i++)
            {
                int header_offset_const = 32 + i * 32;

                // Start is the beginning of the TEX1 section;
                // (i * 32) is the offset of the header in the header data block;
                // 32 is the offset of the header data block from the beginning of TEX1;
                // 12 is the offset of the palette data offset in the header
                writer.Seek((int)start + (i * 32) + 32 + 12);
                writer.Write(paletteDataOffsets[Textures[i].Name] - header_offset_const);

                // Same as above, except instead of 12 it's 28.
                // 28 is the offset of the image data offset in the header
                writer.Seek((int)start + (i * 32) + 32 + 28);
                writer.Write(imageDataOffsets[Textures[i].Name] - header_offset_const);
            }
        }

        public string getTextureInstanceName(int index)
        {
            if (Textures is null)
            {
                return null;
            }
            else
            {
                string name = Textures[index].Name;

                int number = 0;
                for (int i = 0; i < Textures.Count; i++)
                {
                    if (i == index)
                    {
                        break;
                    }
                    if (Textures[i].Name == name)
                    {
                        number += 1;
                    }
                }
                return String.Format("{0}:{1}", name, number);
            }
        }

        public int getTextureIndexFromInstanceName(string instanceName)
        {
            if (Textures is null)
            {
                return -1;
            }

            string[] subs = instanceName.Split(new string[] { ":" }, 2, StringSplitOptions.None);
            if (subs.Length == 2)
            {
                string texture = subs[0];
                int instanceNumber;
                if (!int.TryParse(subs[1], out instanceNumber))
                {
                    texture = instanceName;
                    instanceNumber = 0;
                }

                int instancesPassed = 0;
                for (int i = 0; i < Textures.Count; i++)
                {
                    if (Textures[i].Name == texture)
                    {
                        if (instancesPassed == instanceNumber)
                        {
                            return i;
                        }
                        else
                        {
                            instancesPassed += 1;
                        }
                    }
                }
                return -1;
                //throw new Exception(String.Format("Didn't find texture: {0}", instanceName));
            }
            else
            {
                for (int i = 0; i < Textures.Count; i++)
                {
                    if (Textures[i].Name == instanceName)
                    {
                        return i;
                    }
                }
                return -1;
                //throw new Exception(String.Format("Didn't find texture: {0}", instanceName));
            }
        }

        public BinaryTextureImage this[int i]
        {
            get
            {
                if (Textures != null && Textures.Count > i)
                {
                    return Textures[i];
                }
                else
                {
                    Console.WriteLine($"Could not retrieve texture at index {i}.");
                    return null;
                }
            }
            set
            {
                if (Textures is null)
                    Textures = new List<BinaryTextureImage>();

                Textures[i] = value;
            }
        }

        public BinaryTextureImage this[string s]
        {

            get
            {
                s = s.Split(":")[0];
                if (Textures is null)
                {
                    Console.WriteLine("There are no textures currently loaded.");
                    return null;
                }

                if (Textures.Count == 0)
                {
                    Console.WriteLine("There are no textures currently loaded.");
                    return null;
                }

                foreach (BinaryTextureImage tex in Textures)
                {
                    if (tex.Name == s)
                        return tex;
                }

                Console.Write($"No texture with the name {s} was found.");
                return null;
            }

            private set
            {
                s = s.Split(":")[0];
                if (Textures is null)
                {
                    Textures = new List<BinaryTextureImage>();
                    Console.WriteLine("There are no textures currently loaded.");
                    return;
                }

                for (int i = 0; i < Textures.Count; i++)
                {
                    if (Textures[i].Name == s)
                    {
                        Textures[i] = value;
                        break;
                    }

                    if (i == Textures.Count - 1)
                        Console.WriteLine($"No texture with the name {s} was found.");
                }
            }
        }
    }
}
