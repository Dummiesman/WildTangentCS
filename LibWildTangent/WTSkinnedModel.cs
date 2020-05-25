using System;
using System.Collections.Generic;
using System.IO;
#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif

namespace WildTangent
{
    public class SkinnedModelBone
    {
        public string Name;
        public int ParentIndex;
        public Vector3 Origin;
        public Matrix3x3 Matrix;

        public static SkinnedModelBone ReadBinary(BinaryReader reader)
        {
            var returnBone = new SkinnedModelBone();
            returnBone.Name = reader.ReadNullTerminatedString();
            returnBone.ParentIndex = reader.ReadInt32();
            returnBone.Origin = reader.ReadVector3();

            Matrix3x3 mtx = new Matrix3x3();
            for(int i=0; i < 3; i++)
            {
                float x = reader.ReadInt16() / 32767f;
                float y = reader.ReadInt16() / 32767f;
                float z = reader.ReadInt16() / 32767f;
                mtx.SetRow(i, new Vector3(x, y, z));
            }
            returnBone.Matrix = mtx;
                
            return returnBone;
        }
    }

    public struct SkinnedModelFace
    {
        public int Index0;
        public int Index1;
        public int Index2;
        public int Materialindex;
    }

    public struct SkinnedModelVertexLayer
    {
        public int BoneIndex;
        public Vector3 BoneOffset;
        public byte b4;
        public byte b5;
        public float f6;
    }

    public struct SkinnedModelVertex
    {
        public List<SkinnedModelVertexLayer> LayerData;
    }

    public class WTSkinnedObject
    {
        public string Name;
        public List<SkinnedModelFace> Faces = new List<SkinnedModelFace>();
        public List<Vector2> UVs = new List<Vector2>();
        public List<SkinnedModelVertex> Vertices = new List<SkinnedModelVertex>();

#if !UNITY
        public void Dump()
        {
            Console.WriteLine($"==== OBJECT {Name} ====");
            Console.WriteLine($"Indices[{Faces.Count}]:");
            foreach(var index in Faces)
            {
                Console.WriteLine($"\t{index.Index0}\t\t{index.Index1}\t\t{index.Index2}\t\t{index.Materialindex}");
            }
            Console.WriteLine($"Uvs[{Vertices.Count}]:");
            foreach(var uv in UVs)
            {
                Console.WriteLine($"\t{uv.X}\t\t{uv.Y}");
            }
            Console.WriteLine($"Vertices[{Vertices.Count}]:");
            foreach(var vert in Vertices)
            {
                Console.WriteLine($"\tLayerCount: {vert.LayerData.Count}");
                foreach(var ld in vert.LayerData)
                {
                    Console.WriteLine("\t{");
                    Console.WriteLine($"\t\t{ld.BoneIndex}");
                    Console.WriteLine($"\t\t{ld.BoneOffset.X} {ld.BoneOffset.Y} {ld.BoneOffset.Z}");
                    Console.WriteLine($"\t\t{ld.b4}");
                    Console.WriteLine($"\t\t{ld.b5}");
                    Console.WriteLine($"\t\t{ld.f6}");
                    Console.WriteLine("\t}");
                }
            }
        }
#endif

        public static WTSkinnedObject ReadBinary(BinaryReader reader)
        {
            var returnObject = new WTSkinnedObject();

            returnObject.Name = reader.ReadNullTerminatedString();
            reader.BaseStream.Seek(64 - returnObject.Name.Length - 1, SeekOrigin.Current); //64 bytes string

            int unk1 = reader.ReadInt32();
            int someFlag = reader.ReadInt32();
            int vertCount = reader.ReadInt32();
            int faceCount = reader.ReadInt32();
            int writeAfter2 = reader.ReadInt32();
            int headerLengthFromOrigin = reader.ReadInt32();
            int faceListLengthFromOrigin = reader.ReadInt32();
            int vertexListLengthFromOrigin = reader.ReadInt32();
            int totalObjectFileSize = reader.ReadInt32(); //total object file size
                                                          //FACE DATA
            for (int j = 0; j < faceCount; j++)
            {
                var face = new SkinnedModelFace()
                {
                    Index0 = reader.ReadInt32(),
                    Index1 = reader.ReadInt32(),
                    Index2 = reader.ReadInt32(),
                    Materialindex = reader.ReadInt32()
                };
                returnObject.Faces.Add(face);
            }

            //UVS?
            for (int j = 0; j < vertCount; j++)
            {
                returnObject.UVs.Add(reader.ReadVector2());
            }

            //SOMETHING * VERT COUNT
            for (int j = 0; j < vertCount; j++)
            {
                int layerCount = reader.ReadInt32();
                List<SkinnedModelVertexLayer> layers = new List<SkinnedModelVertexLayer>();

                for (int k = 0; k < layerCount; k++)
                {
                    int i0 = reader.ReadInt32();
                    float f1 = reader.ReadSingle();
                    float f2 = reader.ReadSingle();
                    float f3 = reader.ReadSingle();
                    byte b4 = reader.ReadByte();
                    byte b5 = reader.ReadByte();
                    float f6 = reader.ReadSingle();

                    layers.Add(new SkinnedModelVertexLayer()
                    {
                        BoneIndex = i0,
                        BoneOffset = new Vector3(f1, f2, f3),
                        b4 = b4,
                        b5 = b5,
                        f6 = f6
                    });
                }

                returnObject.Vertices.Add(new SkinnedModelVertex() { LayerData = layers });
            }

            return returnObject;
        }
    }

    public class WTSkinnedModel
    {
        //constants
        private const int HEADER_MAGIC = 0x302E3256; // "V2.0"

        //members
        public Dictionary<int, string> Textures = new Dictionary<int, string>();
        public Dictionary<int, WTMaterial> Materials = new Dictionary<int, WTMaterial>();
        public List<Tuple<string, string>> UnkStringList = new List<Tuple<string, string>>();
        public List<SkinnedModelBone> Bones = new List<SkinnedModelBone>();
        public List<WTSkinnedObject> Objects = new List<WTSkinnedObject>();

        public static WTSkinnedModel ReadBinary(Stream stream)
        {
            var reader = new BinaryReader(stream);
            
            //Header is 5 bytes long. V2.0 + a null byte
            int headerMagic = reader.ReadInt32();
            byte nullByte = reader.ReadByte();
            if(headerMagic != HEADER_MAGIC || nullByte != 0)
            {
                reader.Dispose();
                throw new Exception("Bad SMS header!");
            }
            reader.BaseStream.Seek(4, SeekOrigin.Current);

            //cosntruct model
            var returnModel = new WTSkinnedModel();

            int objectCount = reader.ReadInt32();
            int unkCount = reader.ReadInt32();
            
            int tagCount = reader.ReadInt32();
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

            //weird tag list
            for(int i=0; i < tagCount; i++)
            {
                string item1 = reader.ReadNullTerminatedString();
                
                bool hasPair2 = reader.ReadUInt16() != 0;
                string item2 = (hasPair2) ? reader.ReadNullTerminatedString() : null;

                returnModel.UnkStringList.Add(new Tuple<string, string>(item1, item2));
            }

            for(int i=0; i < objectCount; i++)
            {
                var obj = WTSkinnedObject.ReadBinary(reader);
                returnModel.Objects.Add(obj);
            }

            //read bone count
            int boneCount = reader.ReadInt32();
            for(int i=0; i < boneCount; i++)
            {
                returnModel.Bones.Add(SkinnedModelBone.ReadBinary(reader));
            }

            reader.Dispose();
            return returnModel;
        }

        public static WTSkinnedModel ReadBinary(string inFilePath)
        {
            return ReadBinary(File.OpenRead(inFilePath));
        }
    }
}
