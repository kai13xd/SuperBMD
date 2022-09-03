global using OpenTK.Mathematics;
global using Newtonsoft.Json;
global using Newtonsoft.Json.Converters;
global using System;
global using System.Linq;
global using System.Text;
global using System.Collections.Generic;
global using System.Globalization;
global using System.Reflection;
global using System.IO;
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

            Arguments cmdArgs = new Arguments(args);

            List<Material> mat_presets = null;
            Model model;
            if (cmdArgs.DoProfile)
            {
                if (cmdArgs.InputPath.EndsWith(".bmd") || cmdArgs.InputPath.EndsWith(".bdl"))
                {
                    Console.WriteLine("Reading the model...");
                    model = Model.Load(cmdArgs, mat_presets, "");

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

            if (cmdArgs.MaterialsPath != "")
            {
                JsonSerializer serializer = new JsonSerializer();

                serializer.Converters.Add((new Newtonsoft.Json.Converters.StringEnumConverter()));
                Console.WriteLine("Reading the Materials...");
                using (TextReader file = File.OpenText(cmdArgs.MaterialsPath))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        try
                        {
                            mat_presets = serializer.Deserialize<List<Material>>(reader);
                        }
                        catch (Newtonsoft.Json.JsonReaderException e)
                        {
                            Console.WriteLine(String.Format("Error encountered while reading {0}", cmdArgs.MaterialsPath));
                            Console.WriteLine(String.Format("JsonReaderException: {0}", e.Message));
                            return;
                        }
                        catch (Newtonsoft.Json.JsonSerializationException e)
                        {
                            Console.WriteLine(String.Format("Error encountered while reading {0}", cmdArgs.MaterialsPath));
                            Console.WriteLine(String.Format("JsonSerializationException: {0}", e.Message));
                            return;
                        }
                    }
                }
            }

            string additionalTexPath = null;
            if (cmdArgs.MaterialsPath != "")
            {
                additionalTexPath = Path.GetDirectoryName(cmdArgs.MaterialsPath);
            }
            FileInfo fi = new FileInfo(cmdArgs.InputPath);
            string destinationFormat = (fi.Extension == ".bmd" || fi.Extension == ".bdl") ? ".DAE" : (cmdArgs.ExportBDL ? ".BDL" : ".BMD");

            if (destinationFormat == ".DAE" && cmdArgs.ExportObj)
            {
                destinationFormat = ".OBJ";
            }

            Console.WriteLine(string.Format("Preparing to convert {0} from {1} to {2}", fi.Name.Replace(fi.Extension, ""), fi.Extension.ToUpper(), destinationFormat));
            model = Model.Load(cmdArgs, mat_presets, additionalTexPath);

            if (cmdArgs.HierarchyPath != "")
            {
                model.Scenegraph.LoadHierarchyFromJson(cmdArgs.HierarchyPath);
            }

            if (cmdArgs.InputPath.EndsWith(".bmd") || cmdArgs.InputPath.EndsWith(".bdl"))
            {
                Console.WriteLine(string.Format("Converting {0} into {1}...", fi.Extension.ToUpper(), destinationFormat));
                if (cmdArgs.ExportObj)
                    model.ExportAssImp(cmdArgs.OutputPath, "obj", new ExportSettings(), cmdArgs);
                else
                    model.ExportAssImp(cmdArgs.OutputPath, "dae", new ExportSettings(), cmdArgs);
            }
            else
            {
                Console.Write("Finishing the Job...");
                model.ExportBMD(cmdArgs.OutputPath, cmdArgs.ExportBDL);
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
