#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif

namespace WildTangent
{
    public struct Matrix3x3
    {
        public float m00, m01, m02;
        public float m10, m11, m12;
        public float m20, m21, m22;

#if UNITY
        public Matrix4x4 ToMatrix4x4()
        {
            var mtx = Matrix4x4.identity;
            var thisCopy = this;
            void applySet(int set)
            {
                var ms = mtx.GetRow(set);
                var ls = thisCopy.GetColumn(set);
                ms.x = ls.x;
                ms.y = ls.y;
                ms.z = ls.z;
                mtx.SetRow(set, ms);
            }
            applySet(0);
            applySet(1);
            applySet(2);
            return mtx;
        }
#endif

        public void SetColumn(int col, Vector3 vector)
        {
#if UNITY
            switch (col)
            {
                case 0:
                    m00 = vector.x;
                    m10 = vector.y;
                    m20 = vector.z;
                    break;
                case 1:
                    m01 = vector.x;
                    m11 = vector.y;
                    m21 = vector.z;
                    break;

                case 2:
                    m02 = vector.x;
                    m12 = vector.y;
                    m22 = vector.z;
                    break;
            }
#endif
        }

        public void SetRow(int row, Vector3 vector)
        {
#if UNITY
            switch (row)
            {
                case 0:
                    m00 = vector.x;
                    m01 = vector.y;
                    m01 = vector.z;
                    break;
                case 1:
                    m10 = vector.x;
                    m11 = vector.y;
                    m12 = vector.z;
                    break;

                case 2:
                    m20 = vector.x;
                    m21 = vector.y;
                    m22 = vector.z;
                    break;
            }
#endif
        }

        public Vector3 GetColumn(int col)
        {
            switch (col)
            {
                case 0:
                    return new Vector3(m00, m10, m20);
                case 1:                      
                    return new Vector3(m01, m11, m21);
                case 2:                      
                    return new Vector3(m02, m12, m22);
            }
#if UNITY
            return Vector3.zero;
#else
            return Vector3.Zero;
#endif
        }

        public Vector3 GetRow(int row)
        {
            switch (row)
            {
                case 0:
                    return new Vector3(m00, m01, m02);
                case 1:
                    return new Vector3(m10, m11, m12);
                case 2:
                    return new Vector3(m20, m21, m22);
            }
#if UNITY
            return Vector3.zero;
#else
            return Vector3.Zero;
#endif
        }
    }
}
