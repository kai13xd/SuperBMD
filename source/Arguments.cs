namespace SuperBMD
{
    /// <summary>
    /// Container for arguments taken from the user's input.
    /// </summary>
    public struct Arguments
    {
        public string InputPath;
        public string OutputPath;
        public string MaterialsPath;
        public string OutputMaterialPath;
        public string TexheadersPath;
        public string TriStripMode;
        public bool RotateModel;
        public bool ExportBDL;
        public bool DoProfile;
        public bool SortMeshes;
        public bool EnsureOneMaterialPerMesh;
        public bool ExportObj;
        public bool ForceFloat;
        public bool DegenerateTriangles;
        public bool ReadMipmaps;
        public bool DumpHierarchy;
        public string HierarchyPath;
        public bool ExportAnims;
        public Geometry.GXDataType VertexType;
        public byte Fraction;
        public bool MaterialOrderStrict;

        /// <summary>
        /// Initializes a new Arguments instance from the arguments passed in to SuperBMD.
        /// </summary>
        /// <param name="args">Arguments from the user</param>
        public Arguments(string[] args)
        {
            InputPath = "";
            OutputPath = "";
            MaterialsPath = "";
            OutputMaterialPath = "";
            TexheadersPath = "";
            TriStripMode = "static";
            RotateModel = false;
            ExportBDL = false;
            DoProfile = false;
            SortMeshes = true;
            EnsureOneMaterialPerMesh = false;
            ExportObj = false;
            ForceFloat = false;
            DegenerateTriangles = false;
            ReadMipmaps = true;
            DumpHierarchy = false;
            HierarchyPath = "";
            ExportAnims = false;
            VertexType = Geometry.GXDataType.Float32;
            Fraction = 0;
            MaterialOrderStrict = false;
            int positionalArguments = 0;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-m":
                    case "--mat":
                        if (i + 1 >= args.Length)
                            throw new Exception("The parameters were malformed.");

                        MaterialsPath = args[i + 1];
                        i++;
                        break;
                    case "--outmat":
                        if (i + 1 >= args.Length)
                            throw new Exception("The parameters were malformed.");

                        OutputMaterialPath = args[i + 1];
                        i++;
                        break;
                    case "-x":
                    case "--texheader":
                        if (i + 1 >= args.Length)
                            throw new Exception("The parameters were malformed.");

                        TexheadersPath = args[i + 1];
                        i++;
                        break;
                    case "-t":
                    case "--tristrip":
                        if (i + 1 >= args.Length)
                            throw new Exception("The parameters were malformed.");

                        TriStripMode = args[i + 1].ToLower();
                        i++;
                        break;
                    case "-r":
                    case "--rotate":
                        RotateModel = true;
                        break;
                    case "-b":
                    case "--bdl":
                        ExportBDL = true;
                        break;
                    case "--p":
                    case "--profile":
                        DoProfile = true;
                        break;
                    case "--nosort":
                        SortMeshes = false;
                        break;
                    case "--onematpermesh":
                        EnsureOneMaterialPerMesh = true;
                        break;
                    case "--obj":
                        ExportObj = true;
                        break;
                    case "--texfloat32":
                        ForceFloat = true;
                        break;
                    case "--degeneratetri":
                        DegenerateTriangles = true;
                        break;
                    case "--nomipmaps":
                        ReadMipmaps = false;
                        break;
                    case "--dumphierarchy":
                        DumpHierarchy = true;
                        break;
                    case "--hierarchy":
                        if (i + 1 >= args.Length)
                            throw new Exception("The parameters were malformed.");

                        HierarchyPath = args[i + 1];
                        i++;
                        break;
                    case "-a":
                    case "--animation":
                        ExportAnims = true;
                        break;
                    case "--vtxpos":
                        if (i + 2 >= args.Length)
                            throw new Exception("The parameters were malformed.");
                        VertexType = (Geometry.GXDataType)Enum.Parse(typeof(Geometry.GXDataType), args[i + 1]);
                        Fraction = byte.Parse(args[i + 2]);
                        i += 2;
                        break;
                    case "--mat_strict":
                        MaterialOrderStrict = true;
                        break;
                    default:
                        if (positionalArguments == 0)
                        {
                            positionalArguments += 1;
                            InputPath = args[i];
                            break;
                        }
                        else if (positionalArguments == 1)
                        {
                            positionalArguments += 1;
                            OutputPath = args[i];
                            break;
                        }
                        else
                        {
                            throw new Exception($"Unknown parameter \"{args[i]}\"");
                        }
                }
            }
            ValidateArgs();
        }

        /// <summary>
        /// Ensures that all the settings parsed from the user's input are valid.
        /// </summary>
        /// <param name="args">Array of settings parsed from the user's input</param>
        private void ValidateArgs()
        {
            // Input
            if (InputPath == "")
                throw new Exception("No input file was specified.");
            if (!File.Exists(InputPath))
                throw new Exception($"Input file \"{InputPath}\" does not exist.");

            // Output
            if (OutputPath == "")
            {
                string dirName = Path.GetDirectoryName(InputPath);
                string fileName = Path.GetFileNameWithoutExtension(InputPath);


                if (InputPath.EndsWith(".bmd") || InputPath.EndsWith(".bdl"))
                    OutputPath = $"{dirName}/{fileName}.bdl";
                else
                    OutputPath = $"{dirName}/{fileName}.bmd";
            }

            // Material presets
            if (MaterialsPath != "")
            {
                if (!File.Exists(MaterialsPath))
                    throw new Exception($"Material presets file \"{MaterialsPath}\" does not exist.");
            }

            // Texture headers
            if (TexheadersPath != "")
            {
                if (!File.Exists(TexheadersPath))
                    throw new Exception($"Texture headers file \"{TexheadersPath}\" does not exist.");
            }

            // Tristrip options
            if (TriStripMode != "static" && TriStripMode != "all" && TriStripMode != "none")
                throw new Exception($"Unknown tristrip option \"{TriStripMode}\".");
        }
    }
}
