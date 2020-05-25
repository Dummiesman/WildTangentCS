using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WildTangent
{
    public class WTModel
    {
        //members
        public Dictionary<int, string> Textures = new Dictionary<int, string>();
        public Dictionary<int, WTMaterial> Materials = new Dictionary<int, WTMaterial>();
        public List<WTMesh> Meshes = new List<WTMesh>();
        public List<WTLight> Lights = new List<WTLight>();
        public List<WTHelper> Helpers = new List<WTHelper>();
        public List<WTSpline> Splines = new List<WTSpline>();

        public void WriteBinary(Stream stream, bool saveAsScene = false)
        {
            var writer = new BinaryWriter(stream);

            //idk what these are
            if (saveAsScene)
            {
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(1f);
                writer.Write(0.02745098f);
                writer.Write(0.02745098f);
                writer.Write(0.02745098f);
            }

            writer.Write(1f); // version
            writer.Write(Meshes.Count);
            writer.Write(Lights.Count);
            writer.Write(Helpers.Count);
            writer.Write(Splines.Count);

            writer.Write(Materials.Count);
            writer.Write(Textures.Count);

            foreach (var texture in Textures.OrderBy(x => x.Key))
            {
                writer.WriteNullTerminatedString(texture.Value);
                writer.Write(texture.Key);
            }

            foreach (var material in Materials.OrderBy(x => x.Key))
            {
                material.Value.WriteBinary(writer);
            }

            foreach (var mesh in Meshes)
            {
                mesh.WriteBinary(writer, saveAsScene);
            }

            foreach (var light in Lights)
            {
                light.WriteBinary(writer);
            }

            foreach (var helper in Helpers)
            {
                helper.WriteBinary(writer);
            }

            foreach (var spline in Splines)
            {
                spline.WriteBinary(writer);
            }

            writer.Dispose();
        }

        public static WTModel ReadBinary(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var returnModel = new WTModel();

            //determine filetype
            bool isSceneFile = false;
            reader.BaseStream.Seek(24, SeekOrigin.Begin);

            float version = reader.ReadSingle();
            if (version == 1f)
            {
                isSceneFile = true;
            }
            else
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                version = reader.ReadSingle();
                if (version != 1f)
                {
                    throw new System.Exception($"Expected version 1.0, got version {version}. Either not a MDL/SCN file, or wrong version.");
                }
            }

            //read data
            int meshCount = reader.ReadInt32();
            int lightCount = reader.ReadInt32();
            int helperCount = reader.ReadInt32();
            int splineCount = reader.ReadInt32();

            int materialCount = reader.ReadInt32();
            int textureCount = reader.ReadInt32();

            //textures
            for (int i = 0; i < textureCount; i++)
            {
                string tex = reader.ReadNullTerminatedString();
                int id = reader.ReadInt32();
                returnModel.Textures.Add(id, tex);
            }

            //materials
            for (int i = 0; i < materialCount; i++)
            {
                var material = WTMaterial.ReadBinary(reader);
                returnModel.Materials.Add(material.ID, material);
            }

            //meshes
            for (int i = 0; i < meshCount; i++)
            {
                returnModel.Meshes.Add(WTMesh.ReadBinary(reader, isSceneFile));
            }

            //lights
            for (int i = 0; i < lightCount; i++)
            {
                returnModel.Lights.Add(WTLight.ReadBinary(reader));
            }

            //helpers
            for (int i = 0; i < helperCount; i++)
            {
                returnModel.Helpers.Add(WTHelper.ReadBinary(reader));
            }

            //splines
            for (int i = 0; i < splineCount; i++)
            {
                returnModel.Splines.Add(WTSpline.ReadBinary(reader));
            }

            reader.Dispose();
            return returnModel;
        }

        public static WTModel ReadBinary(string inFilePath)
        {
            return ReadBinary(File.OpenRead(inFilePath));
        }
    }

}
