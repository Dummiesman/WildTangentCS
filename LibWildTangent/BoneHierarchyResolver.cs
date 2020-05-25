#if UNITY
using UnityEngine;
#else
using System.Numerics;
#endif

namespace WildTangent
{
    class BoneHierarchyResolver
    {
        private WTSkinnedModel model;

        public Vector3 GetAbsolutePosition(SkinnedModelBone bone)
        {
            Vector3 current = bone.Origin;
            int parentId = bone.ParentIndex;
            while(parentId >= 0)
            {
                current += model.Bones[parentId].Origin;
                parentId = model.Bones[parentId].ParentIndex;
            }
            return current;
        }

        public BoneHierarchyResolver(WTSkinnedModel mdl)
        {
            this.model = mdl;
        }
    }
}
