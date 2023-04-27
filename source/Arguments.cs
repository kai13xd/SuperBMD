namespace SuperBMD
{
    /// <summary>
    /// Static class that holds arguments taken from the user's input.
    /// </summary>
    public static class Arguments
    {
        public static string InputPath { get; private set; } = "";
        public static string OutputPath { get; private set; } = "";
        public static string MaterialPath { get; private set; } = "";
        public static string OutputMaterialPath { get; private set; } = "";
        public static string TexHeaderPath { get; private set; } = "";
        public static string TriStripMode { get; private set; } = "static";
        public static bool ShouldRotateModel { get; private set; } = false;
        public static bool ShouldExportAsBDL { get; private set; } = false;
        public static bool ShouldProfile { get; private set; } = false;
        public static bool ShouldSortMeshes { get; private set; } = true;
        public static bool ShouldEnsureOneMaterialPerMesh { get; private set; } = false;
        public static bool ShouldExportAsObj { get; private set; } = false;
        public static bool ShouldForceFloat { get; private set; } = false;
        public static bool ShouldDegenerateTriangles { get; private set; } = false;
        public static bool ShouldReadMipmaps { get; private set; } = true;
        public static bool ShouldDumpHierarchy { get; private set; } = false;
        public static string HierarchyPath { get; private set; } = "";
        public static bool ShouldExportAnims { get; private set; } = false;
        public static bool ShouldExportColorAsBytes { get; private set; } = true;
        public static bool ShouldExportColorAsHexString { get; private set; } = false;
        public static StorageType VertexType { get; private set; } = StorageType.Float32;
        public static byte Fraction { get; private set; } = 0;
        public static bool IsMaterialOrderStrict { get; private set; } = false;


        /// <summary>
        /// Initializes Arguments instance from the arguments passed in to SuperBMD.
        /// </summary>
        /// <param name="args">Arguments from the user</param>
        public static void ParseArguments(string[] args)
        {
            int positionalArguments = 0;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-m":
                    case "--mat":
                        if (i + 1 >= args.Length)
                            throw new Exception("Not enough parameters have been specified for given cmd arguments.");
                        MaterialPath = args[i + 1];

                        i++;
                        break;
                    case "--outmat":
                        if (i + 1 >= args.Length)
                            throw new Exception("Not enough parameters have been specified for given cmd arguments.");
                        OutputMaterialPath = args[i + 1];
                        i++;
                        break;
                    case "-x":
                    case "--texheader":
                        if (i + 1 >= args.Length)
                            throw new Exception("Not enough parameters have been specified for given cmd arguments.");

                        TexHeaderPath = args[i + 1];

                        i++;
                        break;
                    case "-t":
                    case "--tristrip":
                        if (i + 1 >= args.Length)
                            throw new Exception("Not enough parameters have been specified for given cmd arguments.");

                        TriStripMode = args[i + 1].ToLower();
                        i++;
                        break;
                    case "-r":
                    case "--rotate":
                        ShouldRotateModel = true;
                        break;
                    case "-b":
                    case "--bdl":
                        ShouldExportAsBDL = true;
                        break;
                    case "--p":
                    case "--profile":
                        ShouldProfile = true;
                        break;
                    case "--nosort":
                        ShouldSortMeshes = false;
                        break;
                    case "--onematpermesh":
                        ShouldEnsureOneMaterialPerMesh = true;
                        break;
                    case "--obj":
                        ShouldExportAsObj = true;
                        break;
                    case "--texfloat32":
                        ShouldForceFloat = true;
                        break;
                    case "--degeneratetri":
                        ShouldDegenerateTriangles = true;
                        break;
                    case "--nomipmaps":
                        ShouldReadMipmaps = false;
                        break;
                    case "--dumphierarchy":
                        ShouldDumpHierarchy = true;
                        break;
                    case "--hierarchy":
                        if (i + 1 >= args.Length)
                            throw new Exception("Not enough parameters have been specified for given cmd arguments.");

                        HierarchyPath = args[i + 1];
                        i++;
                        break;
                    case "-a":
                    case "--animation":
                        ShouldExportAnims = true;
                        break;
                    case "--vtxpos":
                        if (i + 2 >= args.Length)
                            throw new Exception("Not enough parameters have been specified for given cmd arguments.");
                        VertexType = (Geometry.StorageType)Enum.Parse(typeof(Geometry.StorageType), args[i + 1]);
                        Fraction = byte.Parse(args[i + 2]);
                        i += 2;
                        break;
                    case "--mat_strict":
                        IsMaterialOrderStrict = true;
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
        private static void ValidateArgs()
        {

            // Input
            if (InputPath.IsEmpty())
                throw new Exception("No input file was specified.");
            if (!File.Exists(InputPath))
                throw new Exception($"Input file \"{InputPath}\" does not exist.");

            // Output
            if (OutputPath.IsEmpty())
            {
                string dirName = Path.GetDirectoryName(InputPath);
                string fileName = Path.GetFileNameWithoutExtension(InputPath);


                if (InputPath.EndsWith(".bmd") || InputPath.EndsWith(".bdl"))
                    OutputPath = $"{dirName}/{fileName}.bdl";
                else
                    OutputPath = $"{dirName}/{fileName}.bmd";
            }

            // Material presets
            if (!MaterialPath.IsEmpty())
            {
                if (!File.Exists(MaterialPath))
                    throw new Exception($"Material presets file \"{MaterialPath}\" does not exist.");
            }

            // Texture headers
            if (!TexHeaderPath.IsEmpty())
            {
                if (!File.Exists(TexHeaderPath))
                    throw new Exception($"Texture headers file \"{TexHeaderPath}\" does not exist.");
            }

            // Tristrip options
            string[] triStripOptions = { "none", "all", "static" };
            if (!triStripOptions.Contains(TriStripMode))
            {
                throw new Exception($"'{TriStripMode}' is not a valid -t/--tristrip option! Did you mean 'none', 'all', or 'static'?");
            }
        }
    }
}
