using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio
{
    public sealed class ParticleSystemRenderer : Renderer
    {
        public PPtr<Mesh> m_Mesh;

        public ParticleSystemRenderer(ObjectReader reader) : base(reader)
        {
            //var currentPos = reader.Position;
            //while (currentPos + 12 < reader.BaseStream.Length)
            //{
            //    var fileID = reader.ReadInt32();
            //    var PathID = reader.m_Version < SerializedFileFormatVersion.Unknown_14 ? reader.ReadInt32() : reader.ReadInt64();
            //    bool bValidPathID = reader.assetsFile.m_Objects.Any(x => x.m_PathID == PathID);
            //    if (bValidPathID)
            //    {
            //        reader.Position = currentPos;
            //        break;
            //    }
            //    currentPos += 4;
            //}
            //var activeVertexStreamsCount = reader.ReadInt32();
            //var alignment = reader.ReadInt32();
            //var cameraVelocityScale = reader.ReadSingle();
            //var lengthScale = reader.ReadSingle();
            //var maskInteraction = reader.ReadInt32();
            //var maxParticleSize = reader.ReadSingle();
            //m_Mesh = new PPtr<Mesh>(reader);
            //var meshCount = reader.ReadInt32();
            //var minParticleSize = reader.ReadSingle();

        }
    }
}
