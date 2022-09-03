using SuperBMD.Materials;
using SuperBMD.Util;
using SuperBMD.Materials.IO;

namespace SuperBMD.BMD
{
    struct PresetResult
    {
        public PresetResult(Material mat, int i)
        {
            Preset = mat;
            Index = i;
        }
        public Material Preset;
        public int Index;
    }
    public class MAT3
    {
        public List<Material> Materials = new();
        public List<short> RemapIndices = new();
        private List<string> MaterialNames = new();
        private List<IndirectTexturing> IndirectTexBlock = new();
        private List<CullMode> CullModeBlock = new();
        private List<Color> MaterialColorBlock = new();
        private List<ChannelControl> ChannelControlBlock = new();
        private List<Color> AmbientColorBlock = new();
        private List<Color> LightingColorBlock = new();
        private List<TexCoordGen> TexCoord1GenBlock = new();
        private List<TexCoordGen> TexCoord2GenBlock = new();
        private List<Materials.TexMatrix> TexMatrix1Block = new();
        private List<Materials.TexMatrix> TexMatrix2Block = new();
        private List<short> TexRemapBlock = new();
        private List<TevOrder> TevOrderBlock = new();
        private List<Color> TevColorBlock = new();
        private List<Color> TevKonstColorBlock = new();
        private List<TevStage> TevStageBlock = new();
        private List<TevSwapMode> SwapModeBlock = new();
        private List<TevSwapModeTable> SwapTableBlock = new();
        private List<Fog> FogBlock = new();
        private List<AlphaCompare> AlphaCompBlock = new();
        private List<Materials.BlendMode> BlendModeBlock = new();
        private List<NBTScale> NBTScaleBlock = new();
        private List<ZMode> ZModeBlock = new();
        private List<bool> ZCompLocBlock = new();
        private List<bool> DitherBlock = new();
        private List<byte> NumColorChannelsBlock = new();
        private List<byte> NumTexGensBlock = new();
        private List<byte> NumTevStagesBlock = new();

        private string[] delimiter = new string[] { ":" };

        public MAT3(ref EndianBinaryReader reader, BMDInfo modelstats)
        {
            var offset = reader.Position;

            if (reader.ReadString(4) != "MAT3")
                throw new Exception("SuperBMD is lost! MAT3 header is malformed!");
            int mat3Size = reader.ReadInt();
            int matCount = reader.ReadShort();
            int matInitOffset = 0;
            reader.Skip(2);

            modelstats.MAT3Size = mat3Size;


            for (Mat3OffsetIndex i = 0; i <= Mat3OffsetIndex.NBTScaleData; ++i)
            {
                int sectionOffset = reader.ReadInt();

                if (sectionOffset == 0)
                    continue;

                reader.Remember();
                int nextOffset = reader.PeekInt();
                int sectionSize = 0;

                if (i == Mat3OffsetIndex.NBTScaleData)
                {

                }

                if (nextOffset == 0 && i != Mat3OffsetIndex.NBTScaleData)
                {
                    int saveReaderPos = reader.Position;

                    reader.Position += 4;

                    while (reader.PeekInt() == 0)
                        reader.Position += 4;

                    nextOffset = reader.PeekInt();
                    sectionSize = nextOffset - sectionOffset;

                    reader.Position = saveReaderPos;
                }
                else if (i == Mat3OffsetIndex.NBTScaleData)
                    sectionSize = mat3Size - sectionOffset;
                else
                    sectionSize = nextOffset - sectionOffset;

                reader.Position = (offset) + sectionOffset;

                switch (i)
                {
                    case Mat3OffsetIndex.MaterialData:
                        matInitOffset = reader.Position;
                        break;
                    case Mat3OffsetIndex.IndexData:
                        RemapIndices = new List<short>();

                        for (int index = 0; index < matCount; index++)
                            RemapIndices.Add(reader.ReadShort());
                        break;
                    case Mat3OffsetIndex.NameTable:
                        MaterialNames = NameTableIO.Load(ref reader, offset + sectionOffset);
                        break;
                    case Mat3OffsetIndex.IndirectData:
                        IndirectTexBlock = IndirectTexturingIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.CullMode:
                        CullModeBlock = CullModeIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.MaterialColor:
                        MaterialColorBlock = ColorIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.ColorChannelCount:
                        NumColorChannelsBlock = new List<byte>();

                        for (int chanCnt = 0; chanCnt < sectionSize; chanCnt++)
                        {
                            byte chanCntIn = reader.ReadByte();

                            if (chanCntIn < 84)
                                NumColorChannelsBlock.Add(chanCntIn);
                        }

                        break;
                    case Mat3OffsetIndex.ColorChannelData:
                        ChannelControlBlock = ColorChannelIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.AmbientColorData:
                        AmbientColorBlock = ColorIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.LightData:
                        LightingColorBlock = ColorIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TexGenCount:
                        NumTexGensBlock = new List<byte>();

                        for (int genCnt = 0; genCnt < sectionSize; genCnt++)
                        {
                            byte genCntIn = reader.ReadByte();

                            if (genCntIn < 84)
                                NumTexGensBlock.Add(genCntIn);
                        }

                        break;
                    case Mat3OffsetIndex.TexCoordData:
                        TexCoord1GenBlock = TexCoordGenIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TexCoord2Data:
                        TexCoord2GenBlock = TexCoordGenIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TexMatrixData:
                        TexMatrix1Block = TexMatrixIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TexMatrix2Data:
                        TexMatrix2Block = TexMatrixIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TexNoData:
                        TexRemapBlock = new List<short>();
                        int texNoCnt = sectionSize / 2;

                        for (int texNo = 0; texNo < texNoCnt; texNo++)
                            TexRemapBlock.Add(reader.ReadShort());

                        break;
                    case Mat3OffsetIndex.TevOrderData:
                        TevOrderBlock = TevOrderIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TevColorData:
                        TevColorBlock = Int16ColorIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TevKColorData:
                        TevKonstColorBlock = ColorIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TevStageCount:
                        NumTevStagesBlock = new List<byte>();

                        for (int stgCnt = 0; stgCnt < sectionSize; stgCnt++)
                        {
                            byte stgCntIn = reader.ReadByte();

                            if (stgCntIn < 84)
                                NumTevStagesBlock.Add(stgCntIn);
                        }

                        break;
                    case Mat3OffsetIndex.TevStageData:
                        TevStageBlock = TevStageIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TevSwapModeData:
                        SwapModeBlock = TevSwapModeIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.TevSwapModeTable:
                        SwapTableBlock = TevSwapModeTableIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.FogData:
                        FogBlock = FogIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.AlphaCompareData:
                        AlphaCompBlock = AlphaCompareIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.BlendData:
                        BlendModeBlock = BlendModeIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.ZModeData:
                        ZModeBlock = ZModeIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                    case Mat3OffsetIndex.ZCompLoc:
                        ZCompLocBlock = new List<bool>();

                        for (int zcomp = 0; zcomp < sectionSize; zcomp++)
                        {
                            byte boolIn = reader.ReadByte();

                            if (boolIn > 1)
                                break;

                            ZCompLocBlock.Add(Convert.ToBoolean(boolIn));
                        }

                        break;
                    case Mat3OffsetIndex.DitherData:
                        DitherBlock = new List<bool>();

                        for (int dith = 0; dith < sectionSize; dith++)
                        {
                            byte boolIn = reader.ReadByte();

                            if (boolIn > 1)
                                break;

                            DitherBlock.Add(Convert.ToBoolean(boolIn));
                        }

                        break;
                    case Mat3OffsetIndex.NBTScaleData:
                        NBTScaleBlock = NBTScaleIO.Load(ref reader, sectionOffset, sectionSize);
                        break;
                }

                reader.Recall();
            }

            int highestMatIndex = 0;

            for (int i = 0; i < matCount; i++)
            {
                if (RemapIndices[i] > highestMatIndex)
                    highestMatIndex = RemapIndices[i];
            }

            reader.Position = matInitOffset;
            Materials = new List<Material>();
            for (int i = 0; i <= highestMatIndex; i++)
            {
                LoadInitData(ref reader, RemapIndices[i]);
            }

            reader.Seek(offset + mat3Size);

            List<Material> matCopies = new List<Material>();
            for (int i = 0; i < RemapIndices.Count; i++)
            {
                Material originalMat = Materials[RemapIndices[i]];
                Material copyMat = new Material(originalMat);
                copyMat.Name = MaterialNames[i];
                matCopies.Add(copyMat);
            }

            Materials = matCopies;
        }

        private void LoadInitData(ref EndianBinaryReader reader, int matindex)
        {
            Material mat = new Material();
            mat.Name = MaterialNames[matindex];
            mat.Flag = reader.ReadByte();
            mat.CullMode = CullModeBlock[reader.ReadByte()];

            mat.ColorChannelControlsCount = NumColorChannelsBlock[reader.ReadByte()];
            mat.NumTexGensCount = NumTexGensBlock[reader.ReadByte()];
            mat.NumTevStagesCount = NumTevStagesBlock[reader.ReadByte()];

            if (matindex < IndirectTexBlock.Count)
            {
                mat.IndTexEntry = IndirectTexBlock[matindex];
            }
            else
            {
                Console.WriteLine("Warning: Material {0} referenced an out of range IndirectTexBlock index", mat.Name);
            }

            mat.ZCompLoc = ZCompLocBlock[reader.ReadByte()];
            mat.ZMode = ZModeBlock[reader.ReadByte()];

            if (DitherBlock is null)
                reader.Skip();
            else
                mat.Dither = DitherBlock[reader.ReadByte()];
            int matColorIndex = reader.ReadShort();
            if (matColorIndex != -1)
                mat.MaterialColors[0] = MaterialColorBlock[matColorIndex];
            matColorIndex = reader.ReadShort();
            if (matColorIndex != -1)
                mat.MaterialColors[1] = MaterialColorBlock[matColorIndex];

            for (int i = 0; i < 4; i++)
            {
                int chanIndex = reader.ReadShort();
                if (chanIndex == -1)
                    continue;
                else if (chanIndex < ChannelControlBlock.Count)
                {
                    mat.ChannelControls[i] = ChannelControlBlock[chanIndex];
                }
                else
                {
                    Console.WriteLine(string.Format("Warning for material {0} i={2}, color channel index out of range: {1}", mat.Name, chanIndex, i));
                }
            }
            for (int i = 0; i < 2; i++)
            {
                int ambColorIndex = reader.ReadShort();
                if (ambColorIndex == -1)
                    continue;
                else if (ambColorIndex < AmbientColorBlock.Count)
                {
                    mat.AmbientColors[i] = AmbientColorBlock[ambColorIndex];
                }
                else
                {
                    Console.WriteLine(string.Format("Warning for material {0} i={2}, ambient color index out of range: {1}", mat.Name, ambColorIndex, i));
                }
            }

            for (int i = 0; i < 8; i++)
            {
                int lightIndex = reader.ReadShort();
                if ((lightIndex == -1) || (lightIndex > LightingColorBlock.Count) || (LightingColorBlock.Count == 0))
                    continue;
                else
                    mat.LightingColors[i] = LightingColorBlock[lightIndex];
            }

            for (int i = 0; i < 8; i++)
            {
                int texGenIndex = reader.ReadShort();
                if (texGenIndex == -1)
                    continue;
                else if (texGenIndex < TexCoord1GenBlock.Count)
                    mat.TexCoord1Gens[i] = TexCoord1GenBlock[texGenIndex];
                else
                    Console.WriteLine(string.Format("Warning for material {0} i={2}, TexCoord1GenBlock index out of range: {1}", mat.Name, texGenIndex, i));
            }

            for (int i = 0; i < 8; i++)
            {
                int texGenIndex = reader.ReadShort();
                if (texGenIndex == -1)
                    continue;
                else
                    mat.PostTexCoordGens[i] = TexCoord2GenBlock[texGenIndex];
            }

            for (int i = 0; i < 10; i++)
            {
                int texMatIndex = reader.ReadShort();
                if (texMatIndex == -1)
                    continue;
                else
                    mat.TexMatrix1[i] = TexMatrix1Block[texMatIndex];
            }

            for (int i = 0; i < 20; i++)
            {
                int texMatIndex = reader.ReadShort();
                if (texMatIndex == -1)
                    continue;
                else if (texMatIndex < TexMatrix2Block.Count)
                    mat.PostTexMatrix[i] = TexMatrix2Block[texMatIndex];
                else
                    Console.WriteLine(string.Format("Warning for material {0}, TexMatrix2Block index out of range: {1}", mat.Name, texMatIndex));
            }

            for (int i = 0; i < 8; i++)
            {
                int texIndex = reader.ReadShort();
                if (texIndex == -1)
                    continue;
                else
                    mat.TextureIndices[i] = TexRemapBlock[texIndex];
            }

            for (int i = 0; i < 4; i++)
            {
                int tevKColor = reader.ReadShort();
                if (tevKColor == -1)
                    continue;
                else
                    mat.KonstColors[i] = TevKonstColorBlock[tevKColor];
            }

            for (int i = 0; i < 16; i++)
            {
                mat.ColorSels[i] = (KonstColorSel)reader.ReadByte();
            }

            for (int i = 0; i < 16; i++)
            {
                mat.AlphaSels[i] = (KonstAlphaSel)reader.ReadByte();
            }

            for (int i = 0; i < 16; i++)
            {
                int tevOrderIndex = reader.ReadShort();
                if (tevOrderIndex == -1)
                    continue;
                else
                    mat.TevOrders[i] = TevOrderBlock[tevOrderIndex];
            }

            for (int i = 0; i < 4; i++)
            {
                int tevColor = reader.ReadShort();
                if (tevColor == -1)
                    continue;
                else
                    mat.TevColors[i] = TevColorBlock[tevColor];
            }

            for (int i = 0; i < 16; i++)
            {
                int tevStageIndex = reader.ReadShort();
                if (tevStageIndex == -1)
                    continue;
                else
                    mat.TevStages[i] = TevStageBlock[tevStageIndex];
            }

            for (int i = 0; i < 16; i++)
            {
                int tevSwapModeIndex = reader.ReadShort();
                if (tevSwapModeIndex == -1)
                    continue;
                else
                    mat.SwapModes[i] = SwapModeBlock[tevSwapModeIndex];
            }

            for (int i = 0; i < 16; i++)
            {
                int tevSwapModeTableIndex = reader.ReadShort();
                if ((tevSwapModeTableIndex < 0) || (tevSwapModeTableIndex >= SwapTableBlock.Count))
                    continue;
                else
                {
                    if (tevSwapModeTableIndex >= SwapTableBlock.Count)
                        continue;

                    mat.SwapTables[i] = SwapTableBlock[tevSwapModeTableIndex];
                }
            }

            mat.FogInfo = FogBlock[reader.ReadShort()];
            mat.AlphCompare = AlphaCompBlock[reader.ReadShort()];
            mat.BlendMode = BlendModeBlock[reader.ReadShort()];
            mat.NBTScale = NBTScaleBlock[reader.ReadShort()];
            //mat.Debug_Print();
            Materials.Add(mat);
        }

        public MAT3(Assimp.Scene scene, TEX1 textures, SHP1 shapes, List<Material> mat_presets = null)
        {
            LoadFromScene(scene, textures, shapes, mat_presets);
            FillMaterialDataBlocks();
        }

        private string FindOriginalMaterialName(string name, List<Material> mat_presets)
        {
            string result = null;
            if (mat_presets is null)
            {
                return result;
            }

            foreach (Material mat in mat_presets)
            {
                if (mat is null)
                {
                    continue;
                }
                if (mat.Name.StartsWith("__MatDefault"))
                {
                    continue;
                }
                if (name.StartsWith("m"))
                {
                    string sanitized = Model.AssimpMatnamePartSanitize(mat.Name);
                    if (
                        (name.Length > 2 && name.Substring(2) == sanitized) ||
                        (name.Length > 3 && name.Substring(3) == sanitized) ||
                        (name.Length > 4 && name.Substring(4) == sanitized))
                    {
                        //Console.WriteLine(String.Format("Matched up {0} with {1} from the json file", name, mat.Name));
                        result = mat.Name;
                        break;
                    }

                    if (name.EndsWith("-material"))
                    {
                        name = name.Substring(0, name.Length - 9);
                        if (
                            (name.Length > 2 && name.Substring(2) == sanitized) ||
                            (name.Length > 3 && name.Substring(3) == sanitized) ||
                            (name.Length > 4 && name.Substring(4) == sanitized))
                        {
                            //Console.WriteLine(String.Format("Matched up {0} with {1} from the json file", name, mat.Name));
                            result = mat.Name;
                            break;
                        }
                    }
                }

                if (name.EndsWith("-material"))
                {
                    string sanitized = Model.AssimpMatnamePartSanitize(mat.Name);
                    name = name.Substring(0, name.Length - 9);
                    if (
                        (name == sanitized)
                        )
                    {
                        //Console.WriteLine(String.Format("Matched up {0} with {1} from the json file", name, mat.Name));
                        result = mat.Name;
                        break;
                    }
                }
            }
            return result;
        }
        private int GetMatIndex(Material mat, List<Material> matPresets)
        {
            return matPresets.IndexOf(mat);
        }


        private PresetResult? FindMatPreset(string name, List<Material> mat_presets)
        {
            if (mat_presets is null)
            {
                return null;
            }
            Material default_mat = null;

            int i = 0;

            foreach (Material mat in mat_presets)
            {
                if (mat is null)
                {
                    if (Arguments.IsMaterialOrderStrict)
                    {
                        throw new Exception("Warning: Material entry with index { 0 } is malformed, cannot continue in Strict Material Order mode.");
                    }
                    Console.WriteLine(String.Format("Warning: Material entry with index {0} is malformed and has been skipped", i));
                    continue;
                }

                //Console.WriteLine(String.Format("{0}", mat.Name));

                if (mat.Name == "__MatDefault" && default_mat is null)
                {
                    if (Arguments.IsMaterialOrderStrict)
                    {
                        throw new Exception("'__MatDefault' materials cannot be used in Strict Material Order mode!");
                    }
                    default_mat = mat;
                }

                if (mat.Name.StartsWith("__MatDefault:"))
                {
                    if (Arguments.IsMaterialOrderStrict)
                    {
                        throw new Exception("'__MatDefault:' materials cannot be used in Strict Material Order mode!");
                    }
                    string[] subs = mat.Name.Split(delimiter, 2, StringSplitOptions.None);
                    if (subs.Length == 2)
                    {
                        string submat = "_" + subs[1];
                        if (name.Contains(submat))
                        {
                            default_mat = mat;
                        }
                    }
                }

                if (mat.Name == name)
                {
                    //Console.WriteLine(String.Format("Applying material preset to {1}", default_mat.Name, name));
                    return new PresetResult(mat, i);
                }
                if (name.StartsWith("m"))
                {
                    string sanitized = Model.AssimpMatnamePartSanitize(mat.Name);
                    if (
                        (name.Length > 2 && name.Substring(2) == sanitized) ||
                        (name.Length > 3 && name.Substring(3) == sanitized) ||
                        (name.Length > 4 && name.Substring(4) == sanitized))
                    {
                        Console.WriteLine(String.Format("Matched up {0} with {1} from the json file", name, mat.Name));
                        return new PresetResult(mat, i);
                    }
                }
                i++;
            }
            //if (default_mat != null)
            //    Console.WriteLine(String.Format("Applying __MatDefault to {1}", default_mat.Name, name));

            return new PresetResult(default_mat, -1);
        }

        private void SetPreset(Material bmdMaterial, Material preset)
        {
            // put data from preset over current material if it exists

            bmdMaterial.Flag = preset.Flag;
            bmdMaterial.ColorChannelControlsCount = preset.ColorChannelControlsCount;
            bmdMaterial.NumTexGensCount = preset.NumTexGensCount;
            bmdMaterial.NumTevStagesCount = preset.NumTevStagesCount;
            bmdMaterial.CullMode = preset.CullMode;

            if (preset.IndTexEntry is not null) bmdMaterial.IndTexEntry = preset.IndTexEntry;

            if (preset.MaterialColors != null) bmdMaterial.MaterialColors = preset.MaterialColors;
            if (preset.ChannelControls != null) bmdMaterial.ChannelControls = preset.ChannelControls;
            if (preset.AmbientColors != null) bmdMaterial.AmbientColors = preset.AmbientColors;
            if (preset.LightingColors != null) bmdMaterial.LightingColors = preset.LightingColors;

            if (preset.TexCoord1Gens != null) bmdMaterial.TexCoord1Gens = preset.TexCoord1Gens;
            if (preset.PostTexCoordGens != null) bmdMaterial.PostTexCoordGens = preset.PostTexCoordGens;
            if (preset.TexMatrix1 != null) bmdMaterial.TexMatrix1 = preset.TexMatrix1;
            if (preset.PostTexMatrix != null) bmdMaterial.PostTexMatrix = preset.PostTexMatrix;
            bmdMaterial.TextureNames = preset.TextureNames;

            if (preset.TevOrders != null) bmdMaterial.TevOrders = preset.TevOrders;
            if (preset.ColorSels != null) bmdMaterial.ColorSels = preset.ColorSels;
            if (preset.AlphaSels != null) bmdMaterial.AlphaSels = preset.AlphaSels;
            if (preset.TevColors != null) bmdMaterial.TevColors = preset.TevColors;
            if (preset.KonstColors != null) bmdMaterial.KonstColors = preset.KonstColors;
            if (preset.TevStages != null) bmdMaterial.TevStages = preset.TevStages;
            if (preset.SwapModes != null) bmdMaterial.SwapModes = preset.SwapModes;
            if (preset.SwapTables != null) bmdMaterial.SwapTables = preset.SwapTables;
            if (preset.FogInfo != null) bmdMaterial.FogInfo = preset.FogInfo;
            if (preset.AlphCompare != null) bmdMaterial.AlphCompare = preset.AlphCompare;
            if (preset.BlendMode != null) bmdMaterial.BlendMode = preset.BlendMode;
            if (preset.ZMode != null) bmdMaterial.ZMode = preset.ZMode;
            bmdMaterial.ZCompLoc = preset.ZCompLoc;
            bmdMaterial.Dither = preset.Dither;
            if (preset.NBTScale != null) bmdMaterial.NBTScale = preset.NBTScale;
        }

        private void LoadFromJson(Assimp.Scene scene, TEX1 textures, SHP1 shapes, string json_path)
        {
            JsonSerializer serial = new JsonSerializer();
            serial.Formatting = Formatting.Indented;
            serial.Converters.Add(new StringEnumConverter());

            using (StreamReader strm_reader = new StreamReader(json_path))
            {
                strm_reader.BaseStream.Seek(0, SeekOrigin.Begin);
                JsonTextReader reader = new JsonTextReader(strm_reader);
                Materials = serial.Deserialize<List<Material>>(reader);
            }

            for (short i = 0; i < Materials.Count; i++)
            {
                RemapIndices.Add(i);
            }

            foreach (Material mat in Materials)
            {
                MaterialNames.Add(mat.Name);
                for (int i = 0; i < 8; i++)
                {
                    if (mat.TextureNames[i] == "")
                        continue;

                    foreach (BinaryTextureImage tex in textures.Textures)
                    {
                        if (tex.Name == mat.TextureNames[i])
                            mat.TextureIndices[i] = textures.Textures.IndexOf(tex);
                    }
                }

                mat.Readjust();
            }

            for (int i = 0; i < scene.MeshCount; i++)
            {
                Assimp.Material meshMat = scene.Materials[scene.Meshes[i].MaterialIndex];
                string test = meshMat.Name.Replace("-material", "");

                List<string> materialNamesWithoutParentheses = new List<string>();
                foreach (string materialName in MaterialNames)
                {
                    materialNamesWithoutParentheses.Add(materialName.Replace("(", "_").Replace(")", "_"));
                }

                while (!materialNamesWithoutParentheses.Contains(test))
                {
                    if (test.Length <= 1)
                    {
                        throw new Exception($"Mesh \"{scene.Meshes[i].Name}\" has a material named \"{meshMat.Name.Replace("-material", "")}\" which was not found in materials.json.");
                    }
                    test = test.Substring(1);
                }

                for (int j = 0; j < Materials.Count; j++)
                {
                    if (test == materialNamesWithoutParentheses[j])
                    {
                        scene.Meshes[i].MaterialIndex = j;
                        break;
                    }
                }

                //m_RemapIndices[i] = scene.Meshes[i].MaterialIndex;
            }
        }

        private void LoadFromScene(Assimp.Scene scene, TEX1 textures, SHP1 shapes, List<Material> matPresets = null)
        {
            List<int> indices = new List<int>();

            for (short i = 0; i < scene.MeshCount; i++)
            {
                Assimp.Material meshMat = scene.Materials[scene.Meshes[i].MaterialIndex];
                Console.Write("Mesh {0} has material {1}...\n", scene.Meshes[i].Name, meshMat.Name);
                Material bmdMaterial = new();
                bmdMaterial.Name = meshMat.Name;

                bool hasVtxColor0 = shapes.Shapes[i].AttributeData.CheckAttribute(Geometry.GXVertexAttribute.Color0);
                int texIndex = -1;
                string texName = null;
                if (meshMat.HasTextureDiffuse)
                {
                    texName = Path.GetFileNameWithoutExtension(meshMat.TextureDiffuse.FilePath);
                    texIndex = textures.Textures.IndexOf(textures[texName]);
                }

                bmdMaterial.SetUpTev(meshMat.HasTextureDiffuse, hasVtxColor0, texIndex, texName, meshMat);
                string originalName = FindOriginalMaterialName(meshMat.Name, matPresets);
                if (originalName != null)
                {
                    Console.WriteLine("Material name {0} renamed to {1}", meshMat.Name, originalName);
                    meshMat.Name = originalName;
                }

                PresetResult? result = FindMatPreset(meshMat.Name, matPresets);


                if (result != null)
                {
                    Material preset = ((PresetResult)result).Preset;
                    if (preset.Name.StartsWith("__MatDefault:"))
                    {
                        // If a material has a suffix that fits one of the default presets, we remove the suffix as the
                        // suffix serves no further purpose
                        string[] subs = preset.Name.Split(delimiter, 2, StringSplitOptions.None);
                        string substring = "_" + subs[1];
                        bmdMaterial.Name = bmdMaterial.Name.Replace(substring, "");
                    }
                    Console.Write(string.Format("Applying material preset for {0}...", meshMat.Name));
                    SetPreset(bmdMaterial, preset);
                }
                else if (Arguments.IsMaterialOrderStrict)
                {
                    throw new Exception(String.Format("No material entry found for material {0}. In Strict Material Order mode every material needs to have an entry in the JSON!",
                                        meshMat.Name));
                }
                bmdMaterial.Readjust();

                Materials.Add(bmdMaterial);
                RemapIndices.Add(i);

                if (result != null)
                {
                    indices.Add(((PresetResult)result).Index);
                }
                else
                {
                    indices.Add(-1);
                }
                MaterialNames.Add(meshMat.Name);
                Console.WriteLine("✓\n");
            }
            if (Arguments.IsMaterialOrderStrict)
            {
                if (Materials.Count != matPresets.Count)
                {
                    throw new Exception($"Amount of materials doesn't match amount of presets: \"{Materials.Count}\" vs \"{matPresets.Count}\".");
                }
                List<Material> new_list = new List<Material>(Materials);
                List<String> names = new List<String>(MaterialNames);
                for (int i = 0; i < new_list.Count; i++)
                {
                    int index = indices[i];
                    if (index == -1)
                    {
                        throw new Exception("On resorting the materials, couldn't find one material in the material JSON. This shouldn't happen.");
                    }
                    scene.Meshes[i].MaterialIndex = index;
                    new_list[index] = Materials[i];
                    names[index] = MaterialNames[i];
                }
                Materials = new_list;
                MaterialNames = names;
                Console.WriteLine("Materials have been sorted according to their position in the material JSON file.");
            }
        }

        private void FillMaterialDataBlocks()
        {

            foreach (Material mat in Materials)
            {
                IndirectTexBlock.Add(mat.IndTexEntry);

                if (!CullModeBlock.Contains(mat.CullMode))
                    CullModeBlock.Add(mat.CullMode);

                for (int i = 0; i < 2; i++)
                {
                    if (mat.MaterialColors[i] is null)
                        break;
                    if (!MaterialColorBlock.Contains(mat.MaterialColors[i].Value))
                        MaterialColorBlock.Add(mat.MaterialColors[i].Value);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (mat.ChannelControls[i] is null)
                        break;
                    if (!ChannelControlBlock.Contains(mat.ChannelControls[i].Value))
                        ChannelControlBlock.Add(mat.ChannelControls[i].Value);
                }

                for (int i = 0; i < 2; i++)
                {
                    if (mat.AmbientColors[i] is null)
                        break;
                    if (!AmbientColorBlock.Contains(mat.AmbientColors[i].Value))
                        AmbientColorBlock.Add(mat.AmbientColors[i].Value);
                }

                for (int i = 0; i < 8; i++)
                {
                    if (mat.LightingColors[i] is null)
                        break;
                    if (!LightingColorBlock.Contains(mat.LightingColors[i].Value))
                        LightingColorBlock.Add(mat.LightingColors[i].Value);
                }

                for (int i = 0; i < 8; i++)
                {
                    if (mat.TexCoord1Gens[i] is null)
                        break;
                    if (!TexCoord1GenBlock.Contains(mat.TexCoord1Gens[i].Value))
                        TexCoord1GenBlock.Add(mat.TexCoord1Gens[i].Value);
                }

                for (int i = 0; i < 8; i++)
                {
                    if (mat.PostTexCoordGens[i] is null)
                        break;
                    if (!TexCoord2GenBlock.Contains(mat.PostTexCoordGens[i].Value))
                        TexCoord2GenBlock.Add(mat.PostTexCoordGens[i].Value);
                }

                for (int i = 0; i < 10; i++)
                {
                    if (mat.TexMatrix1[i] is null)
                        break;
                    if (!TexMatrix1Block.Contains(mat.TexMatrix1[i].Value))
                        TexMatrix1Block.Add(mat.TexMatrix1[i].Value);
                }

                for (int i = 0; i < 20; i++)
                {
                    if (mat.PostTexMatrix[i] is null)
                        break;
                    if (!TexMatrix2Block.Contains(mat.PostTexMatrix[i].Value))
                        TexMatrix2Block.Add(mat.PostTexMatrix[i].Value);
                }

                for (int i = 0; i < 8; i++)
                {
                    if (mat.TextureIndices[i] == -1)
                        break;
                    if (!TexRemapBlock.Contains((short)mat.TextureIndices[i]))
                        TexRemapBlock.Add((short)mat.TextureIndices[i]);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (mat.KonstColors[i] is null)
                        break;
                    if (!TevKonstColorBlock.Contains(mat.KonstColors[i].Value))
                        TevKonstColorBlock.Add(mat.KonstColors[i].Value);
                }

                for (int i = 0; i < 16; i++)
                {
                    if (mat.TevOrders[i] is null)
                        break;
                    if (!TevOrderBlock.Contains(mat.TevOrders[i].Value))
                        TevOrderBlock.Add(mat.TevOrders[i].Value);
                }

                for (int i = 0; i < 4; i++)
                {
                    if (mat.TevColors[i] is null)
                        break;
                    if (!TevColorBlock.Contains(mat.TevColors[i].Value))
                        TevColorBlock.Add(mat.TevColors[i].Value);
                }

                for (int i = 0; i < 16; i++)
                {
                    if (mat.TevStages[i] is null)
                        break;
                    if (!TevStageBlock.Contains(mat.TevStages[i].Value))
                        TevStageBlock.Add(mat.TevStages[i].Value);
                }

                for (int i = 0; i < 16; i++)
                {
                    if (mat.SwapModes[i] is null)
                        break;
                    if (!SwapModeBlock.Contains(mat.SwapModes[i].Value))
                        SwapModeBlock.Add(mat.SwapModes[i].Value);
                }

                for (int i = 0; i < 16; i++)
                {
                    if (mat.SwapTables[i] is null)
                        break;
                    if (!SwapTableBlock.Contains(mat.SwapTables[i].Value))
                        SwapTableBlock.Add(mat.SwapTables[i].Value);
                }

                if (!FogBlock.Contains(mat.FogInfo))
                    FogBlock.Add(mat.FogInfo);

                if (!AlphaCompBlock.Contains(mat.AlphCompare))
                    AlphaCompBlock.Add(mat.AlphCompare);

                if (!BlendModeBlock.Contains(mat.BlendMode))
                    BlendModeBlock.Add(mat.BlendMode);

                if (!NBTScaleBlock.Contains(mat.NBTScale))
                    NBTScaleBlock.Add(mat.NBTScale);

                if (!ZModeBlock.Contains(mat.ZMode))
                    ZModeBlock.Add(mat.ZMode);

                if (!ZCompLocBlock.Contains(mat.ZCompLoc))
                    ZCompLocBlock.Add(mat.ZCompLoc);

                if (!DitherBlock.Contains(mat.Dither))
                    DitherBlock.Add(mat.Dither);

                if (!NumColorChannelsBlock.Contains(mat.ColorChannelControlsCount))
                    NumColorChannelsBlock.Add(mat.ColorChannelControlsCount);

                if (!NumTevStagesBlock.Contains(mat.NumTevStagesCount))
                    NumTevStagesBlock.Add(mat.NumTevStagesCount);

                if (!NumTexGensBlock.Contains(mat.NumTexGensCount))
                    NumTexGensBlock.Add(mat.NumTexGensCount);
            }
        }

        public void FillScene(Assimp.Scene scene, TEX1 textures, string fileDir)
        {
            //textures.DumpTextures(fileDir);

            foreach (Material mat in Materials)
            {
                Console.Write(mat.Name + " - ");
                Assimp.Material assMat = new Assimp.Material();
                assMat.Name = mat.Name;
                if (mat.TextureIndices[0] != -1)
                {
                    int texIndex = mat.TextureIndices[0];
                    //texIndex = m_TexRemapBlock[texIndex];
                    string texPath = Path.Combine(fileDir, textures[texIndex].Name + ".png");

                    Assimp.TextureSlot tex = new(texPath, Assimp.TextureType.Diffuse, 0,
                       Assimp.TextureMapping.FromUV, 0, 1.0f, Assimp.TextureOperation.Add,
                       textures[texIndex].WrapS.ToAssImpWrapMode(), textures[texIndex].WrapT.ToAssImpWrapMode(), 0);

                    assMat.AddMaterialTexture(in tex);
                }

                if (mat.MaterialColors[0] != null)
                {
                    assMat.ColorDiffuse = mat.MaterialColors[0].Value.ToColor4D();
                }

                if (mat.AmbientColors[0] != null)
                {
                    assMat.ColorAmbient = mat.AmbientColors[0].Value.ToColor4D();
                }

                scene.Materials.Add(assMat);
                Console.Write("✓\n");

            }
        }

        public void Write(ref EndianBinaryWriter writer)
        {
            int start = writer.Position;

            // Calculate what the unique materials are and update the duplicate remap indices list.
            RemapIndices = new List<short>();
            List<Material> uniqueMaterials = new List<Material>();
            for (int i = 0; i < Materials.Count; i++)
            {
                Material mat = Materials[i];
                short duplicateRemapIndex = -1;
                for (int j = 0; j < i; j++)
                {
                    Material othermat = Materials[j];
                    if (mat == othermat)
                    {
                        duplicateRemapIndex = (short)uniqueMaterials.IndexOf(othermat);
                        break;
                    }
                }
                if (duplicateRemapIndex >= 0)
                {
                    RemapIndices.Add(duplicateRemapIndex);
                }
                else
                {
                    RemapIndices.Add((short)uniqueMaterials.Count);
                    uniqueMaterials.Add(mat);
                }
            }

            writer.Write("MAT3");
            writer.Write(0); // Placeholder for section offset
            writer.Write((short)RemapIndices.Count);
            writer.Write((short)-1);

            writer.Write(0x84); // Offset to material init data. Always 132

            for (int i = 0; i < 29; i++)
                writer.Write(0);

            bool[] writtenCheck = new bool[uniqueMaterials.Count];


            for (int i = 0; i < RemapIndices.Count; i++)
            {
                if (writtenCheck[RemapIndices[i]])
                    continue;
                else
                {
                    WriteMaterialInitData(ref writer, uniqueMaterials[RemapIndices[i]]);
                    writtenCheck[RemapIndices[i]] = true;
                }
            }

            //Write Remap indices offset
            int curPosition = writer.Position;
            writer.Seek(start + 16);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            for (int i = 0; i < RemapIndices.Count; i++)
            {
                writer.Write(RemapIndices[i]);
            }

            //Write Name Table offset
            curPosition = writer.Position;
            writer.Seek(start + 20);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            NameTableIO.Write(ref writer, MaterialNames);
            writer.PadAlign(8);

            //Write Indirect texturing offset
            curPosition = writer.Position;
            writer.Seek(start + 24);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            IndirectTexturingIO.Write(ref writer, IndirectTexBlock);

            //Write Cull mode offset
            curPosition = writer.Position;
            writer.Seek(start + 28);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            CullModeIO.Write(ref writer, CullModeBlock);

            //Write Material colors offset
            curPosition = writer.Position;
            writer.Seek(start + 32);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);

            ColorIO.Write(ref writer, MaterialColorBlock);
            //Write Color channel count offset
            curPosition = writer.Position;
            writer.Seek(start + 36);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            foreach (byte chanNum in NumColorChannelsBlock)
                writer.Write(chanNum);
            writer.PadAlign(4);

            //Write Color channel data offset
            curPosition = writer.Position;
            writer.Seek(start + 40);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            ColorChannelIO.Write(ref writer, ChannelControlBlock);


            //Write ambient color data offset
            curPosition = writer.Position;
            writer.Seek(start + 44);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            ColorIO.Write(ref writer, AmbientColorBlock);

            //Write light color data offset
            curPosition = writer.Position;
            writer.Seek(start + 48);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            if (LightingColorBlock != null)
                ColorIO.Write(ref writer, LightingColorBlock);


            //Write tex gen count data offset
            curPosition = writer.Position;
            writer.Seek(start + 52);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            foreach (byte texGenCnt in NumTexGensBlock)
                writer.Write(texGenCnt);
            writer.PadAlign(4);

            //Write tex coord 1 data offset
            curPosition = writer.Position;
            writer.Seek(start + 56);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            TexCoordGenIO.Write(ref writer, TexCoord1GenBlock);

            //Write tex coord 2 data offset
            curPosition = writer.Position;
            writer.Seek(start + 60);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            TexCoordGenIO.Write(ref writer, TexCoord2GenBlock);

            // tex matrix 1 data offset
            curPosition = writer.Position;
            writer.Seek(start + 64);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            TexMatrixIO.Write(ref writer, TexMatrix1Block);

            //Write tex matrix 2 data offset
            curPosition = writer.Position;
            writer.Seek(start + 68);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            if (TexMatrix2Block != null)
            {
                TexMatrixIO.Write(ref writer, TexMatrix2Block);
            }

            // tex number data offset
            curPosition = writer.Position;
            writer.Seek(start + 72);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            foreach (var inte in TexRemapBlock)
                writer.Write(inte);
            writer.PadAlign(4);

            // tev order data offset
            curPosition = writer.Position;
            writer.Seek(start + 76);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            TevOrderIO.Write(ref writer, TevOrderBlock);

            // tev color data offset
            curPosition = writer.Position;
            writer.Seek(start + 80);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            Int16ColorIO.Write(ref writer, TevColorBlock);

            // tev konst color data offset
            curPosition = writer.Position;
            writer.Seek(start + 84);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            ColorIO.Write(ref writer, TevKonstColorBlock);

            // tev stage count data offset
            curPosition = writer.Position;
            writer.Seek(start + 88);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            foreach (byte bt in NumTevStagesBlock)
                writer.Write(bt);
            writer.PadAlign(4);

            // tev stage data offset
            curPosition = writer.Position;
            writer.Seek(start + 92);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            TevStageIO.Write(ref writer, TevStageBlock);

            // tev swap mode offset
            curPosition = writer.Position;
            writer.Seek(start + 96);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            TevSwapModeIO.Write(ref writer, SwapModeBlock);

            // tev swap mode table offset
            curPosition = writer.Position;
            writer.Seek(start + 100);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            TevSwapModeTableIO.Write(ref writer, SwapTableBlock);

            // fog data offset
            curPosition = writer.Position;
            writer.Seek(start + 104);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            FogIO.Write(ref writer, FogBlock);

            // alpha compare offset
            curPosition = writer.Position;
            writer.Seek(start + 108);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            AlphaCompareIO.Write(ref writer, AlphaCompBlock);

            // blend data offset
            curPosition = writer.Position;
            writer.Seek(start + 112);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            BlendModeIO.Write(ref writer, BlendModeBlock);

            // zmode data offset
            curPosition = writer.Position;
            writer.Seek(start + 116);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            ZModeIO.Write(ref writer, ZModeBlock);

            // z comp loc data offset
            curPosition = writer.Position;
            writer.Seek(start + 120);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            foreach (bool bol in ZCompLocBlock)
                writer.Write(bol);
            writer.PadAlign(4);

            // Dither data offset
            curPosition = writer.Position;

            if (DitherBlock != null)
            {
                writer.Seek((int)start + 124);
                writer.Write((int)(curPosition - start));
                writer.Seek((int)curPosition);

                foreach (bool value in DitherBlock)
                    writer.Write(value);

                writer.PadAlign(4);
            }



            // NBT Scale data offset
            curPosition = writer.Position;
            writer.Seek(start + 128);
            writer.Write(curPosition - start);
            writer.Seek(curPosition);
            NBTScaleIO.Write(ref writer, NBTScaleBlock);
            writer.PadAlign(32);

            //Write MAT3 size
            int end = writer.Position;
            int length = (end - start);

            writer.Seek(start + 4);
            writer.Write(length);
            writer.Seek(end);
        }

        private void WriteMaterialInitData(ref EndianBinaryWriter writer, Material mat)
        {
            writer.Write(mat.Flag);
            writer.Write((byte)CullModeBlock.IndexOf(mat.CullMode));

            writer.Write((byte)NumColorChannelsBlock.IndexOf(mat.ColorChannelControlsCount));
            writer.Write((byte)NumTexGensBlock.IndexOf(mat.NumTexGensCount));
            writer.Write((byte)NumTevStagesBlock.IndexOf(mat.NumTevStagesCount));

            writer.Write((byte)ZCompLocBlock.IndexOf(mat.ZCompLoc));
            writer.Write((byte)ZModeBlock.IndexOf(mat.ZMode));
            writer.Write((byte)DitherBlock.IndexOf(mat.Dither));

            if (mat.MaterialColors[0].HasValue)
                writer.Write((short)MaterialColorBlock.IndexOf(mat.MaterialColors[0].Value));
            else
                writer.Write((short)-1);
            if (mat.MaterialColors[1].HasValue)
                writer.Write((short)MaterialColorBlock.IndexOf(mat.MaterialColors[1].Value));
            else
                writer.Write((short)-1);

            for (int i = 0; i < 4; i++)
            {
                if (mat.ChannelControls[i] != null)
                    writer.Write((short)ChannelControlBlock.IndexOf(mat.ChannelControls[i].Value));
                else
                    writer.Write((short)-1);
            }

            if (mat.AmbientColors[0].HasValue)
                writer.Write((short)AmbientColorBlock.IndexOf(mat.AmbientColors[0].Value));
            else
                writer.Write((short)-1);
            if (mat.AmbientColors[1].HasValue)
                writer.Write((short)AmbientColorBlock.IndexOf(mat.AmbientColors[1].Value));
            else
                writer.Write((short)-1);

            for (int i = 0; i < 8; i++)
            {
                //if (m_LightingColorBlock.Count != 0)
                if (mat.LightingColors[i] != null)
                    writer.Write((short)LightingColorBlock.IndexOf(mat.LightingColors[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 8; i++)
            {
                if (mat.TexCoord1Gens[i] != null)
                    writer.Write((short)TexCoord1GenBlock.IndexOf(mat.TexCoord1Gens[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 8; i++)
            {
                if (mat.PostTexCoordGens[i] != null)
                    writer.Write((short)TexCoord2GenBlock.IndexOf(mat.PostTexCoordGens[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 10; i++)
            {
                if (mat.TexMatrix1[i] != null)
                    writer.Write((short)TexMatrix1Block.IndexOf(mat.TexMatrix1[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 20; i++)
            {
                if (mat.PostTexMatrix[i] != null)
                    writer.Write((short)TexMatrix2Block.IndexOf(mat.PostTexMatrix[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 8; i++)
            {
                if (mat.TextureIndices[i] != -1)
                    writer.Write((short)TexRemapBlock.IndexOf((short)mat.TextureIndices[i]));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 4; i++)
            {
                if (mat.KonstColors[i] != null)
                    writer.Write((short)TevKonstColorBlock.IndexOf(mat.KonstColors[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 16; i++)
            {
                writer.Write((byte)mat.ColorSels[i]);
            }

            for (int i = 0; i < 16; i++)
            {
                writer.Write((byte)mat.AlphaSels[i]);
            }

            for (int i = 0; i < 16; i++)
            {
                if (mat.TevOrders[i] != null)
                    writer.Write((short)TevOrderBlock.IndexOf(mat.TevOrders[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 4; i++)
            {
                if (mat.TevColors[i] != null)
                    writer.Write((short)TevColorBlock.IndexOf(mat.TevColors[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 16; i++)
            {
                if (mat.TevStages[i] != null)
                    writer.Write((short)TevStageBlock.IndexOf(mat.TevStages[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 16; i++)
            {
                if (mat.SwapModes[i] != null)
                    writer.Write((short)SwapModeBlock.IndexOf(mat.SwapModes[i].Value));
                else
                    writer.Write((short)-1);
            }

            for (int i = 0; i < 16; i++)
            {
                if (mat.SwapTables[i] != null)
                    writer.Write((short)SwapTableBlock.IndexOf(mat.SwapTables[i].Value));
                else
                    writer.Write((short)-1);
            }

            writer.Write((short)FogBlock.IndexOf(mat.FogInfo));
            writer.Write((short)AlphaCompBlock.IndexOf(mat.AlphCompare));
            writer.Write((short)BlendModeBlock.IndexOf(mat.BlendMode));
            writer.Write((short)NBTScaleBlock.IndexOf(mat.NBTScale));
        }

        public void DumpMaterials(string out_path)
        {
            JsonSerializer serial = new JsonSerializer();
            serial.Formatting = Formatting.Indented;
            serial.Converters.Add(new StringEnumConverter());
            Directory.CreateDirectory(Path.GetDirectoryName(out_path));
            using (FileStream strm = new FileStream(out_path, FileMode.Create, FileAccess.Write))
            {
                StreamWriter writer = new StreamWriter(strm);
                writer.AutoFlush = true;
                serial.Serialize(writer, Materials);
            }
        }

        public void LoadAdditionalTextures(TEX1 tex1, string texpath)
        {
            //string modeldir = Path.GetDirectoryName(modelpath);
            foreach (Material mat in Materials)
            {
                foreach (string texname in mat.TextureNames)
                {
                    if (texname != null && texname != "")
                    {
                        if (tex1[texname] is null || texname == "")
                        {
                            var textureName = texname.Split(":")[0];
                            Console.Write("Searching for " + textureName);
                            string path = "";
                            foreach (string extension in new string[] { ".png", ".jpg", ".tga", ".bmp" })
                            {
                                Console.Write(".");
                                string tmppath = Path.Combine(texpath, texname + extension);
                                if (File.Exists(tmppath))
                                {
                                    path = tmppath;
                                    break;
                                }
                            }

                            if (path != "")
                            {
                                tex1.AddTextureFromPath(path);
                                short texindex = (short)(tex1.Textures.Count - 1);
                                TexRemapBlock.Add(texindex);
                                Console.WriteLine("----------------------------------------");
                            }
                            else
                            {
                                Console.WriteLine(string.Format("Could not find texture {0} in file path {1}", texname, texpath));
                            }
                        }
                    }
                }
            }
        }

        public void MapTextureNamesToIndices(TEX1 textures)
        {
            //Console.WriteLine("Mapping names to indices");
            foreach (Material mat in Materials)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (mat.TextureNames[i] != null && mat.TextureNames[i] != "")
                    {

                        int index = textures.getTextureIndexFromInstanceName(mat.TextureNames[i]);
                        if (index < 0)
                        {
                            Console.WriteLine("Failed to get texture index for texture {0} in material {1}", mat.TextureNames[i], mat.Name);
                        }
                        else
                        {
                            mat.TextureIndices[i] = index;
                            BinaryTextureImage tex = textures[index];
                            if (!TexRemapBlock.Contains((short)index))
                            {
                                TexRemapBlock.Add((short)index);
                            }
                            Console.WriteLine(string.Format("Mapped \"{0}\" to index {1} ({2})", mat.TextureNames[i], index, tex.Name));
                            Console.WriteLine("---------------------------------------------------");
                        }
                        /*foreach (BinaryTextureImage tex in textures.Textures) {
                            if (tex.Name == mat.TextureNames[i]) {
                                mat.TextureIndices[i] = j;
                                if (!m_TexRemapBlock.Contains((short)j)) {
                                    m_TexRemapBlock.Add((short)j);
                                }
                                Console.WriteLine(string.Format("Mapped \"{0}\" to index {1}", tex.Name, j));
                                Console.WriteLine("---------------------------------------------------");
                                break;
                            }
                            j++;
                        }*/
                    }
                }
            }
        }


        public void SetTextureNames(TEX1 textures)
        {
            foreach (Material mat in Materials)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (mat.TextureIndices[i] == -1)
                        continue;

                    //mat.TextureNames[i] = textures[mat.TextureIndices[i]].Name;
                    mat.TextureNames[i] = textures.getTextureInstanceName(mat.TextureIndices[i]);
                }
            }
        }
    }
}
