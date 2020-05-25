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
    public struct OriginAndMatrix
    {
        public Vector3 Origin;
        public Matrix3x3 Matrix;

        public static OriginAndMatrix ReadBinary(BinaryReader reader)
        {
            Vector3 origin = reader.ReadVector3();
            Matrix3x3 mtx = new Matrix3x3();
            for (int j = 0; j < 3; j++)
            {
                float x = reader.ReadInt16() / 32767f;
                float y = reader.ReadInt16() / 32767f;
                float z = reader.ReadInt16() / 32767f;
                mtx.SetRow(j, new Vector3(x, y, z));
            }
            return new OriginAndMatrix() { Matrix = mtx, Origin = origin };
        }
    }

    public class WTAnimation
    {
        //constants
        private const int HEADER_MAGIC = 0x302E3156; // "V2.0"

        //members
        public List<string> Tags = new List<string>();
        public List<List<OriginAndMatrix>> TagMatrices = new List<List<OriginAndMatrix>>();
        public List<List<OriginAndMatrix>> FrameMatrices = new List<List<OriginAndMatrix>>();
        public List<int> FrameUnkData = new List<int>();
        public List<string> BoneNames = new List<string>();
        
        public static WTAnimation ReadBinary(Stream stream)
        {
            var reader = new BinaryReader(stream);

            //Header is 5 bytes long. V2.0 + a null byte
            int headerMagic = reader.ReadInt32();
            byte nullByte = reader.ReadByte();
            if (headerMagic != HEADER_MAGIC || nullByte != 0)
            {
                reader.Dispose();
                throw new Exception("Bad SMA header!");
            }
            reader.BaseStream.Seek(4, SeekOrigin.Current);

            //cosntruct model
            var returnAnim = new WTAnimation();

            int unkaCount = reader.ReadInt32();
            int unkCount = reader.ReadInt32();
            int tagCount = reader.ReadInt32();
            int frameCount = reader.ReadInt32();

            //read tags
            for(int i=0; i < tagCount; i++)
            {
                returnAnim.Tags.Add(reader.ReadNullTerminatedString());
            }

            //read in tag poses for each frame
            for(int i=0; i < tagCount; i++)
            {
                List<OriginAndMatrix> frameList = new List<OriginAndMatrix>();
                for(int j=0; j < frameCount; j++)
                {
                    frameList.Add(OriginAndMatrix.ReadBinary(reader));
                }
                returnAnim.TagMatrices.Add(frameList);
            }

            //read in.. some int for each frame?
            for (int i = 0; i < frameCount; i++)
                returnAnim.FrameUnkData.Add(reader.ReadInt32());

            //read in bones and bone poses for each frame
            int animBoneCount = reader.ReadInt32();
            for(int i=0; i < animBoneCount; i++)
            {
                string bnName = reader.ReadNullTerminatedString();
                returnAnim.BoneNames.Add(bnName);
            }
            for(int i=0; i < frameCount; i++)
            {
                List<OriginAndMatrix> frameList = new List<OriginAndMatrix>();
                for (int j = 0; j < animBoneCount; j++)
                {
                    frameList.Add(OriginAndMatrix.ReadBinary(reader));
                }
                returnAnim.FrameMatrices.Add(frameList);
            }

            /*
             * Console.WriteLine($"After SMA parse: {reader.BaseStream.Length - reader.BaseStream.Position} bytes remain");
            Console.WriteLine("int dump");
            foreach(var unkInt in returnAnim.FrameUnkData)
            {
                Console.WriteLine(unkInt);
            }
            */
            reader.Dispose();
            return returnAnim;
        }

        public static WTAnimation ReadBinary(string inFilePath)
        {
            return ReadBinary(File.OpenRead(inFilePath));
        }
    }
}
