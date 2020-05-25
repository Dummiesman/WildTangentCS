using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif

namespace WildTangent
{
    public enum WTLightType
    {
        OMNI = 0,
        SPOT = 1,
        UNK2 = 2,
        UNK3 = 3,
        DIRECTIONAL = 4
    }

    public class WTSubSpline
    {
        public bool Closed;
        public List<Vector3> Points = new List<Vector3>();

        public void WriteBinary(BinaryWriter writer)
        {
            writer.Write((ushort)Points.Count);
            writer.Write(Closed ? (byte)1 : (byte)0);

            for (int i = 0; i < Points.Count; i++)
            {
                writer.WriteVector3(Points[i]);
            }
        }

        public static WTSubSpline ReadBinary(BinaryReader reader)
        {
            var subspline = new WTSubSpline();

            int subSplinePtCount = reader.ReadUInt16();
            subspline.Closed = reader.ReadByte() != 0;

            for (int k = 0; k < subSplinePtCount; k++)
            {
                subspline.Points.Add(reader.ReadVector3());
            }

            return subspline;
        }
    }

    public class WTSpline
    {
        public string Name;
#if UNITY
        public Vector3 Position = Vector3.zero;
#else
        public Vector3 Position = Vector3.Zero;
#endif

        public List<WTSubSpline> SubSplines = new List<WTSubSpline>();
        public string Reference = null;

        public void WriteBinary(BinaryWriter writer)
        {
            writer.WriteNullTerminatedString(Name);

            writer.WriteVector3(Position);
            writer.Write((ushort)SubSplines.Count);
            for(int i=0; i < SubSplines.Count; i++)
            {
                SubSplines[i].WriteBinary(writer);
            }

            bool hasReference = !string.IsNullOrEmpty(Reference) && !string.IsNullOrWhiteSpace(Reference);
            writer.Write(hasReference ? (ushort)1 : (ushort)0);
            if (hasReference)
                writer.WriteNullTerminatedString(Reference);
        }

        public static WTSpline ReadBinary(BinaryReader reader)
        {
            var spline = new WTSpline
            {
                Name = reader.ReadNullTerminatedString()
            };

            spline.Position = reader.ReadVector3();

            int subSplineCount = reader.ReadUInt16();
            for (int j = 0; j < subSplineCount; j++)
            {
                var subspline = WTSubSpline.ReadBinary(reader);
                spline.SubSplines.Add(subspline);
            }

            bool additionalString = reader.ReadUInt16() != 0;
            if (additionalString)
            {
                string additionalStringval = reader.ReadNullTerminatedString();
                spline.Reference = additionalStringval;
            }

            return spline;
        }
    }

    public class WTHelper
    {
        public string Name = string.Empty;
#if UNITY
        public Vector3 Position = Vector3.zero;
#else
        public Vector3 Position = Vector3.Zero;
#endif
        public Matrix3x3 Matrix;
        public string Reference = null;

        public void WriteBinary(BinaryWriter writer)
        {
            writer.WriteNullTerminatedString(Name);

            writer.WriteVector3(Position);
            writer.WriteMatrix3x3(Matrix);

            bool hasReference = !string.IsNullOrEmpty(Reference) && !string.IsNullOrWhiteSpace(Reference);
            writer.Write(hasReference ? (ushort)1 : (ushort)0);
            if (hasReference)
                writer.WriteNullTerminatedString(Reference);
        }

        public static WTHelper ReadBinary(BinaryReader reader)
        {
            var helper = new WTHelper
            {
                Name = reader.ReadNullTerminatedString()
            };

            helper.Position = reader.ReadVector3();
            helper.Matrix = reader.ReadMatrix3x3();


            bool isReference = reader.ReadUInt16() == 1;
            if (isReference)
            {
                string refPath = reader.ReadNullTerminatedString();
                helper.Reference = refPath;
            }

            return helper;
        }
    }

    public class WTLight
    {
        public string Name = string.Empty;
        public WTLightType Type;
        public Vector3 Position;
        public Matrix3x3 Matrix;
        public Color Color;
        public float Unknown;
        public string Reference = null;

        public void WriteBinary(BinaryWriter writer)
        {
            writer.WriteNullTerminatedString(Name);
            writer.Write((ushort)Type);

            writer.WriteVector3(Position);
            writer.WriteMatrix3x3(Matrix);

            writer.WriteColor(Color);
            writer.Write(Unknown);

            bool hasReference = !string.IsNullOrEmpty(Reference) && !string.IsNullOrWhiteSpace(Reference);
            writer.Write(hasReference ? (ushort)1 : (ushort)0);
            if (hasReference)
                writer.WriteNullTerminatedString(Reference);
        }

        public static WTLight ReadBinary(BinaryReader reader)
        {
            var light = new WTLight()
            {
                Name = reader.ReadNullTerminatedString(),
                Type = (WTLightType)reader.ReadUInt16()
            };

            //matrix?
            light.Position = reader.ReadVector3();
            light.Matrix = reader.ReadMatrix3x3();

            //color?
            light.Color = reader.ReadColor();

            //intensity?
            light.Unknown = reader.ReadSingle();

            //?
            bool additionalString = reader.ReadUInt16() != 0;
            if (additionalString)
            {
                string additionalStringval = reader.ReadNullTerminatedString();
                light.Reference = additionalStringval;
            }

            return light;
        }
    }

    public class WTSubmesh
    {
        public short MaterialIndex;
        public short MaterialIndexAlt;
        public List<int> Indices = new List<int>();
    }

    public class WTMesh
    {
#if UNITY
        public Vector3 Position = Vector3.zero;
#else
        public Vector3 Position = Vector3.Zero;
#endif
        public bool HasSecondUVChannel = false;

        public List<Vector3> Vertices = new List<Vector3>();
        
        public List<int> GlobalIndices = new List<int>();
        public List<int> VertexIndexMap = new List<int>();

        public List<Vector3> Normals = new List<Vector3>();
        public List<int> NormalsIndexMap = new List<int>();

        public List<Vector2> UVs = new List<Vector2>();
        public List<int> UVsIndexMap = new List<int>();
        public List<int> UVsIndexMapC2 = new List<int>();

        public List<Color48> Colors = new List<Color48>();
        public List<int> ColorsIndexMap = new List<int>();

        public List<WTSubmesh> Submeshes = new List<WTSubmesh>();

        public void WriteBinary(BinaryWriter writer, bool isSceneMesh)
        {
            writer.WriteVector3(Position);
            
            if (isSceneMesh)
            {
                writer.Write(HasSecondUVChannel ? (ushort)1 : (ushort)0);
            }

            //INDICES
            writer.Write(VertexIndexMap.Count);
            for (int i = 0; i < VertexIndexMap.Count; i++)
                writer.Write(VertexIndexMap[i]);

            //VERTICES
            writer.Write(Vertices.Count);
            for (int i = 0; i < Vertices.Count; i++)
                writer.WriteVector3(Vertices[i]);

            //UVS
            writer.Write((UVs.Count > 0) ? (ushort)1 : (ushort)0);
            if(UVs.Count > 0)
            {
                for (int i = 0; i < UVsIndexMap.Count; i++)
                {
                    writer.Write(UVsIndexMap[i]);
                    if (isSceneMesh && HasSecondUVChannel)
                        writer.Write(UVsIndexMapC2[i]);
                }

                writer.Write(UVs.Count);
                for (int i = 0; i < UVs.Count; i++)
                    writer.WriteVector2(UVs[i]);
            }

            //NORMALS
            writer.Write((Normals.Count > 0) ? (ushort)1 : (ushort)0);
            if (Normals.Count > 0)
            {
                for (int i = 0; i < NormalsIndexMap.Count; i++)
                    writer.Write(NormalsIndexMap[i]);

                writer.Write(Normals.Count);
                for (int i = 0; i < Normals.Count; i++)
                {
                    //encode normal
#if UNITY
                    double nAtan = Math.Atan2(Normals[i].y, Normals[i].x);
                    double nAcos = Math.Acos(Normals[i].z);
#else
                    double nAtan = Math.Atan2(Normals[i].Y, Normals[i].X);
                    double nAcos = Math.Acos(Normals[i].Z);
#endif
                    byte nAtanByte = (byte)(nAtan * 57.29578 * 0.70833331);
                    byte nAcosByte = (byte)(nAcos * 57.29578 * 0.70833331);

                    writer.Write(nAcosByte);
                    writer.Write(nAtanByte);
                }
            }

            //COLORS
            writer.Write(Colors.Count);
            for (int i = 0; i < Colors.Count; i++)
                writer.WriteColor48(Colors[i]);

            //FACES
            writer.Write(Submeshes.Sum(x => x.Indices.Count)); //total
            writer.Write(Submeshes.Count); //submeshcount
            foreach(var submesh in Submeshes)
            {
                writer.Write((ushort)submesh.MaterialIndex); //material
                if (isSceneMesh && HasSecondUVChannel)
                    writer.Write((ushort)submesh.MaterialIndexAlt);

                writer.Write(submesh.Indices.Count / 3); //tricount
                for (int i = 0; i < submesh.Indices.Count; i++)
                    writer.Write(submesh.Indices[i]);
            }
        }

        public static WTMesh ReadBinary(BinaryReader reader, bool isSceneMesh)
        {
            var returnMesh = new WTMesh
            {
                Position = reader.ReadVector3()
            };

            if (isSceneMesh)
            {
                returnMesh.HasSecondUVChannel = reader.ReadUInt16() != 0;
            }

            //INDICES
            int indexCount = reader.ReadInt32();
            for (int i = 0; i < indexCount; i++)
                returnMesh.VertexIndexMap.Add(reader.ReadInt32());

            //VERTICES
            int vertCount = reader.ReadInt32();
            for (int i = 0; i < vertCount; i++)
            {
                returnMesh.Vertices.Add(reader.ReadVector3());
            }

            //UVS
            //Console.WriteLine($"reading uvs at {reader.BaseStream.Position}, idxc {indexCount} uv2 {returnMesh.SceneThing}");
            bool modelHasUvs = (reader.ReadUInt16() != 0);
            if (modelHasUvs)
            {
                for (int i = 0; i < indexCount; i++)
                {
                    returnMesh.UVsIndexMap.Add(reader.ReadInt32());
                    if (returnMesh.HasSecondUVChannel && isSceneMesh)
                    {
                        returnMesh.UVsIndexMapC2.Add(reader.ReadInt32());
                    }
                }

                int uvCount = reader.ReadInt32();
                for (int i = 0; i < uvCount; i++)
                {
                    returnMesh.UVs.Add(reader.ReadVector2());
                }
            }

            //NORMALS
            bool modelHasNormals = (reader.ReadUInt16() != 0);
            if (modelHasNormals)
            {
                for (int i = 0; i < indexCount; i++)
                {
                    returnMesh.NormalsIndexMap.Add(reader.ReadInt32());
                }

                int normalCount = reader.ReadInt32();
                for (int i = 0; i < normalCount; i++)
                {
                    float elevation = reader.ReadByte() * 1.411764752387544784530887019841f / 57.29578f;
                    float polar = reader.ReadByte() * 1.411764752387544784530887019841f / 57.29578f;

#if UNITY
                    float a = (float)(1f * Mathf.Sin(elevation));
                    Vector3 normal = new Vector3
                    {
                        x = (float)(a * Mathf.Cos(polar)),
                        y = (float)(Mathf.Cos(elevation)),
                        z = (float)(a * Mathf.Sin(polar))
                    };
#else
                    float a = (float)(1f * Math.Sin(elevation));
                    Vector3 normal = new Vector3
                    {
                        X = (float)(a * Math.Cos(polar)),
                        Y = (float)(Math.Cos(elevation)),
                        Z = (float)(a * Math.Sin(polar))
                    };
#endif
                    returnMesh.Normals.Add(normal);
                }
            }

            //COLORS
            int numColors = reader.ReadInt32();
            if (numColors > 0)
            {
                for (int i = 0; i < numColors; i++) 
                {
                    returnMesh.Colors.Add(reader.ReadColor48());
                }
            }

            //FACES
            int faceCountTotal = reader.ReadInt32();
            int faceSubmeshes = reader.ReadInt32();
            for (int i = 0; i < faceSubmeshes; i++)
            {
                var submesh = new WTSubmesh
                {
                    MaterialIndex = reader.ReadInt16()
                };
                if (returnMesh.HasSecondUVChannel && isSceneMesh)
                {
                    submesh.MaterialIndexAlt = reader.ReadInt16();
                }

                int submeshIndexCount = reader.ReadInt32();
                int submeshTriCount = submeshIndexCount * 3;

                for (int j = 0; j < submeshTriCount; j++)
                {
                    submesh.Indices.Add(reader.ReadInt32());
                }

                //add to data list
                returnMesh.Submeshes.Add(submesh);
            }

            return returnMesh;
        }
    }
}
