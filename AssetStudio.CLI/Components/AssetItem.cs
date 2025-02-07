﻿namespace AssetStudio.CLI
{
    public class AssetItem
    {
        public string Text;
        public Object Asset;
        public SerializedFile SourceFile;
        public string Container = string.Empty;
        public string TypeString;
        public long m_PathID;
        public long FullSize;
        public ClassIDType Type;
        public string InfoText;
        public string UniqueID;
        public string ExportFileName;

        public AssetItem(Object asset)
        {
            Asset = asset;
            Text = asset.Name;
            //Text = $"{asset.Name}.{asset.type.ToString()}.{asset.m_PathID}";
            SourceFile = asset.assetsFile;
            Type = asset.type;
            TypeString = Type.ToString();
            m_PathID = asset.m_PathID;
            FullSize = asset.byteSize;
        }
    }
}
