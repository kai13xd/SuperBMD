global using OpenTK;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System;
global using System.Linq;
global using System.Text;
global using System.Collections.Generic;
global using System.Globalization;
global using System.Reflection;
global using System.IO;
global using SuperBMD.Geometry;
global using Kai;

using SuperBMD.Materials;

namespace SuperBMD
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "SuperBMD Console";
            Assembly assembly = typeof(Program).Assembly;
            Console.WriteLine("SuperBMD v" + assembly.GetName().Version);

            // Prevents floats being written to thedae with commas instead of periods on European systems.
            CultureInfo.CurrentCulture = new CultureInfo("", false);

            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                DisplayHelp();
                return;
            }

            Arguments.ParseArguments(args);

            List<Material> mat_presets = null;
            Model model;
            if (Arguments.ShouldProfile)
            {

                if (Arguments.InputPath.EndsWith(".bmd") || Arguments.InputPath.EndsWith(".bdl"))
                {
                    Console.WriteLine("Reading the model...");
                    model = Model.Load(mat_presets, "");

                    Console.WriteLine("Profiling ->");
                    model.ModelStats.PrintInfo();
                    model.ModelStats.PrintModelInfo(model);
                    Console.WriteLine("Press any key to Exit");
                    Console.ReadKey();
                    return;
                }
                else
                {
                    Console.WriteLine("Profiling is only supported for BMD/BDL!");
                }
            }

            if (Arguments.MaterialPath != "")
            {

                mat_presets = File.ReadAllText(Arguments.MaterialPath).JsonDeserialize<List<Material>>();


            }

            string additionalTexPath = null;
            if (Arguments.MaterialPath != "")
            {
                additionalTexPath = Path.GetDirectoryName(Arguments.MaterialPath);
            }
            FileInfo fi = new FileInfo(Arguments.InputPath);
            string destinationFormat = (fi.Extension == ".bmd" || fi.Extension == ".bdl") ? ".DAE" : (Arguments.ShouldExportAsBDL ? ".BDL" : ".BMD");

            if (destinationFormat == ".DAE" && Arguments.ShouldExportAsObj)
            {
                destinationFormat = ".OBJ";
            }

            Console.WriteLine(string.Format("Preparing to convert {0} from {1} to {2}", fi.Name.Replace(fi.Extension, ""), fi.Extension.ToUpper(), destinationFormat));
            model = Model.Load(mat_presets, additionalTexPath);

            if (!Arguments.HierarchyPath.IsEmpty())
            {
                model.Scenegraph.LoadHierarchyFromJson(Arguments.HierarchyPath);
            }

            if (Arguments.InputPath.EndsWith(".bmd") || Arguments.InputPath.EndsWith(".bdl"))
            {
                Console.WriteLine(string.Format("Converting {0} into {1}...", fi.Extension.ToUpper(), destinationFormat));
                if (Arguments.ShouldExportAsObj)
                    model.ExportAssImp(Arguments.OutputPath, "obj", new ExportSettings());
                else
                    model.ExportAssImp(Arguments.OutputPath, "dae", new ExportSettings());
            }
            else
            {
                Console.WriteLine("Finishing the Job...");
                model.ExportBMD(Arguments.OutputPath, Arguments.ShouldExportAsBDL);
                Console.WriteLine("✓");
            }
            Console.WriteLine("The Conversion is complete!");

        }

        /// <summary>
        /// Prints credits and argument descriptions to the console.
        /// </summary>
        private static void DisplayHelp()
        {
            var helpText = new StringBuilder();
            helpText.AppendLine("A tool to import and export various 3D model formats into the Binary Model (BMD or BDL) format.\n");
            helpText.AppendLine("Written by Sage_of_Mirrors/Gamma (@SageOfMirrors) and Yoshi2/RenolY2.");
            helpText.AppendLine("Console lines written by Super Hackio");
            helpText.AppendLine("Made possible with help from arookas, LordNed, xDaniel, and many others.");
            helpText.AppendLine("Visit https://github.com/Sage-of-Mirrors/SuperBMD/wiki for more information.");
            helpText.AppendLine("Usage: SuperBMD.exe (inputfilepath) [outputfilepath] [-m/mat filepath] [-x/--texheader filepath] [-t/--tristrip mode] [-r/--rotate] [-b/--bdl]\n");
            helpText.AppendLine("Parameters:");
            helpText.AppendLine("                   inputfilepath\tPath to the input file, either a BMD/BDL file or a DAE model.\t");
            helpText.AppendLine("                   outputfilepath\tPath to the output file.\t");
            helpText.AppendLine("--mat -m               filePath\tPath to the material configuration JSON for DAE to BMD conversion.\t");
            helpText.AppendLine("--outmat  -m           filePath\tOutput path for the material configuration JSON for BMD to DAE conversion.\t");
            helpText.AppendLine("--texheader -x         filePath\tPath to the texture headers JSON for DAE to BMD conversion.\t");
            helpText.AppendLine("--tristrip -t          mode\t\tMode for tristrip generation.\t");
            helpText.AppendLine("  static:    Only generate tristrips for static (unrigged) meshes.\t");
            helpText.AppendLine("  all:       Generate tristrips for all meshes.\t");
            helpText.AppendLine("  none:      Do not generate tristrips.\t");
            helpText.AppendLine("--rotate -r            Rotate the model from Z-up to Y-up orientation.\t");
            helpText.AppendLine("--bdl                  Generate a BDL instead of a BMD.\t");
            helpText.AppendLine("--nosort               Disable naturalistic sorting of meshes by name.\t");
            helpText.AppendLine("--onematpermesh        Ensure one material per mesh.\t");
            helpText.AppendLine("--obj                  If input is BMD/BDL, export the model as Wavefront OBJ instead of Collada (DAE).\t");
            helpText.AppendLine("--texfloat32           On conversion into BMD, always store texture UV coordinates as 32 bit floats.");
            helpText.AppendLine("--degeneratetri        On conversion into BMD, write triangle lists as triangle strips using degenerate triangles.\t");
            helpText.AppendLine("--profile -p           Generate a report with information on theBMD/.BDL (Other formats not supported)\t");
            helpText.AppendLine("--animation -a         Generate *.bck files from animation data stored in DAE, if present");
            Console.WriteLine(helpText);
        }

    }
}