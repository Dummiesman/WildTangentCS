using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WildTangent;

namespace WildTangentConverter
{
    public class DAEExporter
    {
        //
        public Vector3 Scale = Vector3.One;
        public string SourcePath = string.Empty;

        //
        private WTModel model;
        private bool useAbsolutePaths = false;
        private bool useUniqueUvMapNames = false;

        private static string Vec2ListToFloatList(List<Vector2> list)
        {
            StringBuilder floatList = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                floatList.Append($"{value.X} ");
                floatList.Append($"{value.Y}");
                if (i != list.Count - 1)
                    floatList.Append(" ");
            }
            return floatList.ToString();
        }

        private static string Vec3ListToFloatList(List<Vector3> list)
        {
            StringBuilder floatList = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                floatList.Append($"{value.X} ");
                floatList.Append($"{value.Y} ");
                floatList.Append($"{value.Z}");
                if (i != list.Count - 1)
                    floatList.Append(" ");
            }
            return floatList.ToString();
        }

        private static string ColorListToFloatList(List<Color48> list)
        {
            StringBuilder floatList = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                var value = list[i];
                floatList.Append($"{value.R / 255f} ");
                floatList.Append($"{value.G / 255f} ");
                floatList.Append($"{value.G / 255f}");
                if (i != list.Count - 1)
                    floatList.Append(" ");
            }
            return floatList.ToString();
        }

        private static void AddDAESource(string id, List<Vector2> data, StringBuilder writer, string accessor0 = "X", string accessor1 = "Y")
        {
            writer.AppendLine($"\t\t\t\t<source id=\"{id}\">");
            writer.AppendLine($"\t\t\t\t\t<float_array id=\"{id}-array\" count=\"{data.Count * 2}\">{Vec2ListToFloatList(data)}</float_array>");
            writer.AppendLine($"\t\t\t\t\t<technique_common>");
            writer.AppendLine($"\t\t\t\t\t\t<accessor source=\"#{id}-array\" count=\"{data.Count}\" stride=\"2\">");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"{accessor0}\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"{accessor1}\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t</accessor>");
            writer.AppendLine($"\t\t\t\t\t</technique_common>");
            writer.AppendLine($"\t\t\t\t</source>");
        }

        private static void AddDAESource(string id, List<Vector3> data, StringBuilder writer, string accessor0 = "X", string accessor1 = "Y", string accessor2 = "Z")
        {
            writer.AppendLine($"\t\t\t\t<source id=\"{id}\">");
            writer.AppendLine($"\t\t\t\t\t<float_array id=\"{id}-array\" count=\"{data.Count * 3}\">{Vec3ListToFloatList(data)}</float_array>");
            writer.AppendLine($"\t\t\t\t\t<technique_common>");
            writer.AppendLine($"\t\t\t\t\t\t<accessor source=\"#{id}-array\" count=\"{data.Count}\" stride=\"3\">");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"{accessor0}\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"{accessor1}\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"{accessor2}\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t</accessor>");
            writer.AppendLine($"\t\t\t\t\t</technique_common>");
            writer.AppendLine($"\t\t\t\t</source>");
        }

        private static void AddDAESource(string id, List<Color48> data, StringBuilder writer, string name)
        {
            writer.AppendLine($"\t\t\t\t<source id=\"{id}\" name=\"{name}\">");
            writer.AppendLine($"\t\t\t\t\t<float_array id=\"{id}-array\" count=\"{data.Count * 3}\">{ColorListToFloatList(data)}</float_array>");
            writer.AppendLine($"\t\t\t\t\t<technique_common>");
            writer.AppendLine($"\t\t\t\t\t\t<accessor source=\"#{id}-array\" count=\"{data.Count}\" stride=\"3\">");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"R\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"G\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t\t<param name=\"B\" type=\"float\"/>");
            writer.AppendLine($"\t\t\t\t\t\t</accessor>");
            writer.AppendLine($"\t\t\t\t\t</technique_common>");
            writer.AppendLine($"\t\t\t\t</source>");
        }

        private static string CreateTriangleArray(WTMesh mesh, List<int> indices)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < indices.Count / 3; i++)
            {
                void appendIndices(int index)
                {
                    int indexIndex = indices[(i * 3) + index];
                    builder.Append($"{mesh.VertexIndexMap[indexIndex]}");
                    if (mesh.Normals.Count > 0)
                        builder.Append($" {mesh.NormalsIndexMap[indexIndex]}");
                    if (mesh.UVs.Count > 0)
                        builder.Append($" {mesh.UVsIndexMap[indexIndex]}");
                    if (mesh.HasSecondUVChannel && mesh.UVsIndexMapC2.Count > 0)
                        builder.Append($" {mesh.UVsIndexMapC2[indexIndex]}");
                    if (mesh.Colors.Count > 0)
                        builder.Append($" {indexIndex}");
                }

                appendIndices(0);
                builder.Append(" ");

                appendIndices(1);
                builder.Append(" ");

                appendIndices(2);
                if (i < (indices.Count / 3) - 1)
                    builder.Append(" ");
            }
            return builder.ToString();
        }

        private static string CleanNameOrId(string nameOrId)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(nameOrId.Replace(" ","_").Replace("\t","_"), "_");
        }

        private static string GetLightXmlElement(WTLightType type)
        {
            switch (type)
            {
                case WTLightType.DIRECTIONAL:
                    return "directional";
                case WTLightType.OMNI:
                    return "point";
                case WTLightType.SPOT:
                    return "spot";
            }
            return "unknownlighttype";
        }

        private static string CreateLightColorString(Color color)
        {
            return $"<color sid=\"color\">{color.R} {color.G} {color.B}</color>";
        }

        private void WriteMaterialPrameter(string sid, WTMaterial material, int index, StringBuilder writer, string @default = "1 1 1 1")
        {
            writer.AppendLine($"\t\t\t\t\t\t<{sid}>");
            if (material.TextureSlots[index] >= 0 && model.Textures.ContainsKey(material.TextureSlots[index]))
            {
                int key = material.TextureSlots[index];
                writer.AppendLine($"\t\t\t\t\t\t\t<texture texture=\"{CleanNameOrId(model.Textures[key])}-sampler\" texcoord=\"UVMap\"/>");
            }
            else if(@default != null)
            {
                writer.AppendLine($"\t\t\t\t\t\t\t<color sid=\"{sid}\">{@default}</color>");
            }
            writer.AppendLine($"\t\t\t\t\t\t</{sid}>");
        }

        public void Export(string path)
        {
            var writer = new StringBuilder();
            writer.AppendLine(Properties.Resources.DAEHeader);

            //write materials
            Console.WriteLine("Writing library_effects...");
            writer.AppendLine("\t<library_effects>");
            foreach (var material in model.Materials)
            {
                writer.AppendLine($"\t\t<effect id=\"{CleanNameOrId(material.Value.Name)}-effect\">");
                writer.AppendLine($"\t\t\t<profile_COMMON>");

                foreach(int textureRef in material.Value.TextureSlots)
                {
                    if (model.Textures.ContainsKey(textureRef))
                    {
                        int key = textureRef;
                        writer.AppendLine($"\t\t\t\t<newparam sid=\"{CleanNameOrId(model.Textures[key])}-surface\">");
                        writer.AppendLine($"\t\t\t\t\t<surface type=\"2D\">");
                        writer.AppendLine($"\t\t\t\t\t\t<init_from>{CleanNameOrId(model.Textures[key])}</init_from>");
                        writer.AppendLine($"\t\t\t\t\t</surface>");
                        writer.AppendLine($"\t\t\t\t</newparam>");
                        writer.AppendLine($"\t\t\t\t<newparam sid=\"{CleanNameOrId(model.Textures[key])}-sampler\">");
                        writer.AppendLine($"\t\t\t\t\t<sampler2D>");
                        writer.AppendLine($"\t\t\t\t\t\t<source>{CleanNameOrId(model.Textures[key])}-surface</source>");
                        writer.AppendLine($"\t\t\t\t\t</sampler2D>");
                        writer.AppendLine($"\t\t\t\t</newparam>");
                    }
                }

                writer.AppendLine($"\t\t\t\t<technique sid=\"common\">");
                writer.AppendLine($"\t\t\t\t\t<lambert>");

                WriteMaterialPrameter("diffuse", material.Value, 0, writer);
                WriteMaterialPrameter("reflective", material.Value, 2, writer, null);
                WriteMaterialPrameter("emission", material.Value, 3, writer, null);
                

                writer.AppendLine($"\t\t\t\t\t\t<index_of_refraction>");
                writer.AppendLine($"\t\t\t\t\t\t\t<float sid=\"ior\">1.45</float>");
                writer.AppendLine($"\t\t\t\t\t\t</index_of_refraction>");
                writer.AppendLine($"\t\t\t\t\t</lambert>");
                writer.AppendLine($"\t\t\t\t</technique>");
                writer.AppendLine($"\t\t\t</profile_COMMON>");
                writer.AppendLine($"\t\t</effect>");
            }
            writer.AppendLine("\t</library_effects>");

            Console.WriteLine("Writing library_materials...");
            writer.AppendLine("\t<library_materials>");
            foreach (var material in model.Materials)
            {
                writer.AppendLine($"\t\t<material id=\"{CleanNameOrId(material.Value.Name)}-material\" name=\"{material.Value.Name}\">");
                writer.AppendLine($"\t\t\t<instance_effect url=\"#{CleanNameOrId(material.Value.Name)}-effect\"/>");
                writer.AppendLine($"\t\t</material>");
            }
            writer.AppendLine("\t</library_materials>");

            //write textures
            if (model.Textures.Count > 0)
            {
                Console.WriteLine("Writing library_images...");
                writer.AppendLine("\t<library_images>");
                foreach (var texture in model.Textures.Values)
                {
                    string textureId = CleanNameOrId(texture);
                    writer.AppendLine($"\t\t<image id=\"{textureId}\" name=\"{textureId}\">");
                    if (useAbsolutePaths)
                    {
                        string fullTexturePath = System.IO.Path.Combine(SourcePath, texture);
                        string colladaAbsolutePath = "file:///" + fullTexturePath.Replace(System.IO.Path.DirectorySeparatorChar, '/').Replace(" ", "%20");
                        writer.AppendLine($"\t\t\t<init_from>{colladaAbsolutePath}</init_from>");
                    }
                    else
                    {
                        writer.AppendLine($"\t\t\t<init_from>{texture}</init_from>");
                    }
                    writer.AppendLine($"\t\t</image>");
                }
                writer.AppendLine("\t</library_images>");
            }

            //write lights
            if (model.Lights.Count > 0)
            {
                Console.WriteLine("Writing library_lights...");
                writer.AppendLine("\t<library_lights>");
                foreach (var light in model.Lights)
                {
                    string lightId = CleanNameOrId(light.Name);
                    writer.AppendLine($"\t\t<light id=\"{lightId}-light\" name=\"{light.Name}\">");
                    writer.AppendLine($"\t\t\t<technique_common>");
                    writer.AppendLine($"\t\t\t\t<{GetLightXmlElement(light.Type)}>");
                    writer.AppendLine($"\t\t\t\t\t{CreateLightColorString(light.Color)}");
                    writer.AppendLine($"\t\t\t\t</{GetLightXmlElement(light.Type)}>");
                    writer.AppendLine($"\t\t\t</technique_common>");
                    writer.AppendLine($"\t\t</light>");
                }
                writer.AppendLine("\t</library_lights>");
            }

            //write geometry
            Console.WriteLine("Processing meshes...");
            writer.AppendLine("\t<library_geometries>");
            for (int mc = 0; mc < model.Meshes.Count; mc++)
            {
                Console.Write($"Mesh {mc}... ");
                var mesh = model.Meshes[mc];
                string objectName = $"Submesh{mc}";
                writer.AppendLine($"\t\t<geometry id=\"{objectName}-mesh\" name=\"{objectName}\">");
                writer.AppendLine($"\t\t\t<mesh>");

                Console.Write("sources ");
                AddDAESource($"{objectName}-mesh-positions", mesh.Vertices, writer);
                if (mesh.Normals.Count > 0)
                    AddDAESource($"{objectName}-mesh-normals", mesh.Normals, writer);
                if (mesh.UVs.Count > 0)
                {
                    AddDAESource($"{objectName}-mesh-map-0", mesh.UVs, writer, "S", "T");
                }
                if (mesh.HasSecondUVChannel && mesh.UVsIndexMapC2.Count > 0)
                {
                    AddDAESource($"{objectName}-mesh-map-1", mesh.UVs, writer, "S", "T");
                }
                if (mesh.Colors.Count > 0)
                    AddDAESource($"{objectName}-mesh-colors-colors", mesh.Colors, writer, "colors");

                writer.AppendLine($"\t\t\t\t<vertices id=\"{objectName}-mesh-vertices\">");
                writer.AppendLine($"\t\t\t\t\t<input semantic=\"POSITION\" source=\"#{objectName}-mesh-positions\"/>");
                writer.AppendLine($"\t\t\t\t</vertices>");

                Console.Write("indices ");
                foreach (var submesh in mesh.Submeshes)
                {
                    int mtlIndex = submesh.MaterialIndex;
                    string mtlName = (mtlIndex >= 0 && model.Materials.ContainsKey(mtlIndex)) ? model.Materials[mtlIndex].Name : null;

                    var indices = submesh.Indices;
                    int count = indices.Count / 3;

                    if (mtlName != null)
                        writer.AppendLine($"\t\t\t\t<triangles material=\"{CleanNameOrId(mtlName)}-material\" count=\"{count}\">");
                    else
                        writer.AppendLine($"\t\t\t\t<triangles count=\"{count}\">");

                    int offset = 0;
                    writer.AppendLine($"\t\t\t\t\t<input semantic=\"VERTEX\" source=\"#{objectName}-mesh-vertices\" offset=\"{offset++}\"/>");

                    if (mesh.Normals.Count > 0)
                        writer.AppendLine($"\t\t\t\t\t<input semantic=\"NORMAL\" source=\"#{objectName}-mesh-normals\" offset=\"{offset++}\"/>");
                    if (mesh.UVs.Count > 0)
                        writer.AppendLine($"\t\t\t\t\t<input semantic=\"TEXCOORD\" source=\"#{objectName}-mesh-map-0\" offset=\"{offset++}\" set=\"0\"/>");
                    if (mesh.HasSecondUVChannel && mesh.UVsIndexMapC2.Count > 0)
                        writer.AppendLine($"\t\t\t\t\t<input semantic=\"TEXCOORD\" source=\"#{objectName}-mesh-map-1\" offset=\"{offset++}\" set=\"1\"/>");
                    if (mesh.Colors.Count > 0)
                        writer.AppendLine($"\t\t\t\t\t<input semantic=\"COLOR\" source=\"#{objectName}-mesh-colors-colors\" offset=\"{offset++}\" set=\"0\"/>");

                    writer.AppendLine($"\t\t\t\t\t<p>{CreateTriangleArray(mesh, indices)}</p>");
                    writer.AppendLine($"\t\t\t\t</triangles>");
                }

                writer.AppendLine($"\t\t\t</mesh>");
                writer.AppendLine($"\t\t</geometry>");
                Console.WriteLine();
            }
            writer.AppendLine("\t</library_geometries>");

            Console.WriteLine("Writing scene...");
            writer.AppendLine("\t<library_visual_scenes>");
            writer.AppendLine("\t\t<visual_scene id=\"Scene\" name=\"Scene\">");
            foreach(var light in model.Lights)
            {
                string libraryName = CleanNameOrId(light.Name);
                writer.AppendLine($"\t\t\t<node id=\"{libraryName}\" name=\"{light.Name}\" type=\"NODE\">");
                writer.AppendLine($"\t\t\t\t<matrix sid=\"transform\">{light.Matrix.m00 * Scale.X} {light.Matrix.m01} {light.Matrix.m02} {light.Position.X * Scale.X} {light.Matrix.m10} {light.Matrix.m11 * Scale.Y} {light.Matrix.m12} {light.Position.Y * Scale.Y} {light.Matrix.m20} {light.Matrix.m21} {light.Matrix.m22 * Scale.Z} {light.Position.Z * Scale.Z} 0 0 0 1</matrix>");
                writer.AppendLine($"\t\t\t\t<instance_light url=\"#{libraryName}-light\"/>");
                writer.AppendLine("\t\t\t</node>");
            }
            foreach(var helper in model.Helpers)
            {
                string libraryName = CleanNameOrId(helper.Name);
                writer.AppendLine($"\t\t\t<node id=\"{libraryName}\" name=\"{helper.Name}\" type=\"NODE\">");
                writer.AppendLine($"\t\t\t\t<matrix sid=\"transform\">{helper.Matrix.m00 * Scale.X} {helper.Matrix.m01} {helper.Matrix.m02} {helper.Position.X * Scale.X} {helper.Matrix.m10} {helper.Matrix.m11 * Scale.Y} {helper.Matrix.m12} {helper.Position.Y * Scale.Y} {helper.Matrix.m20} {helper.Matrix.m21} {helper.Matrix.m22 * Scale.Z} {helper.Position.Z * Scale.Z} 0 0 0 1</matrix>"); 
                 writer.AppendLine("\t\t\t</node>");
            }
            for (int mc = 0; mc < model.Meshes.Count; mc++)
            {
                var mesh = model.Meshes[mc];
                string objectName = $"Submesh{mc}";

                writer.AppendLine($"\t\t\t<node id=\"{objectName}\" name=\"{objectName}\" type=\"NODE\">");
                writer.AppendLine($"\t\t\t\t<matrix sid=\"transform\">{Scale.X} 0 0 {mesh.Position.X * Scale.X} 0 {Scale.Y} 0 {mesh.Position.Y * Scale.Y} 0 0 {Scale.Z} {mesh.Position.Z * Scale.Z} 0 0 0 1</matrix>");
                writer.AppendLine($"\t\t\t\t<instance_geometry url=\"#{objectName}-mesh\" name=\"{objectName}\">");
                writer.AppendLine("\t\t\t\t\t<bind_material>");
                writer.AppendLine("\t\t\t\t\t\t<technique_common>");

                foreach (var submesh in mesh.Submeshes)
                {
                    int mtlIndex = submesh.MaterialIndex    ;
                    string mtlName = (mtlIndex >= 0 && model.Materials.ContainsKey(mtlIndex)) ? model.Materials[mtlIndex].Name : null;

                    if (mtlName != null)
                    {
                        writer.AppendLine($"\t\t\t\t\t\t\t<instance_material symbol=\"{CleanNameOrId(mtlName)}-material\" target=\"#{CleanNameOrId(mtlName)}-material\">");
                        if(mesh.UVs.Count > 0)
                        {
                            writer.AppendLine($"\t\t\t\t\t\t\t\t<bind_vertex_input semantic=\"UVMap\" input_semantic=\"TEXCOORD\" input_set=\"0\"/>");
                        }
                        if(mesh.HasSecondUVChannel && mesh.UVsIndexMapC2.Count > 0)
                        {
                            writer.AppendLine($"\t\t\t\t\t\t\t\t<bind_vertex_input semantic=\"UVMap2\" input_semantic=\"TEXCOORD\" input_set=\"1\"/>");
                        }
                        writer.AppendLine($"\t\t\t\t\t\t\t</instance_material>");
                    }
                }

                writer.AppendLine("\t\t\t\t\t\t</technique_common>");
                writer.AppendLine("\t\t\t\t\t</bind_material>");
                writer.AppendLine("\t\t\t\t</instance_geometry>");
                writer.AppendLine("\t\t\t</node>");
            }
            writer.AppendLine("\t\t</visual_scene>");
            writer.AppendLine("\t</library_visual_scenes>");

            writer.AppendLine("\t<scene>");
            writer.AppendLine("\t\t<instance_visual_scene url=\"#Scene\"/>");
            writer.AppendLine("\t</scene>");

            writer.AppendLine("</COLLADA>");

            System.IO.File.WriteAllText(path, writer.ToString());
        }

        public DAEExporter(WTModel model, bool useAbsolutePaths)
        {
            this.model = model;
            this.useAbsolutePaths = useAbsolutePaths;
        }
    }
}
