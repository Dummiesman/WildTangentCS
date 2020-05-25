using System;
using System.IO;

namespace WildTangent
{
    public class WTMaterial
    {
        public string Name;
        public int ID;
        public bool DoubleSided;
        public bool Collideable;
        public bool Visible;
        public int DataID;
        
        public bool ColorKey;
        public Color48 ColorKeyColor;

        public float USpinner;
        public float VSpinner;
        public short HFSpinner;
        public short VFSpinner;
        public float TimeSpinner;

        public float USpinner2;
        public float VSpinner2;
        public short HFSpinner2;
        public short VFSpinner2;
        public float TimeSpinner2;

        public int RenderLast = 0;
        public int RenderFirst = 0;

        public Color48 Color1;
        public Color48 Color2;
        public Color48 Color3;
        public Color48 Color4;

        public float unkFloat1;
        public float unkFloat2;

        /// <summary>
        /// 0 = Diffuse
        /// 1 = Alpha
        /// 2 = Reflective
        /// 3 = Emissive (or additive blend?)
        /// </summary>
        public int[] TextureSlots = new int[7];

        public void ListSlots(WTModel parent)
        {
            for(int i=0; i < TextureSlots.Length; i++)
            {
                int tId = TextureSlots[i];
                string tName = "NULL";

                if(tId >= 0)
                {
                    if (parent.Textures.ContainsKey(tId))
                    {
                        tName = parent.Textures[tId];
                    }
                    else
                    {
                        Name = "INVALID";
                    }
                }

                Console.WriteLine($"{i}\t{tId}\t{tName}");
            }
        }

        public void WriteBinary(BinaryWriter writer)
        {
            writer.WriteNullTerminatedString(Name);
            writer.Write(ID);
            writer.Write(DoubleSided ? 1 : 0);
            writer.Write(Collideable ? 1 : 0);
            writer.Write(Visible ? 1 : 0);
            writer.Write(DataID);

            writer.Write(ColorKey ? 1 : 0);
            if (ColorKey)
            {
                writer.WriteColor48(ColorKeyColor);
            }

            writer.Write((USpinner != 0f || VSpinner != 0f) ? 1 : 0);
            if(USpinner != 0f || VSpinner != 0f)
            {
                writer.Write(USpinner);
                writer.Write(VSpinner);
            }

            writer.Write((HFSpinner != 0 || VFSpinner != 0) ? 1 : 0);
            if(HFSpinner != 0 || VFSpinner != 0)
            {
                writer.Write(HFSpinner);
                writer.Write(VFSpinner);
                writer.Write(TimeSpinner);
            }

            writer.Write((USpinner2 != 0f || VSpinner2 != 0f) ? 1 : 0);
            if (USpinner2 != 0f || VSpinner2 != 0f)
            {
                writer.Write(USpinner2);
                writer.Write(VSpinner2);
            }

            writer.Write((HFSpinner2 != 0 || VFSpinner2 != 0) ? 1 : 0);
            if (HFSpinner2 != 0 || VFSpinner2 != 0)
            {
                writer.Write(HFSpinner2);
                writer.Write(VFSpinner2);
                writer.Write(TimeSpinner2);
            }

            writer.Write(RenderLast);
            writer.Write(RenderFirst);

            writer.WriteColor48(Color1);
            writer.WriteColor48(Color2);
            writer.WriteColor48(Color3);
            writer.WriteColor48(Color4);

            writer.Write(unkFloat1);
            writer.Write(unkFloat2);

            for (int mtc = 0; mtc < 7; mtc++)
            {
                writer.Write(TextureSlots[mtc]);
            }
        }

        public static WTMaterial ReadBinary(BinaryReader reader)
        {
            var material = new WTMaterial()
            {
                Name = reader.ReadNullTerminatedString(),
                ID = reader.ReadInt32(),
                DoubleSided = reader.ReadInt32() == 1,
                Collideable = reader.ReadInt32() == 1,
                Visible = reader.ReadInt32() == 1,
                DataID = reader.ReadInt32()
            };

            material.ColorKey = reader.ReadInt32() == 1;
            if (material.ColorKey)
            {
                material.ColorKeyColor = reader.ReadColor48();
            }

            bool hasUvSpin = reader.ReadInt32() == 1;
            if (hasUvSpin)
            {
                material.USpinner = reader.ReadSingle();
                material.VSpinner = reader.ReadSingle();
            }

            bool hasHfVfSpin = reader.ReadInt32() == 1;
            if (hasHfVfSpin)
            {
                material.HFSpinner = reader.ReadInt16();
                material.VFSpinner = reader.ReadInt16();
                material.TimeSpinner = reader.ReadSingle();
            }

            bool hasUvSpin2 = reader.ReadInt32() == 1;
            if (hasUvSpin2)
            {
                material.USpinner2 = reader.ReadSingle();
                material.VSpinner2 = reader.ReadSingle();
            }

            bool hasHfVfSpin2 = reader.ReadInt32() == 1;
            if (hasHfVfSpin2)
            {
                material.HFSpinner2 = reader.ReadInt16();
                material.VFSpinner2 = reader.ReadInt16();
                material.TimeSpinner2 = reader.ReadSingle();
            }

            material.RenderLast = reader.ReadInt32();
            material.RenderFirst = reader.ReadInt32();

            material.Color1 = reader.ReadColor48();
            material.Color2 = reader.ReadColor48();
            material.Color3 = reader.ReadColor48();
            material.Color4 = reader.ReadColor48();

            material.unkFloat1 = reader.ReadSingle();
            material.unkFloat2 = reader.ReadSingle();

            for (int mtc = 0; mtc < 7; mtc++)
            {
                int texId = reader.ReadInt32();
                material.TextureSlots[mtc] = texId;
            }

            return material;
        }
    }
}
