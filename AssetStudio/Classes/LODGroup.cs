using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public class LOD
    {
        public float screenRelativeHeight;
        public float fadeTransitionWidth;
        public float controlByDistance;
        public List<PPtr<Renderer>> renderers;

        public LOD(ObjectReader reader)
        {
            screenRelativeHeight = reader.ReadSingle();
            fadeTransitionWidth = reader.ReadSingle();
            controlByDistance = reader.ReadSingle();
            var renderersSize = reader.ReadInt32();
            renderers = new List<PPtr<Renderer>>(renderersSize);
            for (int i = 0; i < renderersSize; i++)
            {
                renderers.Add(new PPtr<Renderer>(reader));
            }
        }
    }
    public sealed class LODGroup : Component
    {

        public LODGroup(ObjectReader reader) : base(reader)
        {
            var m_LocalReferencePoint = reader.ReadVector3();
            var m_Size = reader.ReadSingle();
            var m_FadeMode = reader.ReadInt32();
            var m_AnimateCrossFading = reader.ReadBoolean();
            var m_LastLODIsBillboard = reader.ReadBoolean();

            var m_LODSize = reader.ReadInt32();
            var m_LODs = new List<LOD>();
            for (int i = 0; i < m_LODSize; i++)
            {
                m_LODs.Add(new LOD(reader));
            }
            var m_Enabled = reader.ReadBoolean();
            var m_DisableCulled = reader.ReadBoolean();
            var m_RegardLod0AsLod1 = reader.ReadBoolean();
            var m_UseDistance = reader.ReadBoolean();
            var m_NoCulledUseDistance = reader.ReadBoolean();
        }
    }
}
