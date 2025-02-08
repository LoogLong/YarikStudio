using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public sealed class SkinnedMeshRenderer : Renderer
    {
        public PPtr<Mesh> m_Mesh;
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
            var lastPosition = reader.Position;
            reader.Position = reader.byteStart;
            List<long> pendingPathIDs = new();
            var leng = lastPosition + 50 - reader.Position;
            for (int i = 0; i < leng; i++)
            {
                if (reader.Position + i + 8 < reader.Length)
                {
                    var m_MaterialsSize = reader.ReadBytes(i);
                    var pathID = reader.ReadInt64();
                    pendingPathIDs.Add(pathID);
                    reader.Position = lastPosition;
                }
            }
            {
                var entries = ResourceMap.GetEntries();
                var meshs = entries.AsParallel().Where(x => x.Type == ClassIDType.Mesh);

                List<long> maybePathIDs = new();

                foreach (var pathID in pendingPathIDs)
                {
                    var pp = meshs.Where(x => x.PathID == pathID).ToList();
                    if (pp.Count > 0)
                    {
                        maybePathIDs.Add(pathID);
                    }
                }
                if (maybePathIDs.Count > 0)
                {
                    var d = 1;
                }
            }

            m_Mesh = new PPtr<Mesh>(reader);

            var numBones = reader.ReadInt32();
            m_Bones = new List<PPtr<Transform>>();
            for (int b = 0; b < numBones; b++)
            {
                m_Bones.Add(new PPtr<Transform>(reader));
            }

            if (version[0] > 4 || (version[0] == 4 && version[1] >= 3)) //4.3 and up
            {
                m_BlendShapeWeights = reader.ReadSingleArray();
            }

            if (reader.Game.Type.IsGIGroup())
            {
                m_RootBone = new PPtr<Transform>(reader);
                m_AABB = new AABB(reader);
                m_DirtyAABB = reader.ReadBoolean();
                reader.AlignStream();
            }
        }
    }
}
