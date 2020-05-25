using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif

namespace WildTangent
{
    public static class BinaryExtensions
    {
        public static void ReadList<T>(this System.IO.BinaryReader reader, Func<T> readFunc, List<T> list, int count)
        {
            for (int i = 0; i < count; i++)
                list.Add(readFunc.Invoke());
        }

        public static void WriteList<T>(this System.IO.BinaryWriter writer, Action<T> writeFunc, List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
                writeFunc.Invoke(list[i]);
        }

        public static void WriteColor48(this System.IO.BinaryWriter writer, Color48 col)
        {
            writer.Write(col.R);
            writer.Write(col.G);
            writer.Write(col.B);
        }

        public static void WriteColor(this System.IO.BinaryWriter writer, Color col)
        {
            writer.Write(col.R);
            writer.Write(col.G);
            writer.Write(col.B);
        }

        public static void WriteVector3(this System.IO.BinaryWriter writer, Vector3 vec)
        {
#if UNITY
            writer.Write(vec.x);
            writer.Write(vec.y);
            writer.Write(vec.z);
#else
            writer.Write(vec.X);
            writer.Write(vec.Y);
            writer.Write(vec.Z);
#endif
        }

        public static void WriteVector2(this System.IO.BinaryWriter writer, Vector2 vec)
        {
#if UNITY
            writer.Write(vec.x);
            writer.Write(vec.y);
#else
            writer.Write(vec.X);
            writer.Write(vec.Y);
#endif
        }

        public static Vector3 ReadVector3(this System.IO.BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        public static Vector2 ReadVector2(this System.IO.BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            return new Vector2(x, y);
        }

        public static Color ReadColor(this System.IO.BinaryReader reader)
        {
            float r = reader.ReadSingle();
            float g = reader.ReadSingle();
            float b = reader.ReadSingle();
            return new Color(r, g, b);
        }

        public static void WriteMatrix3x3(this System.IO.BinaryWriter writer, Matrix3x3 mtx)
        {
            writer.WriteVector3(mtx.GetRow(0));
            writer.WriteVector3(mtx.GetRow(1));
            writer.WriteVector3(mtx.GetRow(2));
        }

        public static Matrix3x3 ReadMatrix3x3(this System.IO.BinaryReader reader)
        {
            return new Matrix3x3()
            {
                m00 = reader.ReadSingle(),
                m01 = reader.ReadSingle(),
                m02 = reader.ReadSingle(),
                m10 = reader.ReadSingle(),
                m11 = reader.ReadSingle(),
                m12 = reader.ReadSingle(),
                m20 = reader.ReadSingle(),
                m21 = reader.ReadSingle(),
                m22 = reader.ReadSingle()
            };
        }

        public static Color48 ReadColor48(this System.IO.BinaryReader reader)
        {
            return new Color48(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
        }

        public static void WriteNullTerminatedString(this System.IO.BinaryWriter writer, string str)
        {
            for (int i = 0; i < str.Length; i++)
                writer.Write((byte)str[i]);
            writer.Write((byte)0);
        }

        public static string ReadNullTerminatedString(this System.IO.BinaryReader reader)
        {
            string str = "";
            while (true)
            {
                byte b = reader.ReadByte();
                if (b == 0)
                    break;
                str += (char)b;
            }
            return str;
        }
    }
}
