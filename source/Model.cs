﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using System.IO;
using SuperBMD.BMD;
using SuperBMD.Animation;
using System.Text.RegularExpressions;

namespace SuperBMD
{
    public class Model
    {
        public INF1 Scenegraph { get; private set; }
        public VTX1 VertexData { get; private set; }
        public EVP1 SkinningEnvelopes { get; private set; }
        public DRW1 PartialWeightData { get; private set; }
        public JNT1 Joints { get; private set; }
        public SHP1 Shapes { get; private set; }
        public MAT3 Materials { get; private set; }
        public MDL3 MatDisplayList { get; private set; }
        public TEX1 Textures { get; private set; }
        public BMDInfo ModelStats { get; private set; }
        static private string[] characters_to_replace = new string[] { " ", "(", ")", ":", "-" };

        public List<BCK> BCKAnims { get; private set; }

        private int packetCount;
        private int vertexCount;

        public static Model Load(Arguments args, List<SuperBMD.Materials.Material> mat_presets, string additionalTexPath)
        {
            string extension = Path.GetExtension(args.InputPath);
            Model output = null;

            if (extension == ".bmd" || extension == ".bdl")
            {
                var a = new EndianBinaryReader(args.InputPath);
                output = new Model(ref a, args);
            }
            else
            {
                Assimp.AssimpContext context = new();

                // AssImp adds dummy nodes for pivots from FBX, so we'll force them off
                context.SetConfig(new Assimp.Configs.FBXPreservePivotsConfig(false));
                Assimp.PostProcessSteps postprocess = Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.JoinIdenticalVertices;

                if (args.TriStripMode == "none")
                {
                    // By not joining identical vertices, the Tri Strip algorithm we use cannot make tristrips, 
                    // effectively disabling tri stripping
                    postprocess = Assimp.PostProcessSteps.Triangulate;
                }
                Assimp.Scene aiScene = context.ImportFile(args.InputPath, postprocess);

                output = new Model(aiScene, args, mat_presets, additionalTexPath);
            }
            return output;
        }

        public Model(ref EndianBinaryReader reader, Arguments args)
        {
            ModelStats = new BMDInfo();
            BCKAnims = new List<BCK>();

            var j3d2Magic = reader.ReadString(4);
            var modelMagic = reader.ReadString(4);

            if (j3d2Magic != "J3D2")
                throw new Exception("Model was not a BMD or BDL! (J3D2 magic not found)");
            if ((modelMagic != "bmd3") && (modelMagic != "bdl4"))
                throw new Exception("Model was not a BMD or BDL! (Model type was not bmd3 or bdl4)");

            int modelSize = reader.ReadInt();
            int sectionCount = reader.ReadInt();
            ModelStats.TotalSize = modelSize;

            // Skip the dummy section, SVR3
            reader.Skip(16);

            Scenegraph = new INF1(ref reader, ModelStats);
            VertexData = new VTX1(ref reader, ModelStats);
            SkinningEnvelopes = new EVP1(ref reader, ModelStats);
            PartialWeightData = new DRW1(ref reader, ModelStats);
            Joints = new JNT1(ref reader, ModelStats);
            SkinningEnvelopes.SetInverseBindMatrices(Joints.FlatSkeleton);
            Shapes = new SHP1(ref reader, ModelStats);
            Shapes.SetVertexWeights(SkinningEnvelopes, PartialWeightData);
            Materials = new MAT3(ref reader, ModelStats);
            SkipMDL3(ref reader);
            Textures = new TEX1(ref reader, ModelStats);
            Materials.SetTextureNames(Textures);


            if (args.OutputMaterialPath != "")
            {
                Materials.DumpMaterials(Path.GetDirectoryName(args.OutputMaterialPath));
            }
            else
            {
                if (args.OutputPath != "")
                {
                    string outDir = Path.GetDirectoryName(args.OutputPath);
                    string filenameNoExt = Path.GetFileNameWithoutExtension(args.InputPath);
                    Materials.DumpMaterials(Path.Combine(outDir, "materials.json"));
                }
                else
                {
                    string inDir = Path.GetDirectoryName(args.InputPath);
                    string filenameNoExt = Path.GetFileNameWithoutExtension(args.InputPath);
                    Materials.DumpMaterials(Path.Combine(inDir, "materials.json"));
                }
            }
            foreach (Geometry.Shape shape in Shapes.Shapes)
                packetCount += shape.Packets.Count;

            vertexCount = VertexData.Attributes.Positions.Count;
        }

        private void SkipMDL3(ref EndianBinaryReader reader)
        {
            if (reader.PeekInt() == 0x4D444C33)
            {
                reader.Skip(4);
                int mdl3Size = reader.ReadInt();
                ModelStats.MDL3Size = mdl3Size;
                reader.Skip(mdl3Size);
            }
        }

        public Model(Scene scene, Arguments args, List<SuperBMD.Materials.Material> mat_presets = null, string additionalTexPath = null)
        {
            ModelStats = new BMDInfo();
            BCKAnims = new List<BCK>();

            if (args.EnsureOneMaterialPerMesh || args.MaterialOrderStrict)
            {
                EnsureOneMaterialPerMesh(scene);
            }

            if (args.SortMeshes)
            {
                SortMeshesByObjectNames(scene);
            }

            // For FBX mesh names are empty, instead we need to check the nodes and rename
            // the meshes after the node names.
            foreach (Assimp.Node node in scene.RootNode.Children)
            {
                foreach (int meshindex in node.MeshIndices)
                {
                    Assimp.Mesh mesh = scene.Meshes[meshindex];
                    if (mesh.Name == String.Empty)
                    {
                        mesh.Name = node.Name;
                    }
                }
            }

            Console.Write("Searching for the Skeleton Root");
            Assimp.Node root = null;
            for (int i = 0; i < scene.RootNode.ChildCount; i++)
            {
                if (scene.RootNode.Children[i].Name.ToLowerInvariant() == "skeleton_root")
                {
                    if (scene.RootNode.Children[i].ChildCount == 0)
                    {
                        throw new System.Exception("skeleton_root has no children! If you are making a rigged model, make sure skeleton_root contains the root of your skeleton.");
                    }
                    root = scene.RootNode.Children[i].Children[0];
                    break;
                }
                Console.Write(".");
            }

            Console.Write(root is null ? "✓ No Skeleton found" : "✓ Skeleton Found");


            foreach (Mesh mesh in scene.Meshes)
            {
                if (mesh.HasBones && root is null)
                {
                    throw new System.Exception("Model uses bones but the skeleton root has not been found! Make sure your skeleton is inside a dummy object called 'skeleton_root'.");
                }
            }


            if (args.RotateModel)
            {

                Console.Write("Rotating the model");
                int i = 0;
                Matrix4x4 rotate = Matrix4x4.FromRotationX((float)(-(1 / 2.0) * Math.PI));
                Matrix4x4 rotateinv = rotate;
                rotateinv.Inverse();


                foreach (Mesh mesh in scene.Meshes)
                {
                    if (root != null)
                    {
                        foreach (Assimp.Bone bone in mesh.Bones)
                        {
                            bone.OffsetMatrix = rotateinv * bone.OffsetMatrix;
                            Console.Write("|");
                        }
                    }

                    for (i = 0; i < mesh.VertexCount; i++)
                    {
                        Vector3D vertex = mesh.Vertices[i];
                        vertex.Set(vertex.X, vertex.Z, -vertex.Y);
                        mesh.Vertices[i] = vertex;
                    }
                    for (i = 0; i < mesh.Normals.Count; i++)
                    {
                        Vector3D norm = mesh.Normals[i];
                        norm.Set(norm.X, norm.Z, -norm.Y);

                        mesh.Normals[i] = norm;
                    }
                    Console.Write(".");
                }
                Console.Write("✓");

            }

            foreach (Mesh mesh in scene.Meshes)
            {
                if (mesh.HasNormals)
                {
                    for (int i = 0; i < mesh.Normals.Count; i++)
                    {
                        Vector3D normal = mesh.Normals[i];
                        normal.X = (float)Math.Round(normal.X, 4);
                        normal.Y = (float)Math.Round(normal.Y, 4);
                        normal.Z = (float)Math.Round(normal.Z, 4);
                        mesh.Normals[i] = normal;
                    }
                }
            }


            Console.WriteLine("Generating the Vertex Data ->");
            VertexData = new VTX1(scene, args.ForceFloat, args.VertexType, args.Fraction);

            Console.Write("Generating the Bone Data");
            Joints = new JNT1(scene, VertexData);

            Console.WriteLine("Generating the Texture Data -> ");
            Textures = new TEX1(scene, args);

            Console.Write("Generating the Envelope Data");
            SkinningEnvelopes = new EVP1();
            SkinningEnvelopes.SetInverseBindMatrices(scene, Joints.FlatSkeleton);


            Console.Write("Generating the Weight Data");
            PartialWeightData = new DRW1(scene, Joints.BoneNameIndices);
            Joints.UpdateBoundingBoxes(VertexData);


            Console.WriteLine("Generating the Mesh Data ->");
            Shapes = SHP1.Create(scene, Joints.BoneNameIndices, VertexData.Attributes, SkinningEnvelopes, PartialWeightData,
                args.TriStripMode, args.DegenerateTriangles);

            //Joints.UpdateBoundingBoxes(VertexData);



            Console.WriteLine("Generating the Material Data ->");
            Materials = new MAT3(scene, Textures, Shapes, args, mat_presets);


            Console.WriteLine("Loading the Textures ->");
            if (additionalTexPath is null)
            {
                Materials.LoadAdditionalTextures(Textures, Path.GetDirectoryName(args.InputPath), args.ReadMipmaps);
            }
            else
            {
                Materials.LoadAdditionalTextures(Textures, additionalTexPath, args.ReadMipmaps);
            }

            Materials.MapTextureNamesToIndices(Textures);

            if (args.ExportBDL)
            {

                Console.WriteLine("Compiling the MDL3 ->");
                MatDisplayList = new MDL3(Materials.Materials, Textures.Textures);
            }


            Console.Write("Generating the Joints");
            Scenegraph = new INF1(scene, Joints, args.MaterialOrderStrict);

            foreach (Geometry.Shape shape in Shapes.Shapes)
                packetCount += shape.Packets.Count;

            vertexCount = VertexData.Attributes.Positions.Count;

            if (args.ExportAnims && scene.AnimationCount > 0)
            {
                foreach (Assimp.Animation anm in scene.Animations)
                    BCKAnims.Add(new BCK(anm, Joints.FlatSkeleton));
            }
        }

        public void ExportBMD(string fileName, bool isBDL)
        {
            string outDir = Path.GetDirectoryName(fileName);

            EndianBinaryWriter writer = new(fileName);

            if (isBDL)
            {
                writer.Write("J3D2bdl4");
                writer.Write(0); // Placeholder for file size
                writer.Write(9); // Number of sections; bmd has 8, bdl has 9
            }
            else
            {
                writer.Write("J3D2bmd3");
                writer.Write(0); // Placeholder for file size
                writer.Write(8);
            }

            writer.PadAlign(32);
            Scenegraph.Write(ref writer, packetCount, vertexCount);
            VertexData.Write(ref writer);
            SkinningEnvelopes.Write(ref writer);
            PartialWeightData.Write(ref writer);
            Joints.Write(ref writer);
            Shapes.Write(ref writer);
            Materials.Write(ref writer);

            if (isBDL)
                MatDisplayList.Write(ref writer);

            Textures.Write(ref writer);

            writer.Seek(8);
            writer.Write((int)writer.FileLength);


            if (BCKAnims.Count > 0)
            {
                for (int i = 0; i < BCKAnims.Count; i++)
                {
                    string bckName = Path.Combine(outDir, $"anim_{i}.bck");

                    using (FileStream strm = new FileStream(bckName, FileMode.Create, FileAccess.Write))
                    {
                        EndianBinaryWriter bckWriter = new EndianBinaryWriter();
                        BCKAnims[i].Write(ref bckWriter);
                    }
                }

            }
            writer.PadAlign(32);
            writer.Close();
        }

        public void ExportAssImp(string fileName, string modelType, ExportSettings settings, Arguments cmdargs)
        {
            fileName = Path.GetFullPath(fileName); // Get absolute path instead of relative
            string outDir = Path.GetDirectoryName(fileName);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            if (modelType == "obj")
            {
                fileName = Path.Combine(outDir, fileNameNoExt + ".obj");
            }
            else
            {
                fileName = Path.Combine(outDir, fileNameNoExt + ".dae");
            }
            Scene outScene = new Scene { RootNode = new Node("RootNode") };


            Console.WriteLine("Processing Materials ->");
            Materials.FillScene(outScene, Textures, outDir);

            Console.WriteLine("Processing Meshes ->");
            Shapes.FillScene(outScene, VertexData.Attributes, Joints.FlatSkeleton, SkinningEnvelopes.InverseBindMatrices);
            Console.Write("Processing Skeleton");
            Scenegraph.FillScene(outScene, Joints.FlatSkeleton, settings.UseSkeletonRoot);
            Scenegraph.CorrectMaterialIndices(outScene, Materials);

            Console.WriteLine("Processing Textures ->");
            Textures.DumpTextures(outDir, "tex_headers.json", true, cmdargs.ReadMipmaps);
            this.Scenegraph.DumpJson(Path.Combine(outDir, "hierarchy.json"));
            this.Joints.DumpJson(Path.Combine(outDir, "joints.json"));
            this.PartialWeightData.DumpJson(Path.Combine(outDir, "partialweights.json"));
            this.Shapes.DumpJson(Path.Combine(outDir, "shapes.json"));

            Console.WriteLine("Removing Duplicate Verticies ->");
            foreach (Mesh mesh in outScene.Meshes)
            {
                Console.Write(mesh.Name.Replace('_', ' ') + ": ");
                // Assimp has a JoinIdenticalVertices post process step, but we can't use that or the skinning info we manually add won't take it into account.
                RemoveDuplicateVertices(mesh);
                Console.Write("✓");

            }


            AssimpContext cont = new AssimpContext();

            if (modelType == "obj")
            {
                Console.WriteLine("Writing the OBJ file...");
                cont.ExportFile(outScene, fileName, "obj");//, PostProcessSteps.ValidateDataStructure);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName))
                {
                    string mtllibname = fileName.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Last() + ".mtl";
                    file.WriteLine(String.Format("mtllib {0}", mtllibname));
                    foreach (Assimp.Mesh mesh in outScene.Meshes)
                    {
                        foreach (Assimp.Vector3D vertex in mesh.Vertices)
                        {
                            file.WriteLine(String.Format("v {0} {1} {2}", vertex.X, vertex.Y, vertex.Z));
                        }
                    }

                    foreach (Assimp.Mesh mesh in outScene.Meshes)
                    {
                        foreach (Assimp.Vector3D normal in mesh.Normals)
                        {
                            file.WriteLine(String.Format("vn {0} {1} {2}", normal.X, normal.Y, normal.Z));
                        }
                    }

                    foreach (Assimp.Mesh mesh in outScene.Meshes)
                    {
                        if (mesh.HasTextureCoords(0))
                        {
                            foreach (Assimp.Vector3D uv in mesh.TextureCoordinateChannels[0])
                            {
                                file.WriteLine(String.Format("vt {0} {1}", uv.X, uv.Y));
                            }
                        }
                    }

                    int vertex_offset = 1;

                    foreach (Assimp.Mesh mesh in outScene.Meshes)
                    {
                        string material_name = outScene.Materials[mesh.MaterialIndex].Name;
                        file.WriteLine(String.Format("usemtl {0}", material_name));



                        foreach (Assimp.Face face in mesh.Faces)
                        {
                            file.Write("f ");
                            foreach (int index in face.Indices)
                            {
                                file.Write(index + vertex_offset);
                                if (mesh.HasTextureCoords(0))
                                {
                                    file.Write("/");
                                    file.Write(index + vertex_offset);
                                }
                                if (!mesh.HasTextureCoords(0) && mesh.HasNormals)
                                {
                                    file.Write("//");
                                    file.Write(index + vertex_offset);
                                }
                                else if (mesh.HasNormals)
                                {
                                    file.Write("/");
                                    file.Write(index + vertex_offset);
                                }
                                file.Write(" ");
                            }
                            file.Write("\n");
                        }
                        vertex_offset += mesh.VertexCount;
                    }
                }
                return;
            }
            else
            {
                cont.ExportFile(outScene, fileName, "collada", PostProcessSteps.ValidateDataStructure);
            }

            //if (SkinningEnvelopes.Weights.Count == 0)
            //    return; // There's no skinning information, so we can stop here

            // Now we need to add some skinning info, since AssImp doesn't do it for some bizarre reason

            StreamWriter test = new StreamWriter(fileName + ".tmp");
            StreamReader dae = File.OpenText(fileName);


            Console.Write("Finalizing the Mesh");
            while (!dae.EndOfStream)
            {
                string line = dae.ReadLine();

                if (line == "  <library_visual_scenes>")
                {
                    AddControllerLibrary(outScene, test);
                    test.WriteLine(line);
                    test.Flush();
                }
                else if (line.Contains("<node"))
                {
                    string[] testLn = line.Split('\"');
                    string name = testLn[3];

                    if (Joints.FlatSkeleton.Exists(x => x.Name == name))
                    {
                        string jointLine = line.Replace(">", $" sid=\"{name}\" type=\"JOINT\">");
                        test.WriteLine(jointLine);
                        test.Flush();
                    }
                    else
                    {
                        test.WriteLine(line);
                        test.Flush();
                    }
                }
                else if (line.Contains("</visual_scene>"))
                {
                    foreach (Mesh mesh in outScene.Meshes)
                    {
                        string matname = "mat";
                        bool keepmatnames = false;
                        if (keepmatnames == true)
                        {
                            matname = AssimpMatnameSanitize(mesh.MaterialIndex, outScene.Materials[mesh.MaterialIndex].Name);
                        }
                        else
                        {
                            matname = AssimpMatnameSanitize(mesh.MaterialIndex, Materials.Materials[mesh.MaterialIndex].Name);
                        }
                        test.WriteLine($"      <node id=\"{mesh.Name}\" name=\"{mesh.Name}\" type=\"NODE\">");
                        test.WriteLine($"       <instance_controller url=\"#{mesh.Name}-skin\">");
                        test.WriteLine("        <skeleton>#skeleton_root</skeleton>");
                        test.WriteLine("        <bind_material>");
                        test.WriteLine("         <technique_common>");
                        test.WriteLine($"          <instance_material symbol=\"m{matname}\" target=\"#{matname}\" />");
                        test.WriteLine("         </technique_common>");
                        test.WriteLine("        </bind_material>");
                        test.WriteLine("       </instance_controller>");
                        test.WriteLine("      </node>");
                        test.Flush();
                    }
                    test.WriteLine(line);
                    test.Flush();
                }
                else if (line.Contains("<matrix"))
                {
                    string matLine = line.Replace("<matrix>", "<matrix sid=\"matrix\">");
                    test.WriteLine(matLine);
                    test.Flush();
                }
                else
                {
                    test.WriteLine(line);
                    test.Flush();
                }
                Console.Write(".");
            }
            Console.Write("✓");
            test.Close();
            dae.Close();
            File.Copy(fileName + ".tmp", fileName, true);
            File.Delete(fileName + ".tmp");
        }

        private void AddControllerLibrary(Scene scene, StreamWriter writer)
        {
            writer.WriteLine("  <library_controllers>");
            for (int i = 0; i < scene.MeshCount; i++)
            {
                Mesh curMesh = scene.Meshes[i];
                curMesh.Name = curMesh.Name.Replace('_', '-');
                writer.WriteLine($"   <controller id=\"{curMesh.Name}-skin\" name=\"{curMesh.Name}Skin\">");
                writer.WriteLine($"    <skin source=\"#meshId{i}\">");
                WriteBindShapeMatrixToStream(writer);
                WriteJointNameArrayToStream(curMesh, writer);
                WriteInverseBindMatricesToStream(curMesh, writer);
                WriteSkinWeightsToStream(curMesh, writer);
                writer.WriteLine("     <joints>");
                writer.WriteLine($"      <input semantic=\"JOINT\" source=\"#{curMesh.Name}-skin-joints-array\"></input>");
                writer.WriteLine($"      <input semantic=\"INV_BIND_MATRIX\" source=\"#{curMesh.Name}-skin-bind_poses-array\"></input>");
                writer.WriteLine("     </joints>");
                writer.Flush();
                WriteVertexWeightsToStream(curMesh, writer);
                writer.WriteLine("    </skin>");
                writer.WriteLine("   </controller>");
                writer.Flush();
            }
            writer.WriteLine("  </library_controllers>");
            writer.Flush();
        }

        private void WriteBindShapeMatrixToStream(StreamWriter writer)
        {
            writer.WriteLine("     <bind_shape_matrix>");
            writer.WriteLine("      1 0 0 0");
            writer.WriteLine("      0 1 0 0");
            writer.WriteLine("      0 0 1 0");
            writer.WriteLine("      0 0 0 1");
            writer.WriteLine("     </bind_shape_matrix>");
            writer.Flush();
        }

        private void WriteJointNameArrayToStream(Mesh mesh, StreamWriter writer)
        {
            writer.WriteLine($"      <source id =\"{mesh.Name}-skin-joints-array\">");
            writer.WriteLine($"      <Name_array id=\"{mesh.Name}-skin-joints-array\" count=\"{mesh.Bones.Count}\">");

            writer.Write("       ");
            foreach (Bone bone in mesh.Bones)
            {
                writer.Write($"{bone.Name}");
                if (bone != mesh.Bones.Last())
                    writer.Write(' ');
                else
                    writer.Write('\n');

                writer.Flush();
            }
            writer.WriteLine("      </Name_array>");
            writer.Flush();
            writer.WriteLine("      <technique_common>");
            writer.WriteLine($"       <accessor source=\"#{mesh.Name}-skin-joints-array\" count=\"{mesh.Bones.Count}\" stride=\"1\">");
            writer.WriteLine("         <param name=\"JOINT\" type=\"Name\"></param>");
            writer.WriteLine("       </accessor>");
            writer.WriteLine("      </technique_common>");
            writer.WriteLine("      </source>");
            writer.Flush();
        }

        private void WriteInverseBindMatricesToStream(Mesh mesh, StreamWriter writer)
        {
            writer.WriteLine($"      <source id =\"{mesh.Name}-skin-bind_poses-array\">");
            writer.WriteLine($"      <float_array id=\"{mesh.Name}-skin-bind_poses-array\" count=\"{mesh.Bones.Count * 16}\">");
            foreach (Bone bone in mesh.Bones)
            {
                Matrix4x4 ibm = bone.OffsetMatrix;
                ibm.Transpose();
                string fmt = "G7";
                writer.WriteLine($"       {ibm.A1.ToString(fmt)} {ibm.A2.ToString(fmt)} {ibm.A3.ToString(fmt)} {ibm.A4.ToString(fmt)}");
                writer.WriteLine($"       {ibm.B1.ToString(fmt)} {ibm.B2.ToString(fmt)} {ibm.B3.ToString(fmt)} {ibm.B4.ToString(fmt)}");
                writer.WriteLine($"       {ibm.C1.ToString(fmt)} {ibm.C2.ToString(fmt)} {ibm.C3.ToString(fmt)} {ibm.C4.ToString(fmt)}");
                writer.WriteLine($"       {ibm.D1.ToString(fmt)} {ibm.D2.ToString(fmt)} {ibm.D3.ToString(fmt)} {ibm.D4.ToString(fmt)}");

                if (bone != mesh.Bones.Last())
                    writer.WriteLine("");
            }
            writer.WriteLine("      </float_array>");
            writer.Flush();
            writer.WriteLine("      <technique_common>");
            writer.WriteLine($"       <accessor source=\"#{mesh.Name}-skin-bind_poses-array\" count=\"{mesh.Bones.Count}\" stride=\"16\">");
            writer.WriteLine("         <param name=\"TRANSFORM\" type=\"float4x4\"></param>");
            writer.WriteLine("       </accessor>");
            writer.WriteLine("      </technique_common>");
            writer.WriteLine("      </source>");
            writer.Flush();
        }

        private void WriteSkinWeightsToStream(Mesh mesh, StreamWriter writer)
        {
            int totalWeightCount = 0;
            foreach (Bone bone in mesh.Bones)
            {
                totalWeightCount += bone.VertexWeightCount;
            }
            writer.WriteLine($"      <source id =\"{mesh.Name}-skin-weights-array\">");
            writer.WriteLine($"      <float_array id=\"{mesh.Name}-skin-weights-array\" count=\"{totalWeightCount}\">");
            writer.Write("       ");
            foreach (Bone bone in mesh.Bones)
            {
                foreach (VertexWeight weight in bone.VertexWeights)
                {
                    writer.Write($"{weight.Weight} ");
                }

                if (bone == mesh.Bones.Last())
                    writer.WriteLine();
            }
            writer.WriteLine("      </float_array>");
            writer.Flush();
            writer.WriteLine("      <technique_common>");
            writer.WriteLine($"       <accessor source=\"#{mesh.Name}-skin-weights-array\" count=\"{totalWeightCount}\" stride=\"1\">");
            writer.WriteLine("         <param name=\"WEIGHT\" type=\"float\"></param>");
            writer.WriteLine("       </accessor>");
            writer.WriteLine("      </technique_common>");
            writer.WriteLine("      </source>");
            writer.Flush();
        }

        private void WriteVertexWeightsToStream(Mesh mesh, StreamWriter writer)
        {
            List<float> weights = new List<float>();
            Dictionary<int, Rigging.Weight> vertIDWeights = new Dictionary<int, Rigging.Weight>();

            foreach (Bone bone in mesh.Bones)
            {
                foreach (VertexWeight weight in bone.VertexWeights)
                {
                    weights.Add(weight.Weight);

                    if (!vertIDWeights.ContainsKey(weight.VertexID))
                        vertIDWeights.Add(weight.VertexID, new Rigging.Weight());

                    vertIDWeights[weight.VertexID].AddWeight(weight.Weight, mesh.Bones.IndexOf(bone));
                }
            }
            writer.WriteLine($"      <vertex_weights count=\"{vertIDWeights.Count}\">");
            writer.WriteLine($"       <input semantic=\"JOINT\" source=\"#{mesh.Name}-skin-joints-array\" offset=\"0\"></input>");
            writer.WriteLine($"       <input semantic=\"WEIGHT\" source=\"#{mesh.Name}-skin-weights-array\" offset=\"1\"></input>");
            writer.WriteLine("       <vcount>");
            writer.Write("        ");
            for (int i = 0; i < vertIDWeights.Count; i++)
                writer.Write($"{vertIDWeights[i].WeightCount} ");

            writer.WriteLine("\n       </vcount>");
            writer.WriteLine("       <v>");
            writer.Write("        ");

            for (int i = 0; i < vertIDWeights.Count; i++)
            {
                Rigging.Weight curWeight = vertIDWeights[i];

                for (int j = 0; j < curWeight.WeightCount; j++)
                {
                    writer.Write($"{curWeight.BoneIndices[j]} {weights.IndexOf(curWeight.Weights[j])} ");
                }
            }
            writer.WriteLine("\n       </v>");
            writer.WriteLine($"      </vertex_weights>");
        }

        // Attempt to replicate Assimp's behaviour for sanitizing material names
        private string AssimpMatnameSanitize(int meshindex, string matname)
        {
            matname = matname.Replace("#", "_");
            foreach (string letter in characters_to_replace)
            {
                matname = matname.Replace(letter, "_");
            }
            return $"m{meshindex}{matname}";
        }

        static public string AssimpMatnamePartSanitize(string matname)
        {
            matname = matname.Replace("#", "_");
            foreach (string letter in characters_to_replace)
            {
                matname = matname.Replace(letter, "_");
            }
            return matname;
        }

        private void RemoveDuplicateVertices(Mesh mesh)
        {
            // Calculate which vertices are duplicates (based on their position, texture coordinates, and normals).
            List<
                Tuple<Vector3D, Vector3D?, List<Vector3D>, List<Color4D>>
                > uniqueVertInfos = new List<
                                            Tuple<Vector3D, Vector3D?, List<Vector3D>, List<Color4D>>
                                            >();

            int[] replaceVertexIDs = new int[mesh.Vertices.Count];
            bool[] vertexIsUnique = new bool[mesh.Vertices.Count];
            for (var origVertexID = 0; origVertexID < mesh.Vertices.Count; origVertexID++)
            {

                var colorsForVert = new List<Color4D>();
                for (var i = 0; i < mesh.VertexColorChannelCount; i++)
                {
                    colorsForVert.Add(mesh.VertexColorChannels[i][origVertexID]);
                }

                var coordsForVert = new List<Vector3D>();
                for (var i = 0; i < mesh.TextureCoordinateChannelCount; i++)
                {
                    coordsForVert.Add(mesh.TextureCoordinateChannels[i][origVertexID]);
                }

                Vector3D? normal;
                if (origVertexID < mesh.Normals.Count)
                {
                    normal = mesh.Normals[origVertexID];
                }
                else
                {
                    normal = null;
                }

                var vertInfo = new Tuple<
                    Vector3D, Vector3D?, List<Vector3D>, List<Color4D>
                    >(mesh.Vertices[origVertexID], normal, coordsForVert, colorsForVert);

                // Determine if this vertex is a duplicate of a previously encountered vertex or not and if it is keep track of the new index
                var duplicateVertexIndex = -1;
                for (var i = 0; i < uniqueVertInfos.Count; i++)
                {
                    Tuple<Vector3D, Vector3D?, List<Vector3D>, List<Color4D>> otherVertInfo = uniqueVertInfos[i];
                    if (CheckVertInfosAreDuplicates(
                        vertInfo.Item1, vertInfo.Item2, vertInfo.Item3, vertInfo.Item4,
                        otherVertInfo.Item1, otherVertInfo.Item2, otherVertInfo.Item3, otherVertInfo.Item4))
                    {
                        duplicateVertexIndex = i;
                        break;
                    }
                }

                if (duplicateVertexIndex == -1)
                {
                    vertexIsUnique[origVertexID] = true;
                    uniqueVertInfos.Add(vertInfo);
                    replaceVertexIDs[origVertexID] = uniqueVertInfos.Count - 1;
                }
                else
                {
                    vertexIsUnique[origVertexID] = false;
                    replaceVertexIDs[origVertexID] = duplicateVertexIndex;
                }
            }

            // Remove duplicate vertices, normals, and texture coordinates.
            mesh.Vertices.Clear();
            mesh.Normals.Clear();
            // Need to preserve the channel count since it gets set to 0 when clearing all the channels
            int origTexCoordChannelCount = mesh.TextureCoordinateChannelCount;
            for (var i = 0; i < origTexCoordChannelCount; i++)
            {
                mesh.TextureCoordinateChannels[i].Clear();
            }

            int origColorChannelCount = mesh.VertexColorChannelCount;
            for (var i = 0; i < origColorChannelCount; i++)
            {
                mesh.VertexColorChannels[i].Clear();
            }

            foreach (Tuple<Vector3D, Vector3D?, List<Vector3D>, List<Color4D>> vertInfo in uniqueVertInfos)
            {
                mesh.Vertices.Add(vertInfo.Item1);
                if (vertInfo.Item2 != null)
                {
                    mesh.Normals.Add(vertInfo.Item2.Value);
                }
                for (var i = 0; i < origTexCoordChannelCount; i++)
                {
                    var coord = vertInfo.Item3[i];
                    mesh.TextureCoordinateChannels[i].Add(coord);
                }
                for (var i = 0; i < origColorChannelCount; i++)
                {
                    var color = vertInfo.Item4[i];
                    mesh.VertexColorChannels[i].Add(color);
                }
            }

            // Update vertex indices for the faces.
            foreach (Face face in mesh.Faces)
            {
                for (var i = 0; i < face.IndexCount; i++)
                {
                    face.Indices[i] = replaceVertexIDs[face.Indices[i]];
                }
            }

            // Update vertex indices for the bone vertex weights.
            foreach (Bone bone in mesh.Bones)
            {
                List<VertexWeight> origVertexWeights = new List<VertexWeight>(bone.VertexWeights);
                bone.VertexWeights.Clear();
                for (var i = 0; i < origVertexWeights.Count; i++)
                {
                    VertexWeight origWeight = origVertexWeights[i];
                    int origVertexID = origWeight.VertexID;
                    if (!vertexIsUnique[origVertexID])
                        continue;

                    int newVertexID = replaceVertexIDs[origVertexID];
                    VertexWeight newWeight = new VertexWeight(newVertexID, origWeight.Weight);
                    bone.VertexWeights.Add(newWeight);
                }
            }
        }

        private bool CheckVertInfosAreDuplicates(Vector3D vert1, Vector3D? norm1, List<Vector3D> vert1TexCoords, List<Color4D> vert1Colors,
                                                Vector3D vert2, Vector3D? norm2, List<Vector3D> vert2TexCoords, List<Color4D> vert2Colors)
        {
            if (vert1 != vert2)
            {
                // Position is different
                return false;
            }

            if (norm1 != norm2)
            {
                // Normals are different
                return false;
            }

            for (var i = 0; i < vert1TexCoords.Count; i++)
            {
                if (vert1TexCoords[i] != vert2TexCoords[i])
                {
                    // Texture coordinate is different
                    return false;
                }
            }

            for (var i = 0; i < vert1Colors.Count; i++)
            {
                if (vert1Colors[i] != vert2Colors[i])
                {
                    // Color is different
                    return false;
                }
            }

            return true;
        }
        private void SortMeshesByObjectNames(Scene scene)
        {
            // Sort meshes by their name instead of keeping the order they're in inside the file.
            // Specifically, natural sorting is used so that mesh-9 comes before mesh-10.
            Console.Write("Sorting Meshes...");
            List<string> meshNames = new List<string>();
            int maxNumberLength = 0;
            foreach (Node node in scene.RootNode.Children)
            {
                if (node.HasMeshes)
                {
                    int currMaxNumberLength = node.Name.SelectMany(i => Regex.Matches(node.Name, @"\d+").Cast<Match>().Select(m => m.Value.Length)).DefaultIfEmpty(0).Max();
                    if (currMaxNumberLength > maxNumberLength)
                    {
                        maxNumberLength = currMaxNumberLength;
                    }
                    for (int i = 0; i < node.MeshCount; i++)
                    {
                        meshNames.Add(node.Name);
                    }
                }
                Console.Write(".");
            }

            if (meshNames.Count != scene.Meshes.Count)
            {
                throw new Exception($"Number of meshes ({scene.Meshes.Count}) is not the same as the number of mesh objects ({meshNames.Count}); cannot sort.\nMesh objects: {String.Join(", ", meshNames)}\nMeshes: {String.Join(", ", scene.Meshes.Select(mesh => mesh.Name))}");
            }

            // Pad the numbers in mesh names with 0s.
            List<string> meshNamesPadded = new List<string>();
            foreach (string meshName in meshNames)
            {
                meshNamesPadded.Add(Regex.Replace(meshName, @"\d+", m => m.Value.PadLeft(maxNumberLength, '0')));
            }

            // Use Array.Sort to sort the meshes by the order of their object names.
            var meshNamesArray = meshNamesPadded.ToArray();
            var meshesArray = scene.Meshes.ToArray();
            Array.Sort(meshNamesArray, meshesArray);

            for (int i = 0; i < scene.Meshes.Count; i++)
            {
                scene.Meshes[i] = meshesArray[i];
            }
            Console.Write("✓");
        }
        private void EnsureOneMaterialPerMesh(Scene scene)
        {
            foreach (Mesh mesh1 in scene.Meshes)
            {
                foreach (Mesh mesh2 in scene.Meshes)
                {
                    if (mesh1.Name == mesh2.Name && mesh1.MaterialIndex != mesh2.MaterialIndex)
                    {
                        throw new Exception($"Mesh \"{mesh1.Name}\" has more than one material assigned to it! " +
                            $"Break the mesh up per material or turn off the ``--onematpermesh`` option.");
                    }
                }
            }
        }
    }



    public class BMDInfo
    {
        public int TotalSize;
        public int INF1Size;
        public int VTX1Size;
        public int EVP1Size;
        public int DRW1Size;
        public int JNT1Size;
        public int SHP1Size;
        public int MAT3Size;
        public int MDL3Size;
        public int TEX1Size;

        public void PrintInfo()
        {
            Console.WriteLine("Total size: {0} bytes ({1} KiB)", TotalSize, (float)TotalSize / (float)1024);
            PrintSize("INF1", "SceneGraph", INF1Size);
            PrintSize("VTX1", "Vertex Attributes", VTX1Size);
            PrintSize("EVP1", "Envelopes", EVP1Size);
            PrintSize("DRW1", "Partial Weights", DRW1Size);
            PrintSize("JNT1", "Joints", JNT1Size);
            PrintSize("SHP1", "Shape Data", SHP1Size);
            PrintSize("MAT3", "Materials", MAT3Size);
            PrintSize("MDL3", "Display Lists", MDL3Size);
            PrintSize("TEX1", "Textures", TEX1Size);
        }
        private void PrintSize(string sectionName, string longDescription, int size)
        {
            Console.WriteLine("Section {0} ({1}) size: {2} bytes ({3} KiB, {4:0.00}% of total)",
                            sectionName, longDescription, size, (float)size / (float)1024, ((float)size / (float)TotalSize) * 100);
        }
        public void PrintModelInfo(Model mod)
        {
            PrintVertexAttributeInfo(mod.VertexData);
            Console.WriteLine("INF: {0} scene nodes", mod.Scenegraph.FlatNodes.Count);
            Console.WriteLine("EVP1: {0} weights", mod.SkinningEnvelopes.Weights.Count);
            Console.WriteLine("EVP1: {0} inverse bind matrices", mod.SkinningEnvelopes.InverseBindMatrices.Count);
            Console.WriteLine("DRW1: {0} WeightTypeCheck flags, {1} indices", mod.PartialWeightData.WeightTypeCheck.Count, mod.PartialWeightData.Indices.Count);
            Console.WriteLine("JNT1: {0} joints", mod.Joints.FlatSkeleton.Count);
            Console.WriteLine("SHP1: {0} meshes", mod.Shapes.Shapes.Count);
            Console.WriteLine("MAT3: {0} materials", mod.Materials.Materials.Count);
            Console.WriteLine("TEX1: {0} textures", mod.Textures.Textures.Count);
            PrintTextureInfo(mod.Textures);
        }

        private void PrintTextureInfo(TEX1 textures)
        {
            int i = 0;
            Console.WriteLine("Textures in model:");
            foreach (Materials.BinaryTextureImage tex in textures.Textures)
            {
                Console.WriteLine("{0}) {1} Format: {2}, {3}x{4}, {5} mipmaps", i, tex.Name, tex.Format,
                    tex.Width, tex.Height, tex.ImageCount);
                i++;

            }
        }

        private void PrintVertexAttributeInfo(VTX1 vertexData)
        {
            Console.WriteLine("{0} Vertex Positions", vertexData.Attributes.Positions.Count);
            PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Position);
            Console.WriteLine("{0} Vertex Normals", vertexData.Attributes.Normals.Count);
            PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Normal);
            if (vertexData.Attributes.Color_0.Count > 0)
            {
                Console.WriteLine("{0} Vertex Colors (Channel 0)", vertexData.Attributes.Color_0.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Color0);
            }
            if (vertexData.Attributes.Color_1.Count > 0)
            {
                Console.WriteLine("{0} Vertex Colors (Channel 1)", vertexData.Attributes.Color_1.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Color1);
            }

            Console.WriteLine("{0} Vertex Texture Coords (Channel 0)", vertexData.Attributes.TexCoord_0.Count);
            PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex0);

            if (vertexData.Attributes.TexCoord_1.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 1)", vertexData.Attributes.TexCoord_1.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex1);
            }
            if (vertexData.Attributes.TexCoord_2.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 2)", vertexData.Attributes.TexCoord_2.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex2);
            }
            if (vertexData.Attributes.TexCoord_3.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 3)", vertexData.Attributes.TexCoord_3.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex3);
            }
            if (vertexData.Attributes.TexCoord_4.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 4)", vertexData.Attributes.TexCoord_4.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex4);
            }
            if (vertexData.Attributes.TexCoord_5.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 5)", vertexData.Attributes.TexCoord_5.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex5);
            }
            if (vertexData.Attributes.TexCoord_6.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 6)", vertexData.Attributes.TexCoord_6.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex6);
            }
            if (vertexData.Attributes.TexCoord_7.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 7)", vertexData.Attributes.TexCoord_7.Count);
                PrintAttributeFormat(vertexData, Geometry.GXVertexAttribute.Tex7);
            }
        }
        private void PrintAttributeFormat(VTX1 vertexData, Geometry.GXVertexAttribute attr)
        {
            if (vertexData.StorageFormats.ContainsKey(attr))
            {
                Tuple<Geometry.GXDataType, byte> tuple;
                if (vertexData.StorageFormats.TryGetValue(attr, out tuple))
                {
                    Console.WriteLine("Attribute {0} has format {1} with fractional part of {2} bits",
                        attr, tuple.Item1, tuple.Item2);
                };
            }

        }


    }
}