﻿namespace SuperBMD
{
    public class BMDMaterial
    {
        public string Name { get; set; }
        public byte Flag { get; set; }
        [JsonIgnore]
        public byte ColorChannelControlsCount { get; set; }
        [JsonIgnore]
        public byte NumTexGensCount { get; set; }
        [JsonIgnore]
        public byte NumTevStagesCount { get; set; }
        public CullMode CullMode { get; set; } = CullMode.Back;
        public bool ZCompLoc { get; set; }
        public bool Dither { get; set; }
        [JsonIgnore]
        public int[] TextureIndices { get; set; } = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
        public string[] TextureNames { get; set; } = new string[8] { "", "", "", "", "", "", "", "" };
        public IndirectTexturing IndTexEntry { get; set; } = new IndirectTexturing();
        public Color?[] MaterialColors { get; set; } = new Color?[2] { new Color(1, 1, 1, 1), null };
        public ChannelControl?[] ChannelControls { get; set; } = new ChannelControl?[4];
        public Color?[] AmbientColors { get; set; } = new Color?[2] { new Color(50f / 255f, 50f / 255f, 50f / 255f, 50f / 255f), null };
        public Color?[] LightingColors { get; set; } = new Color?[8];
        public TexCoordGen?[] TexCoord1Gens { get; set; } = new TexCoordGen?[8];
        public TexCoordGen?[] PostTexCoordGens { get; set; } = new TexCoordGen?[8];
        public TexMatrix?[] TexMatrix1 { get; set; } = new TexMatrix?[10];
        public TexMatrix?[] PostTexMatrix { get; set; } = new TexMatrix?[20];
        public TevOrder?[] TevOrders { get; set; } = new TevOrder?[16];
        public KonstColorSel[] ColorSels { get; set; } = new KonstColorSel[16];
        public KonstAlphaSel[] AlphaSels { get; set; } = new KonstAlphaSel[16];
        public Color?[] TevColors { get; set; } = new Color?[4] { Color.White, null, null, null };
        public Color?[] KonstColors { get; set; } = new Color?[4] { Color.White, null, null, null };
        public TevStage?[] TevStages { get; set; } = new TevStage?[16];
        public TevSwapMode?[] SwapModes { get; set; } = new TevSwapMode?[16] { new TevSwapMode(0, 0), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
        public TevSwapModeTable?[] SwapTables { get; set; } = new TevSwapModeTable?[16] { new TevSwapModeTable(0, 1, 2, 3), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
        public Fog FogInfo { get; set; } = new Fog(0, false, 0, 0, 0, 0, 0, new Color(0, 0, 0, 0), new float[10]);
        public AlphaCompare AlphCompare { get; set; } = new AlphaCompare(CompareType.Greater, 127, AlphaOp.And, CompareType.Always, 0);
        public Materials.BlendMode BlendMode { get; set; } = new Materials.BlendMode(BlendModeType.Blend, BlendModeControl.SrcAlpha, BlendModeControl.InverseSrcAlpha, LogicOp.NoOp);
        public ZMode ZMode { get; set; } = new ZMode(true, CompareType.LEqual, true);
        public NBTScale NBTScale { get; set; } = new NBTScale(0, Vector3.Zero);
        public BMDMaterial() { }
        public void SetUpTev(bool hasTexture, bool hasVtxColor, int texIndex, string textureName, Assimp.Material meshMaterial)
        {
            Flag = 1;
            // Set up channel control 0 to use vertex colors, if they're present
            if (hasVtxColor)
            {
                AddChannelControl(J3DColorChannelId.Color0, false, ColorSrc.Vertex, LightId.None, DiffuseFn.None, J3DAttenuationFn.None_0, ColorSrc.Register);
                AddChannelControl(J3DColorChannelId.Alpha0, false, ColorSrc.Vertex, LightId.None, DiffuseFn.None, J3DAttenuationFn.None_0, ColorSrc.Register);
            }
            else
            {
                AddChannelControl(J3DColorChannelId.Color0, false, ColorSrc.Register, LightId.None, DiffuseFn.Clamp, J3DAttenuationFn.Spec, ColorSrc.Register);
                AddChannelControl(J3DColorChannelId.Alpha0, false, ColorSrc.Register, LightId.None, DiffuseFn.Clamp, J3DAttenuationFn.Spec, ColorSrc.Register);
            }

            // These settings are common to all the configurations we can use
            var tevStageParameters = new TevStageParameters
            {
                ColorInD = CombineColorInput.Zero,
                ColorOp = TevOp.Add,
                ColorBias = TevBias.Zero,
                ColorScale = TevScale.Scale_1,
                ColorClamp = true,
                ColorRegId = TevRegisterId.TevPrev,

                AlphaInD = CombineAlphaInput.Zero,
                AlphaOp = TevOp.Add,
                AlphaBias = TevBias.Zero,
                AlphaScale = TevScale.Scale_1,
                AlphaClamp = true,
                AlphaRegId = TevRegisterId.TevPrev
            };

            if (hasTexture)
            {
                // Generate texture stuff
                AddTexGen(TexGenType.Matrix2x4, TexGenSrc.Tex0, TexMatrixType.Identity);
                AddTexMatrix(TexGenType.Matrix3x4, 0, Vector3.Zero, Vector2.One, 0, Vector2.Zero, Matrix4.Identity);
                AddTevOrder(TexCoordId.TexCoord0, TexMapId.TexMap0, GXColorChannelId.Color0A0);
                AddTexIndex(texIndex);

                // Texture + Vertex Color
                if (hasVtxColor)
                {
                    tevStageParameters.ColorInA = CombineColorInput.Zero;
                    tevStageParameters.ColorInB = CombineColorInput.RasColor;
                    tevStageParameters.ColorInC = CombineColorInput.TexColor;
                    tevStageParameters.AlphaInA = CombineAlphaInput.Zero;
                    tevStageParameters.AlphaInB = CombineAlphaInput.RasAlpha;
                    tevStageParameters.AlphaInC = CombineAlphaInput.TexAlpha;
                }
                // Texture alone
                else
                {
                    tevStageParameters.ColorInA = CombineColorInput.TexColor;
                    tevStageParameters.ColorInB = CombineColorInput.Zero;
                    tevStageParameters.ColorInC = CombineColorInput.Zero;
                    tevStageParameters.AlphaInA = CombineAlphaInput.TexAlpha;
                    tevStageParameters.AlphaInB = CombineAlphaInput.Zero;
                    tevStageParameters.AlphaInC = CombineAlphaInput.Zero;
                }
            }
            // No texture!
            else
            {
                AddTevOrder(TexCoordId.Null, TexMapId.Null, GXColorChannelId.Color0A0);

                // No vertex colors either, so make sure there's a material color to use instead
                if (!hasVtxColor)
                {
                    if (meshMaterial.HasColorDiffuse)
                    { // Use model's diffuse color
                        Assimp.Color4D color = meshMaterial.ColorDiffuse;
                        MaterialColors[0] = new Color(color.R, color.G, color.B, color.A);
                    }
                    else
                    { // Otherwise default to white
                        MaterialColors[0] = new Color(1, 1, 1, 1);
                    }

                    AddChannelControl(J3DColorChannelId.Color0, false, ColorSrc.Register, LightId.None, DiffuseFn.None, J3DAttenuationFn.None_0, ColorSrc.Register);
                    AddChannelControl(J3DColorChannelId.Alpha0, false, ColorSrc.Register, LightId.None, DiffuseFn.None, J3DAttenuationFn.None_0, ColorSrc.Register);
                }

                // Set up TEV to use the material color we just set
                tevStageParameters.ColorInA = CombineColorInput.RasColor;
                tevStageParameters.ColorInB = CombineColorInput.Zero;
                tevStageParameters.ColorInC = CombineColorInput.Zero;
                tevStageParameters.AlphaInA = CombineAlphaInput.RasAlpha;
                tevStageParameters.AlphaInB = CombineAlphaInput.Zero;
                tevStageParameters.AlphaInC = CombineAlphaInput.Zero;
            }

            AddTevStage(tevStageParameters);
        }

        public void AddChannelControl(J3DColorChannelId id, bool enable, ColorSrc MatSrcColor, LightId litId, DiffuseFn diffuse, J3DAttenuationFn atten, ColorSrc ambSrcColor)
        {
            ChannelControl control = new ChannelControl
            {
                Enable = enable,
                MaterialSrcColor = MatSrcColor,
                LitMask = litId,
                DiffuseFunction = diffuse,
                AttenuationFunction = atten,
                AmbientSrcColor = ambSrcColor
            };

            ChannelControls[(int)id] = control;
        }

        public void AddTexGen(TexGenType genType, TexGenSrc genSrc, TexMatrixType mtrx)
        {
            var texCoordGen = new TexCoordGen(genType, genSrc, mtrx);

            for (int i = 0; i < 8; i++)
            {
                if (TexCoord1Gens[i] is null)
                {
                    TexCoord1Gens[i] = texCoordGen;
                    break;
                }

                if (i == 7)
                    throw new Exception($"TexGen array for material \"{Name}\" is full!");
            }

            NumTexGensCount++;
        }

        public void AddTexMatrix(TexGenType projection, byte type, Vector3 effectTranslation, Vector2 scale, float rotation, Vector2 translation, Matrix4 matrix)
        {
            for (int i = 0; i < 10; i++)
            {
                if (TexMatrix1[i] is null)
                {
                    TexMatrix1[i] = new TexMatrix(projection, type, effectTranslation, scale, rotation, translation, matrix);
                    break;
                }

                if (i == 9)
                    throw new Exception($"TexMatrix1 array for material \"{Name}\" is full!");
            }
        }

        public void AddTexIndex(int index)
        {
            for (int i = 0; i < 8; i++)
            {
                if (TextureIndices[i] == -1)
                {
                    TextureIndices[i] = index;
                    break;
                }

                if (i == 7)
                    throw new Exception($"TextureIndex array for material \"{Name}\" is full!");
            }
        }

        public void AddTevOrder(TexCoordId coordId, TexMapId mapId, GXColorChannelId colorChanId)
        {
            for (int i = 0; i < 8; i++)
            {
                if (TevOrders[i] is null)
                {
                    TevOrders[i] = new TevOrder(coordId, mapId, colorChanId);
                    break;
                }

                if (i == 7)
                    throw new Exception($"TevOrder array for material \"{Name}\" is full!");
            }
        }

        public void AddTevStage(TevStageParameters parameters)
        {
            for (int i = 0; i < 16; i++)
            {
                if (TevStages[i] is null)
                {
                    TevStages[i] = new TevStage(parameters);
                    break;
                }

                if (i == 15)
                    throw new Exception($"TevStage array for material \"{Name}\" is full!");
            }

            NumTevStagesCount++;
        }

        private TevStageParameters SetUpTevStageParametersForTexture()
        {
            TevStageParameters parameters = new TevStageParameters
            {
                ColorInA = CombineColorInput.TexColor,
                ColorInB = CombineColorInput.TexColor,
                ColorInC = CombineColorInput.Zero,
                ColorInD = CombineColorInput.Zero,

                ColorOp = TevOp.Add,
                ColorBias = TevBias.Zero,
                ColorScale = TevScale.Scale_1,
                ColorClamp = true,
                ColorRegId = TevRegisterId.TevPrev,

                AlphaInA = CombineAlphaInput.TexAlpha,
                AlphaInB = CombineAlphaInput.TexAlpha,
                AlphaInC = CombineAlphaInput.Zero,
                AlphaInD = CombineAlphaInput.Zero,

                AlphaOp = TevOp.Add,
                AlphaBias = TevBias.Zero,
                AlphaScale = TevScale.Scale_1,
                AlphaClamp = true,
                AlphaRegId = TevRegisterId.TevPrev
            };

            return parameters;
        }

        public BMDMaterial(BMDMaterial src)
        {
            Flag = src.Flag;
            ColorChannelControlsCount = src.ColorChannelControlsCount;
            NumTevStagesCount = src.NumTevStagesCount;
            NumTexGensCount = src.NumTexGensCount;
            CullMode = src.CullMode;
            ZCompLoc = src.ZCompLoc;
            Dither = src.Dither;
            TextureIndices = src.TextureIndices;
            TextureNames = src.TextureNames;
            IndTexEntry = src.IndTexEntry;
            MaterialColors = src.MaterialColors;
            ChannelControls = src.ChannelControls;
            AmbientColors = src.AmbientColors;
            LightingColors = src.LightingColors;
            TexCoord1Gens = src.TexCoord1Gens;
            PostTexCoordGens = src.PostTexCoordGens;
            TexMatrix1 = src.TexMatrix1;
            PostTexMatrix = src.PostTexMatrix;
            TevOrders = src.TevOrders;
            ColorSels = src.ColorSels;
            AlphaSels = src.AlphaSels;
            TevColors = src.TevColors;
            KonstColors = src.KonstColors;
            TevStages = src.TevStages;
            SwapModes = src.SwapModes;
            SwapTables = src.SwapTables;

            FogInfo = src.FogInfo;
            AlphCompare = src.AlphCompare;
            BlendMode = src.BlendMode;
            ZMode = src.ZMode;
            NBTScale = src.NBTScale;
        }

        public void Debug_Print()
        {
            Console.WriteLine($"TEV stage count: {NumTevStagesCount}\n\n");

            for (int i = 0; i < 16; i++)
            {
                if (TevStages[i] is null)
                    continue;

                Console.WriteLine($"Stage {i}:");
                Console.WriteLine(TevStages[i].ToString());
            }
        }

        public void Readjust()
        {
            NumTevStagesCount = 0;
            NumTexGensCount = 0;

            for (int i = 0; i < 16; i++)
            {
                if (TevStages[i] != null)
                    NumTevStagesCount++;
            }

            for (int i = 0; i < 8; i++)
            {
                if (TexCoord1Gens[i] != null)
                    NumTexGensCount++;
            }

            // Note: Despite the name, this doesn't seem to control the number of color channel controls.
            // At least in Wind Waker, every single model has 1 for this value regardless of how many color channel controls it has.
            ColorChannelControlsCount = 2;
        }

        /*         public static bool operator ==(Material left, Material right)
                {
                    return object.ReferenceEquals(left, right);

                    if (object.ReferenceEquals(left, null) && object.ReferenceEquals(right, null)) {
                        return true;
                    }

                    if (object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null)) {
                        return false;
                    }

                    if (left.Flag != right.Flag)
                        return false;
                    if (left.CullMode != right.CullMode)
                        return false;
                    if (left.ColorChannelControlsCount != right.ColorChannelControlsCount)
                        return false;
                    if (left.NumTexGensCount != right.NumTexGensCount)
                        return false;
                    if (left.NumTevStagesCount != right.NumTevStagesCount)
                        return false;
                    if (left.ZCompLoc != right.ZCompLoc)
                        return false;
                    if (left.ZMode != right.ZMode)
                        return false;
                    if (left.Dither != right.Dither)
                        return false;

                    for (int i = 0; i < 2; i++)
                    {
                        if (left.MaterialColors[i] != right.MaterialColors[i])
                            return false;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (left.ChannelControls[i] != right.ChannelControls[i])
                            return false;
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        if (left.AmbientColors[i] != right.AmbientColors[i])
                            return false;
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        if (left.LightingColors[i] != right.LightingColors[i])
                            return false;
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        if (left.TexCoord1Gens[i] != right.TexCoord1Gens[i]) // TODO: does != actually work on these types of things?? might need custom operators
                            return false;
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        if (left.PostTexCoordGens[i] != right.PostTexCoordGens[i])
                            return false;
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        if (left.TexMatrix1[i] != right.TexMatrix1[i])
                            return false;
                    }
                    for (int i = 0; i < 20; i++)
                    {
                        if (left.PostTexMatrix[i] != right.PostTexMatrix[i])
                            return false;
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        if (left.TextureNames[i] != right.TextureNames[i])
                            return false;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (left.KonstColors[i] != right.KonstColors[i])
                            return false;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        if (left.ColorSels[i] != right.ColorSels[i])
                            return false;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        if (left.AlphaSels[i] != right.AlphaSels[i])
                            return false;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        if (left.TevOrders[i] != right.TevOrders[i])
                            return false;
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        if (left.TevColors[i] != right.TevColors[i])
                            return false;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        if (left.TevStages[i] != right.TevStages[i])
                            return false;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        if (left.SwapModes[i] != right.SwapModes[i])
                            return false;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        if (left.SwapTables[i] != right.SwapTables[i])
                            return false;
                    }

                    if (left.FogInfo != right.FogInfo)
                        return false;
                    if (left.AlphCompare != right.AlphCompare)
                        return false;
                    if (left.BMode != right.BMode)
                        return false;
                    if (left.NBTScale != right.NBTScale)
                        return false;

                    return true;
                } */

        // public static bool operator !=(Material left, Material right)
        // {
        //     if (left == right)
        //         return false;
        //     else
        //        return true;
        // }
    }
}
