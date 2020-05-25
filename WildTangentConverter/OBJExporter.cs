using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WildTangent;

namespace WildTangentConverter
{
    public class OBJExporter
    {
        //
        public Vector3 Scale = Vector3.One;
        public string SourcePath = string.Empty; 

        //
        private WTModel model;
        private bool useAbsolutePaths = false;
        private bool onlyExportSecondUv = false;

        public void Export(string path)
        {            
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(path);

            //create builders
            StringBuilder objBuilder = new StringBuilder();
            StringBuilder mtlBuilder = new StringBuilder();
            objBuilder.AppendLine($"mtllib {fileNameWithoutExtension}.mtl");

            //
            int builderVertexOffset = 0;
            int builderNormalOffset = 0;
            int builderUvOffset = 0;

            Console.WriteLine("Processing materials...");
            foreach (var material in model.Materials.Values)
            {
                void writeTextureSlot(string slotType, int slotId)
                {
                    if (material.TextureSlots[slotId] >= 0)
                    {
                        int textureId = material.TextureSlots[slotId];
                        if (model.Textures.ContainsKey(textureId))
                        {
                            string textureName = model.Textures[textureId];
                            if (useAbsolutePaths)
                                mtlBuilder.AppendLine($"{slotType} {System.IO.Path.Combine(SourcePath, textureName)}");
                            else
                                mtlBuilder.AppendLine($"{slotType} {textureName}");
                        }
                    }
                }

                mtlBuilder.AppendLine($"newmtl {material.Name}");
                writeTextureSlot("map_Kd", 0);
                writeTextureSlot("map_d", 1);
                writeTextureSlot("map_Ke", 3);

                mtlBuilder.AppendLine();
            }

            //
            Console.WriteLine("Processing meshes...");
            for (int mshc = 0; mshc < model.Meshes.Count; mshc++)
            {
                var mesh = model.Meshes[mshc];
                if (onlyExportSecondUv && !mesh.HasSecondUVChannel)
                    continue;

                Console.Write($"Mesh {mshc}... ");

                //add to obj file
                if (mesh.Vertices.Count > 0)
                {
                    Console.Write("v ");
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                    {
                        Vector3 centerScaled = mesh.Position * Scale;
                        Vector3 vertexScaled = mesh.Vertices[i] * Scale;
                        objBuilder.AppendLine($"v {-(vertexScaled.X + centerScaled.X)} { vertexScaled.Y + centerScaled.Y } { vertexScaled.Z + centerScaled.Z}");
                    }
                }
                if (mesh.UVs.Count > 0)
                {
                    Console.Write("vt ");
                    for (int i = 0; i < mesh.UVs.Count; i++)
                        objBuilder.AppendLine($"vt {mesh.UVs[i].X} {mesh.UVs[i].Y}");
                }
                if (mesh.Normals.Count > 0)
                {
                    Console.Write("vn ");
                    for (int i = 0; i < mesh.Normals.Count; i++)
                        objBuilder.AppendLine($"vn {-mesh.Normals[i].X} {mesh.Normals[i].Y} {mesh.Normals[i].Z}");
                }

                Console.Write("f ");
                objBuilder.AppendLine($"o Submesh{mshc}");
                foreach (var submesh in mesh.Submeshes)
                {
                    var indexData = submesh.Indices;
                    int materialIndex = (onlyExportSecondUv) ? submesh.MaterialIndexAlt : submesh.MaterialIndex;
                    string mtlName = (materialIndex < 0 || !model.Materials.ContainsKey(materialIndex)) ? "nomaterial" :
                                                                                                           model.Materials[materialIndex].Name;

                    objBuilder.AppendLine($"usemtl {mtlName}");
                    for (int i = 0; i < indexData.Count / 3; i++)
                    {
                        string makeIndexStr(int indexIndex)
                        {
                            string retStr = $"{mesh.VertexIndexMap[indexIndex] + 1 + builderVertexOffset}";
                            if (mesh.UVs.Count == 0)
                            {
                                retStr += "/";
                            }
                            else
                            {
                                retStr += $"/{mesh.UVsIndexMap[indexIndex] + 1 + builderUvOffset}";
                            }
                            if (mesh.Normals.Count > 0)
                            {
                                retStr += $"/{mesh.NormalsIndexMap[indexIndex] + 1 + builderNormalOffset}";
                            }
                            return retStr;
                        }

                        int indexIndex1 = indexData[i * 3];
                        int indexIndex2 = indexData[(i * 3) + 1];
                        int indexIndex3 = indexData[(i * 3) + 2];
                        string indexStr1 = makeIndexStr(indexIndex1);
                        string indexStr2 = makeIndexStr(indexIndex2);
                        string indexStr3 = makeIndexStr(indexIndex3);

                        objBuilder.AppendLine($"f {indexStr3} {indexStr2} {indexStr1}");
                    }
                }
                Console.WriteLine();

                builderVertexOffset += mesh.Vertices.Count;
                builderUvOffset += mesh.UVs.Count;
                builderNormalOffset += mesh.Normals.Count;
            }


            Console.WriteLine("Processing splines...");
            for (int splc = 0; splc < model.Splines.Count; splc++)
            {
                var spline = model.Splines[splc];
                Console.WriteLine($"Spline {splc}... ");
                objBuilder.AppendLine($"o {spline.Name}");

                Vector3 origin = spline.Position;
                foreach(var subspline in spline.SubSplines)
                {
                    for(int i=0; i < subspline.Points.Count; i++)
                    {
                        var point = subspline.Points[i];
                        objBuilder.AppendLine($"v {(point.X * -Scale.X) + (origin.X * -Scale.X)} {(point.Y * Scale.Y) + (origin.Y * Scale.Y)} {(point.Z * Scale.Z) + (origin.Z * Scale.Z)}");
                    }
                    for(int i=0; i < subspline.Points.Count- 1; i++)
                    {
                        objBuilder.AppendLine($"l {builderVertexOffset + 1 + i} {builderVertexOffset + 2 + i}");
                    }
                    if(subspline.Closed)
                        objBuilder.AppendLine($"l {builderVertexOffset + 1} {builderVertexOffset + 1 + subspline.Points.Count - 1}");
                    builderVertexOffset += subspline.Points.Count;
                }
            }

            //write files
            string mtlPath = System.IO.Path.ChangeExtension(path, "mtl");
            System.IO.File.WriteAllText(path, objBuilder.ToString());
            System.IO.File.WriteAllText(mtlPath, mtlBuilder.ToString());
        }

        public OBJExporter(WTModel model, bool useAbsolutePaths, bool onlyExportUv2)
        {
            this.onlyExportSecondUv = onlyExportUv2;
            this.model = model;
            this.useAbsolutePaths = useAbsolutePaths;
        }
    }
}
