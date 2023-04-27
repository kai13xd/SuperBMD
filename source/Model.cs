using System.Linq;

namespace SuperBMD
{
    public partial class Model
    {
        public INF1 INF1Section { get; private set; }
        public VTX1 VTX1Section { get; private set; }
        public EVP1 EVP1Section { get; private set; }
        public DRW1 DRW1Section { get; private set; }
        public JNT1 JNT1Section { get; private set; }
        public SHP1 SHP1Section { get; private set; }
        public MAT3 MAT3Section { get; private set; }
        public MDL3 MatDisplayList { get; private set; }
        public TEX1 TEX1Section { get; private set; }
        public BMDInfo ModelStats { get; private set; }
        static private string[] characters_to_replace = new string[] { " ", "(", ")", ":", "-" };

        public List<BCK> BCKAnims { get; private set; }

        private int packetCount;
        private int vertexCount;

        public static Model Load(List<BMDMaterial> materialPresets, string additionalTexPath)
        {
            string extension = Path.GetExtension(Arguments.InputPath);
            Model output;
            if (extension == ".bmd" || extension == ".bdl")
            {
                var a = new EndianBinaryReader(Arguments.InputPath);
                output = new Model(ref a);
            }
            else
            {
                AssimpContext context = new();

                // AssImp adds dummy nodes for pivots from FBX, so we'll force them off
                context.SetConfig(new Assimp.Configs.FBXPreservePivotsConfig(false));
                PostProcessSteps postprocess = PostProcessSteps.Triangulate | PostProcessSteps.JoinIdenticalVertices;

                if (Arguments.TriStripMode == "none")
                {
                    // By not joining identical vertices, the Tri Strip algorithm we use cannot make tristrips, 
                    // effectively disabling tri stripping
                    postprocess = PostProcessSteps.Triangulate;
                }
                Scene aiScene = context.ImportFile(Arguments.InputPath, postprocess);

                output = new Model(aiScene, materialPresets, additionalTexPath);
            }
            return output;
        }

        public Model(ref EndianBinaryReader reader)
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

            INF1Section = new INF1(ref reader, ModelStats);
            VTX1Section = new VTX1(ref reader, ModelStats);
            EVP1Section = new EVP1(ref reader, ModelStats);
            DRW1Section = new DRW1(ref reader, ModelStats);
            JNT1Section = new JNT1(ref reader, ModelStats);
            EVP1Section.SetInverseBindMatrices(JNT1Section.FlatSkeleton);
            SHP1Section = new SHP1(ref reader, ModelStats);
            SHP1Section.SetVertexWeights(EVP1Section, DRW1Section);
            MAT3Section = new MAT3(ref reader, ModelStats);
            SkipMDL3(ref reader);
            TEX1Section = new TEX1(ref reader, ModelStats);
            MAT3Section.SetTextureNames(TEX1Section);


            if (Arguments.OutputMaterialPath != "")
            {
                MAT3Section.DumpMaterials(Path.GetDirectoryName(Arguments.OutputMaterialPath));
            }
            else
            {
                if (Arguments.OutputPath != "")
                {
                    string outDir = Path.GetDirectoryName(Arguments.OutputPath);
                    MAT3Section.DumpMaterials(Path.Combine(outDir, "materials.json"));
                }
                else
                {
                    string inDir = Path.GetDirectoryName(Arguments.InputPath);
                    MAT3Section.DumpMaterials(Path.Combine(inDir, "materials.json"));
                }
            }
            foreach (var shape in SHP1Section.Shapes)
                packetCount += shape.Packets.Count;

            vertexCount = VTX1Section.Attributes.Positions.Count;
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

        public Model(Scene assimpScene, List<BMDMaterial> materialPresets = null, string additionalTexPath = null)
        {
            ModelStats = new BMDInfo();
            BCKAnims = new List<BCK>();

            if (Arguments.ShouldEnsureOneMaterialPerMesh || Arguments.IsMaterialOrderStrict)
            {
                EnsureOneMaterialPerMesh(assimpScene);
            }

            if (Arguments.ShouldSortMeshes)
            {
                SortMeshesByObjectNames(assimpScene);
            }

            // For FBX mesh names are empty, instead we need to check the nodes and rename
            // the meshes after the node names.
            foreach (var node in assimpScene.RootNode.Children)
            {
                foreach (int meshindex in node.MeshIndices)
                {
                    Mesh mesh = assimpScene.Meshes[meshindex];
                    if (mesh.Name.IsEmpty())
                    {
                        mesh.Name = node.Name;
                    }
                }
            }

            Console.Write("Searching for the Skeleton Root");
            Node? root = null;
            for (int i = 0; i < assimpScene.RootNode.ChildCount; i++)
            {
                if (assimpScene.RootNode.Children[i].Name.ToLowerInvariant() == "skeleton_root")
                {
                    if (assimpScene.RootNode.Children[i].ChildCount == 0)
                    {
                        throw new System.Exception("skeleton_root has no children! If you are making a rigged model, make sure skeleton_root contains the root of your skeleton.");
                    }
                    root = assimpScene.RootNode.Children[i].Children[0];
                    break;
                }
                Console.Write(".");
            }

            Console.Write(root is null ? "✓\nNo Skeleton found\n" : "✓\nSkeleton Found\n");


            foreach (Mesh mesh in assimpScene.Meshes)
            {
                if (mesh.HasBones && root is null)
                {
                    throw new Exception("Model uses bones but the skeleton root has not been found! Make sure your skeleton is inside a dummy object called 'skeleton_root'.");
                }
            }


            if (Arguments.ShouldRotateModel)
            {

                Console.Write("Rotating the model");
                int i = 0;
                Matrix4x4 rotate = Matrix4x4.FromRotationX((float)(-(1 / 2.0) * Math.PI));
                Matrix4x4 rotateinv = rotate;
                rotateinv.Inverse();


                foreach (Mesh mesh in assimpScene.Meshes)
                {
                    if (root != null)
                    {
                        foreach (Bone bone in mesh.Bones)
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
                Console.Write("✓\n");

            }

            foreach (Mesh mesh in assimpScene.Meshes)
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
            VTX1Section = new VTX1(assimpScene);

            Console.Write("Generating the Bone Data");
            JNT1Section = new JNT1(assimpScene, VTX1Section);

            Console.WriteLine("Generating the Texture Data -> ");
            TEX1Section = new TEX1(assimpScene);

            Console.Write("Generating the Envelope Data");
            EVP1Section = new EVP1();
            EVP1Section.SetInverseBindMatrices(assimpScene, JNT1Section.FlatSkeleton);


            Console.Write("Generating the Weight Data");
            DRW1Section = new DRW1(assimpScene, JNT1Section.BoneNameIndices);
            JNT1Section.UpdateBoundingBoxes(VTX1Section);


            Console.WriteLine("Generating the Mesh Data ->");
            SHP1Section = SHP1.Create(assimpScene, JNT1Section.BoneNameIndices, VTX1Section.Attributes, EVP1Section, DRW1Section);

            //Joints.UpdateBoundingBoxes(VertexData);



            Console.WriteLine("Generating the Material Data ->");
            MAT3Section = new MAT3(assimpScene, TEX1Section, SHP1Section, materialPresets);


            Console.WriteLine("Loading the Textures ->");
            if (additionalTexPath is null)
            {
                MAT3Section.LoadAdditionalTextures(TEX1Section, Path.GetDirectoryName(Arguments.InputPath));
            }
            else
            {
                MAT3Section.LoadAdditionalTextures(TEX1Section, additionalTexPath);
            }

            MAT3Section.MapTextureNamesToIndices(TEX1Section);

            if (Arguments.ShouldExportAsBDL)
            {

                Console.WriteLine("Compiling the MDL3 ->");
                MatDisplayList = new MDL3(MAT3Section.Materials, TEX1Section.Textures);
            }


            Console.Write("Generating the Joints");
            INF1Section = new INF1(assimpScene, JNT1Section, Arguments.IsMaterialOrderStrict);

            foreach (Geometry.Shape shape in SHP1Section.Shapes)
                packetCount += shape.Packets.Count;

            vertexCount = VTX1Section.Attributes.Positions.Count;

            if (Arguments.ShouldExportAnims && assimpScene.AnimationCount > 0)
            {
                foreach (var animation in assimpScene.Animations)
                    BCKAnims.Add(new BCK(animation, JNT1Section.FlatSkeleton));
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
            writer.Write("SuperBMD - Gamma");
            //writer.PadAlign(32);
            INF1Section.Write(ref writer, packetCount, vertexCount);
            VTX1Section.Write(ref writer);
            EVP1Section.Write(ref writer);
            DRW1Section.Write(ref writer);
            JNT1Section.Write(ref writer);
            SHP1Section.Write(ref writer);
            MAT3Section.Write(ref writer);

            if (isBDL)
                MatDisplayList.Write(ref writer);

            TEX1Section.Write(ref writer);

            writer.Seek(8);
            writer.Write((int)writer.Length);


            if (BCKAnims.Count > 0)
            {
                for (int i = 0; i < BCKAnims.Count; i++)
                {
                    string bckName = Path.Combine(outDir, $"anim_{i}.bck");
                    var bckWriter = new EndianBinaryWriter();
                    using var fileStream = new FileStream(bckName, FileMode.Create, FileAccess.Write);
                    BCKAnims[i].Write(ref bckWriter);
                }

            }
            writer.PadAlign(32);
            Console.WriteLine("Output BMD size is " + writer.Length + " bytes!");
            writer.Close();
        }

        public void ExportAssImp(string fileName, string modelType, ExportSettings settings)
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
            var outputScene = new Scene { RootNode = new Node("RootNode") };


            Console.WriteLine("Processing Materials ->");
            MAT3Section.FillScene(outputScene, TEX1Section, outDir);

            Console.WriteLine("Processing Meshes ->");
            SHP1Section.FillScene(outputScene, VTX1Section.Attributes, JNT1Section.FlatSkeleton, EVP1Section.InverseBindMatrices);
            Console.Write("Processing Skeleton");
            INF1Section.FillScene(outputScene, JNT1Section.FlatSkeleton, settings.UseSkeletonRoot);
            INF1Section.CorrectMaterialIndices(outputScene, MAT3Section);

            Console.WriteLine("Processing Textures ->");
            TEX1Section.DumpTextures(outDir, "tex_headers.json", true, Arguments.ShouldReadMipmaps);
            this.INF1Section.DumpJson(Path.Combine(outDir, "hierarchy.json"));
            this.JNT1Section.DumpJson(Path.Combine(outDir, "joints.json"));
            this.DRW1Section.DumpJson(Path.Combine(outDir, "partialweights.json"));
            //this.Shapes.DumpJson(Path.Combine(outDir, "shapes.json"));

            Console.WriteLine("Removing Duplicate Verticies ->");
            foreach (Mesh mesh in outputScene.Meshes)
            {
                Console.Write(mesh.Name.Replace('_', ' ') + ": ");
                // Assimp has a JoinIdenticalVertices post process step, but we can't use that or the skinning info we manually add won't take it into account.
                RemoveDuplicateVertices(mesh);
                Console.Write("✓\n");

            }


            var assimpContext = new AssimpContext();

            if (modelType == "obj")
            {
                Console.WriteLine("Writing the OBJ file...");
                assimpContext.ExportFile(outputScene, fileName, "obj");//, PostProcessSteps.ValidateDataStructure);

                using var streamWriter = new StreamWriter(fileName);
                string mtllibname = fileName.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries).Last() + ".mtl";
                streamWriter.WriteLine($"mtllib {mtllibname}");
                foreach (var vertex in from Mesh mesh in outputScene.Meshes
                                       from Vector3D vertex in mesh.Vertices
                                       select vertex)
                {
                    streamWriter.WriteLine(String.Format("v {0} {1} {2}", vertex.X, vertex.Y, vertex.Z));
                }

                foreach (var normal in from Mesh mesh in outputScene.Meshes
                                       from Vector3D normal in mesh.Normals
                                       select normal)
                {
                    streamWriter.WriteLine(String.Format("vn {0} {1} {2}", normal.X, normal.Y, normal.Z));
                }

                foreach (var uv in from Mesh mesh in outputScene.Meshes
                                   where mesh.HasTextureCoords(0)
                                   from Vector3D uv in mesh.TextureCoordinateChannels[0]
                                   select uv)
                {
                    streamWriter.WriteLine(String.Format("vt {0} {1}", uv.X, uv.Y));
                }

                int vertex_offset = 1;
                foreach (var (mesh, material_name) in from Mesh mesh in outputScene.Meshes
                                                      let material_name = outputScene.Materials[mesh.MaterialIndex].Name
                                                      select (mesh, material_name))
                {
                    streamWriter.WriteLine(String.Format("usemtl {0}", material_name));
                    foreach (Face face in mesh.Faces)
                    {
                        streamWriter.Write("f ");
                        foreach (int index in face.Indices)
                        {
                            streamWriter.Write(index + vertex_offset);
                            if (mesh.HasTextureCoords(0))
                            {
                                streamWriter.Write("/");
                                streamWriter.Write(index + vertex_offset);
                            }
                            if (!mesh.HasTextureCoords(0) && mesh.HasNormals)
                            {
                                streamWriter.Write("//");
                                streamWriter.Write(index + vertex_offset);
                            }
                            else if (mesh.HasNormals)
                            {
                                streamWriter.Write("/");
                                streamWriter.Write(index + vertex_offset);
                            }
                            streamWriter.Write(" ");
                        }
                        streamWriter.Write("\n");
                    }

                    vertex_offset += mesh.VertexCount;
                }

                return;
            }
            else
            {
                assimpContext.ExportFile(outputScene, fileName, "collada", PostProcessSteps.ValidateDataStructure);
            }

            //if (SkinningEnvelopes.Weights.Count == 0)
            //    return; // There's no skinning information, so we can stop here

            // Now we need to add some skinning info, since AssImp doesn't do it for some bizarre reason

            var tempSkinningFile = new StreamWriter(fileName + ".tmp");
            StreamReader dae = File.OpenText(fileName);


            Console.Write("Finalizing the Mesh");
            while (!dae.EndOfStream)
            {
                string line = dae.ReadLine();

                if (line == "  <library_visual_scenes>")
                {
                    AddControllerLibrary(outputScene, tempSkinningFile);
                    tempSkinningFile.WriteLine(line);
                    tempSkinningFile.Flush();
                }
                else if (line.Contains("<node"))
                {
                    string[] testLn = line.Split('\"');
                    string name = testLn[3];

                    if (JNT1Section.FlatSkeleton.Exists(x => x.Name == name))
                    {
                        string jointLine = line.Replace(">", $" sid=\"{name}\" type=\"JOINT\">");
                        tempSkinningFile.WriteLine(jointLine);
                        tempSkinningFile.Flush();
                    }
                    else
                    {
                        tempSkinningFile.WriteLine(line);
                        tempSkinningFile.Flush();
                    }
                }
                else if (line.Contains("</visual_scene>"))
                {
                    foreach (var (mesh, materialName, keepMapNames) in from Mesh mesh in outputScene.Meshes
                                                                       let matname = "mat"
                                                                       let keepmatnames = false
                                                                       select (mesh, matname, keepmatnames))
                    {
                        var finalMaterialName = materialName;
                        if (keepMapNames)
                        {
                            finalMaterialName = AssimpMatnameSanitize(mesh.MaterialIndex, outputScene.Materials[mesh.MaterialIndex].Name);
                        }
                        else
                        {
                            finalMaterialName = AssimpMatnameSanitize(mesh.MaterialIndex, MAT3Section.Materials[mesh.MaterialIndex].Name);
                        }

                        tempSkinningFile.WriteLine($"      <node id=\"{mesh.Name}\" name=\"{mesh.Name}\" type=\"NODE\">");
                        tempSkinningFile.WriteLine($"       <instance_controller url=\"#{mesh.Name}-skin\">");
                        tempSkinningFile.WriteLine("        <skeleton>#skeleton_root</skeleton>");
                        tempSkinningFile.WriteLine("        <bind_material>");
                        tempSkinningFile.WriteLine("         <technique_common>");
                        tempSkinningFile.WriteLine($"          <instance_material symbol=\"m{finalMaterialName}\" target=\"#{finalMaterialName}\" />");
                        tempSkinningFile.WriteLine("         </technique_common>");
                        tempSkinningFile.WriteLine("        </bind_material>");
                        tempSkinningFile.WriteLine("       </instance_controller>");
                        tempSkinningFile.WriteLine("      </node>");
                        tempSkinningFile.Flush();
                    }

                    tempSkinningFile.WriteLine(line);
                    tempSkinningFile.Flush();
                }
                else if (line.Contains("<matrix"))
                {
                    string matLine = line.Replace("<matrix>", "<matrix sid=\"matrix\">");
                    tempSkinningFile.WriteLine(matLine);
                    tempSkinningFile.Flush();
                }
                else
                {
                    tempSkinningFile.WriteLine(line);
                    tempSkinningFile.Flush();
                }
                Console.Write(".");
            }
            Console.Write("✓\n");
            tempSkinningFile.Close();
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

        private static void WriteBindShapeMatrixToStream(StreamWriter writer)
        {
            writer.WriteLine("     <bind_shape_matrix>");
            writer.WriteLine("      1 0 0 0");
            writer.WriteLine("      0 1 0 0");
            writer.WriteLine("      0 0 1 0");
            writer.WriteLine("      0 0 0 1");
            writer.WriteLine("     </bind_shape_matrix>");
            writer.Flush();
        }

        private static void WriteJointNameArrayToStream(Mesh mesh, StreamWriter writer)
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

        private static void WriteInverseBindMatricesToStream(Mesh mesh, StreamWriter writer)
        {
            writer.WriteLine($"      <source id =\"{mesh.Name}-skin-bind_poses-array\">");
            writer.WriteLine($"      <float_array id=\"{mesh.Name}-skin-bind_poses-array\" count=\"{mesh.Bones.Count * 16}\">");
            foreach (Bone bone in mesh.Bones)
            {
                var inverseBindMatricies = bone.OffsetMatrix;
                inverseBindMatricies.Transpose();
                string fmt = "G7";
                writer.WriteLine($"       {inverseBindMatricies.A1.ToString(fmt)} {inverseBindMatricies.A2.ToString(fmt)} {inverseBindMatricies.A3.ToString(fmt)} {inverseBindMatricies.A4.ToString(fmt)}");
                writer.WriteLine($"       {inverseBindMatricies.B1.ToString(fmt)} {inverseBindMatricies.B2.ToString(fmt)} {inverseBindMatricies.B3.ToString(fmt)} {inverseBindMatricies.B4.ToString(fmt)}");
                writer.WriteLine($"       {inverseBindMatricies.C1.ToString(fmt)} {inverseBindMatricies.C2.ToString(fmt)} {inverseBindMatricies.C3.ToString(fmt)} {inverseBindMatricies.C4.ToString(fmt)}");
                writer.WriteLine($"       {inverseBindMatricies.D1.ToString(fmt)} {inverseBindMatricies.D2.ToString(fmt)} {inverseBindMatricies.D3.ToString(fmt)} {inverseBindMatricies.D4.ToString(fmt)}");

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

        private static void WriteSkinWeightsToStream(Mesh mesh, StreamWriter writer)
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

        private static void WriteVertexWeightsToStream(Mesh mesh, StreamWriter writer)
        {
            var weights = new List<float>();
            var vertexIDToWeights = new Dictionary<int, Rigging.Weight>();

            foreach (Bone bone in mesh.Bones)
            {
                foreach (VertexWeight weight in bone.VertexWeights)
                {
                    weights.Add(weight.Weight);

                    if (!vertexIDToWeights.ContainsKey(weight.VertexID))
                        vertexIDToWeights.Add(weight.VertexID, new Rigging.Weight());

                    vertexIDToWeights[weight.VertexID].AddWeight(weight.Weight, mesh.Bones.IndexOf(bone));
                }
            }
            writer.WriteLine($"      <vertex_weights count=\"{vertexIDToWeights.Count}\">");
            writer.WriteLine($"       <input semantic=\"JOINT\" source=\"#{mesh.Name}-skin-joints-array\" offset=\"0\"></input>");
            writer.WriteLine($"       <input semantic=\"WEIGHT\" source=\"#{mesh.Name}-skin-weights-array\" offset=\"1\"></input>");
            writer.WriteLine("       <vcount>");
            writer.Write("        ");
            for (int i = 0; i < vertexIDToWeights.Count; i++)
                writer.Write($"{vertexIDToWeights[i].WeightCount} ");

            writer.WriteLine("\n       </vcount>");
            writer.WriteLine("       <v>");
            writer.Write("        ");

            for (int i = 0; i < vertexIDToWeights.Count; i++)
            {
                Rigging.Weight curWeight = vertexIDToWeights[i];

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

        private static void RemoveDuplicateVertices(Mesh mesh)
        {
            // Calculate which vertices are duplicates (based on their position, texture coordinates, and normals).
            var uniqueVertInfos = new List<Tuple<Vector3D, Vector3D?, List<Vector3D>, List<Color4D>>>();

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
                var originalVertexWeights = new List<VertexWeight>(bone.VertexWeights);
                bone.VertexWeights.Clear();
                for (var i = 0; i < originalVertexWeights.Count; i++)
                {
                    var origWeight = originalVertexWeights[i];
                    int origVertexID = origWeight.VertexID;
                    if (!vertexIsUnique[origVertexID])
                        continue;

                    int newVertexID = replaceVertexIDs[origVertexID];
                    var newWeight = new VertexWeight(newVertexID, origWeight.Weight);
                    bone.VertexWeights.Add(newWeight);
                }
            }
        }

        private static bool CheckVertInfosAreDuplicates(Vector3D vert1, Vector3D? norm1, List<Vector3D> vert1TexCoords, List<Color4D> vert1Colors,
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
        private static void SortMeshesByObjectNames(Scene scene)
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
                    int currMaxNumberLength = node.Name.SelectMany(i => MyRegex().Matches(node.Name).Cast<Match>().Select(m => m.Value.Length))
                                                       .DefaultIfEmpty(0)
                                                       .Max();
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
                throw new Exception($"Number of meshes ({scene.Meshes.Count}) is not the same as the number of mesh objects ({meshNames.Count}); cannot sort.\nMesh objects: {string.Join(", ", meshNames)}\nMeshes: {String.Join(", ", scene.Meshes.Select(mesh => mesh.Name))}");
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
            Console.Write("✓\n");
        }
        private static void EnsureOneMaterialPerMesh(Scene scene)
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

        [GeneratedRegex(@"\d+")]
        private static partial Regex MyRegex();
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
            PrintVertexAttributeInfo(mod.VTX1Section);
            Console.WriteLine("INF: {0} scene nodes", mod.INF1Section.FlatNodes.Count);
            Console.WriteLine("EVP1: {0} weights", mod.EVP1Section.Weights.Count);
            Console.WriteLine("EVP1: {0} inverse bind matrices", mod.EVP1Section.InverseBindMatrices.Count);
            Console.WriteLine("DRW1: {0} WeightTypeCheck flags, {1} indices", mod.DRW1Section.WeightTypeCheck.Count, mod.DRW1Section.Indices.Count);
            Console.WriteLine("JNT1: {0} joints", mod.JNT1Section.FlatSkeleton.Count);
            Console.WriteLine("SHP1: {0} meshes", mod.SHP1Section.Shapes.Count);
            Console.WriteLine("MAT3: {0} materials", mod.MAT3Section.Materials.Count);
            Console.WriteLine("TEX1: {0} textures", mod.TEX1Section.Textures.Count);
            PrintTextureInfo(mod.TEX1Section);
        }

        private static void PrintTextureInfo(TEX1 textures)
        {
            int i = 0;
            Console.WriteLine("Textures in model:");
            foreach (var btiTexture in textures.Textures)
            {
                Console.WriteLine("{0}) {1} Format: {2}, {3}x{4}, {5} mipmaps", i, btiTexture.Name, btiTexture.Format,
                    btiTexture.Width, btiTexture.Height, btiTexture.ImageCount);
                i++;

            }
        }

        private void PrintVertexAttributeInfo(VTX1 vertexData)
        {
            Console.WriteLine("{0} Vertex Positions", vertexData.Attributes.Positions.Count);
            PrintAttributeFormat(vertexData, Geometry.VertexAttribute.Position);
            Console.WriteLine("{0} Vertex Normals", vertexData.Attributes.Normals.Count);
            PrintAttributeFormat(vertexData, Geometry.VertexAttribute.Normal);
            if (vertexData.Attributes.ColorChannel0.Count > 0)
            {
                Console.WriteLine("{0} Vertex Colors (Channel 0)", vertexData.Attributes.ColorChannel0.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.ColorChannel0);
            }
            if (vertexData.Attributes.ColorChannel1.Count > 0)
            {
                Console.WriteLine("{0} Vertex Colors (Channel 1)", vertexData.Attributes.ColorChannel1.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.ColorChannel1);
            }

            Console.WriteLine("{0} Vertex Texture Coords (Channel 0)", vertexData.Attributes.TexCoord0.Count);
            PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord0);

            if (vertexData.Attributes.TexCoord1.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 1)", vertexData.Attributes.TexCoord1.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord1);
            }
            if (vertexData.Attributes.TexCoord2.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 2)", vertexData.Attributes.TexCoord2.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord2);
            }
            if (vertexData.Attributes.TexCoord3.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 3)", vertexData.Attributes.TexCoord3.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord3);
            }
            if (vertexData.Attributes.TexCoord4.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 4)", vertexData.Attributes.TexCoord4.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord4);
            }
            if (vertexData.Attributes.TexCoord5.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 5)", vertexData.Attributes.TexCoord5.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord5);
            }
            if (vertexData.Attributes.TexCoord6.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 6)", vertexData.Attributes.TexCoord6.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord6);
            }
            if (vertexData.Attributes.TexCoord7.Count > 0)
            {
                Console.WriteLine("{0} Vertex Texture Coords (Channel 7)", vertexData.Attributes.TexCoord7.Count);
                PrintAttributeFormat(vertexData, Geometry.VertexAttribute.TexCoord7);
            }
        }
        private static void PrintAttributeFormat(VTX1 vertexData, VertexAttribute attr)
        {
            if (vertexData.StorageFormats.ContainsKey(attr))
            {
                if (!vertexData.StorageFormats.TryGetValue(attr, out Tuple<StorageType, byte> tuple))
                {
                    return;
                }
                Console.WriteLine("Attribute {0} has format {1} with fractional part of {2} bits",
                                            attr, tuple.Item1, tuple.Item2);
            }

        }


    }
}