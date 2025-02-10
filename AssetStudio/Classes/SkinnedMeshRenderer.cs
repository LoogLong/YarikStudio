using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public sealed class SkinnedMeshRenderer : Renderer
    {
        public PPtr<Mesh> m_Mesh;
        public Mesh m_out_mesh;
        public List<PPtr<Transform>> m_Bones;
        public float[] m_BlendShapeWeights;
        public PPtr<Transform> m_RootBone;
        public AABB m_AABB;
        public bool m_DirtyAABB;

        public SkinnedMeshRenderer(ObjectReader reader) : base(reader)
        {
            int m_Quality = reader.ReadInt32();
            var m_UpdateWhenOffscreen = reader.ReadBoolean();
            var m_SkinNormals = reader.ReadBoolean(); //3.1.0 and below
            reader.AlignStream();

            if (version[0] == 2 && version[1] < 6) //2.6 down
            {
                var m_DisableAnimationWhenOffscreen = new PPtr<Animation>(reader);
            }

            // zzz 结构不一样 这里重新定位到开始位置 重头读取
            reader.Position = reader.byteStart;
            
            // read SkinnedMeshRenderer
            var m_GameObject = new PPtr<GameObject>(reader); // size = 12
            var m_Enabled = reader.ReadBoolean();// size = 1 
            var m_CastShadows = reader.ReadByte();// size = 1
            var m_ReceiveShadows = reader.ReadByte();// size = 1
            var m_DynamicOccludee = reader.ReadByte();// size = 1

            var m_MotionVectors = reader.ReadByte();// size = 1
            var m_LightProbeUsage = reader.ReadByte();// size = 1
            var m_ReflectionProbeUsage = reader.ReadByte();// size = 1
            var m_RayTracingMode = reader.ReadByte();// size = 1

            var m_RayTraceProcedural = reader.ReadByte();// size = 1

            reader.Position += 3; // 32bit align

            var m_RenderingLayerMask = reader.ReadUInt32();// size = 4
            var m_RendererPriority = reader.ReadInt32();// size = 4
            var m_LightmapIndex = reader.ReadUInt16();// size = 2
            var m_LightmapIndexDynamic = reader.ReadUInt16();// size = 2
            var m_LightmapTilingOffset = reader.ReadVector4();// size = 16
            var m_LightmapTilingOffsetDynamic = reader.ReadVector4();// size = 16

            var m_MaterialsSize = reader.ReadInt32();
            var m_Materials = new List<PPtr<Material>>();
            for (int i = 0; i < m_MaterialsSize; i++)
            {
                m_Materials.Add(new PPtr<Material>(reader));
            }

            var m_StaticBatchInfo = new StaticBatchInfo(reader); // size = 4
            var m_StaticBatchRoot = new PPtr<Transform>(reader); // size = 12
            var m_ProbeAnchor = new PPtr<Transform>(reader); // size = 12
            var m_LightProbeVolumeOverride = new PPtr<GameObject>(reader); // size = 12
            var m_SortingLayerID = reader.ReadInt32(); // size = 4

            var m_SortingLayer = reader.ReadInt16(); // size = 2
            var m_SortingOrder = reader.ReadInt16(); // size = 2

            var m_NeedHizCulling = reader.ReadByte(); // size = 1
            var m_HighShadingRate = reader.ReadByte(); // size = 1
            var m_RayTracingLayerMask = reader.ReadByte(); // size = 1
            reader.Position += 1; // 32bit align

            var m_CullingDistance = reader.ReadSingle(); // size = 4
            var m_Quality11 = reader.ReadInt32(); // size = 1
            var m_UpdateWhenOffscreen11 = reader.ReadBoolean(); // size = 1
            var m_SkinnedMotionVectors11 = reader.ReadBoolean(); // size = 1
            reader.Position += 2; // 32bit align

            m_Mesh = new PPtr<Mesh>(reader);

            var numBones = reader.ReadInt32();
            m_Bones = new List<PPtr<Transform>>();
            for (int b = 0; b < numBones; b++)
            {
                m_Bones.Add(new PPtr<Transform>(reader));
            }

            var m_SortingFudge = reader.ReadSingle();

            if (version[0] > 4 || (version[0] == 4 && version[1] >= 3)) //4.3 and up
            {
                m_BlendShapeWeights = reader.ReadSingleArray();
            }

            if (reader.Game.Type.IsGIGroup() || reader.Game.Type.IsZZZ())
            {
                m_RootBone = new PPtr<Transform>(reader);
                m_AABB = new AABB(reader);
                m_DirtyAABB = reader.ReadBoolean();
                reader.AlignStream();
            }
        }
    }
}
