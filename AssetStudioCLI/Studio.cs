﻿using AssetStudio;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static AssetStudioCLI.Exporter;
using Object = AssetStudio.Object;
using System.Globalization;
using System.Xml;

namespace AssetStudioCLI
{
    [Flags]
    public enum MapOpType
    {
        None,
        Load,
        Build,
        Both,
        List,
        All = Build | Load | List
    }

    public enum AssetGroupOption
    {
        ByType,
        ByContainer,
        BySource,
        None,
    }

    internal static class Studio
    {
        public static Game Game;
        public static bool ModelOnly = false;
        public static bool SkipContainer = false;
        public static AssetsManager assetsManager = new AssetsManager() { ResolveDependencies = false };
        public static AssemblyLoader assemblyLoader = new AssemblyLoader();
        public static List<AssetItem> exportableAssets = new List<AssetItem>();

        public static int ExtractFolder(string path, string savePath)
        {
            int extractedCount = 0;
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileOriPath = Path.GetDirectoryName(file);
                var fileSavePath = fileOriPath.Replace(path, savePath);
                extractedCount += ExtractFile(file, fileSavePath);
            }
            return extractedCount;
        }

        public static int ExtractFile(string[] fileNames, string savePath)
        {
            int extractedCount = 0;
            for (var i = 0; i < fileNames.Length; i++)
            {
                var fileName = fileNames[i];
                extractedCount += ExtractFile(fileName, savePath);
            }
            return extractedCount;
        }

        public static int ExtractFile(string fileName, string savePath)
        {
            int extractedCount = 0;
            var reader = new FileReader(fileName);
            reader = reader.PreProcessing(Game);
            if (reader.FileType == FileType.BundleFile)
                extractedCount += ExtractBundleFile(reader, savePath);
            else if (reader.FileType == FileType.WebFile)
                extractedCount += ExtractWebDataFile(reader, savePath);
            else if (reader.FileType == FileType.BlkFile)
                extractedCount += ExtractBlkFile(reader, savePath);
            else if (reader.FileType == FileType.BlockFile)
                extractedCount += ExtractBlockFile(reader, savePath);
            else
                reader.Dispose();
            return extractedCount;
        }

        private static int ExtractBundleFile(FileReader reader, string savePath)
        {
            Logger.Info($"Decompressing {reader.FileName} ...");
            try
            {
                var bundleFile = new BundleFile(reader, Game);
                reader.Dispose();
                if (bundleFile.fileList.Length > 0)
                {
                    var extractPath = Path.Combine(savePath, reader.FileName + "_unpacked");
                    return ExtractStreamFile(extractPath, bundleFile.fileList);
                }
            }
            catch (InvalidCastException)
            {
                Logger.Error($"Game type mismatch, Expected {nameof(Mr0k)} but got {Game.Name} ({Game.GetType().Name}) !!");
            }
            return 0;
        }

        private static int ExtractWebDataFile(FileReader reader, string savePath)
        {
            Logger.Info($"Decompressing {reader.FileName} ...");
            var webFile = new WebFile(reader);
            reader.Dispose();
            if (webFile.fileList.Length > 0)
            {
                var extractPath = Path.Combine(savePath, reader.FileName + "_unpacked");
                return ExtractStreamFile(extractPath, webFile.fileList);
            }
            return 0;
        }

        private static int ExtractBlkFile(FileReader reader, string savePath)
        {
            int total = 0;
            Logger.Info($"Decompressing {reader.FileName} ...");
            try
            {
                using var stream = BlkUtils.Decrypt(reader, (Blk)Game);
                do
                {
                    stream.Offset = stream.RelativePosition;
                    var dummyPath = Path.Combine(reader.FullPath, stream.RelativePosition.ToString("X8"));
                    var subReader = new FileReader(dummyPath, stream, true);
                    var subSavePath = Path.Combine(savePath, reader.FileName + "_unpacked");
                    switch (subReader.FileType)
                    {
                        case FileType.BundleFile:
                            total += ExtractBundleFile(subReader, subSavePath);
                            break;
                        case FileType.Mhy0File:
                            total += ExtractMhy0File(subReader, subSavePath);
                            break;
                    }
                } while (stream.Remaining > 0);
            }
            catch (InvalidCastException)
            {
                Logger.Error($"Game type mismatch, Expected {nameof(Blk)} but got {Game.Name} ({Game.GetType().Name}) !!");
            }
            return total;
        }

        private static int ExtractBlockFile(FileReader reader, string savePath)
        {
            int total = 0;
            Logger.Info($"Decompressing {reader.FileName} ...");
            using var stream = new BlockStream(reader.BaseStream, 0);
            do
            {
                stream.Offset = stream.RelativePosition;
                var subSavePath = Path.Combine(savePath, reader.FileName + "_unpacked");
                var dummyPath = Path.Combine(reader.FullPath, stream.RelativePosition.ToString("X8"));
                var subReader = new FileReader(dummyPath, stream, true);
                total += ExtractBundleFile(subReader, subSavePath);
            } while (stream.Remaining > 0);
            return total;
        }

        private static int ExtractMhy0File(FileReader reader, string savePath)
        {
            Logger.Info($"Decompressing {reader.FileName} ...");
            try
            {
                var mhy0File = new Mhy0File(reader, reader.FullPath, (Mhy0)Game);
                reader.Dispose();
                if (mhy0File.fileList.Length > 0)
                {
                    var extractPath = Path.Combine(savePath, reader.FileName + "_unpacked");
                    return ExtractStreamFile(extractPath, mhy0File.fileList);
                }
            }
            catch (InvalidCastException)
            {
                Logger.Error($"Game type mismatch, Expected {nameof(Mhy0)} but got {Game.Name} ({Game.GetType().Name}) !!");
            }
            return 0;
        }

        private static int ExtractStreamFile(string extractPath, StreamFile[] fileList)
        {
            int extractedCount = 0;
            foreach (var file in fileList)
            {
                var filePath = Path.Combine(extractPath, file.path);
                var fileDirectory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(fileDirectory))
                {
                    Directory.CreateDirectory(fileDirectory);
                }
                if (!File.Exists(filePath))
                {
                    using (var fileStream = File.Create(filePath))
                    {
                        file.stream.CopyTo(fileStream);
                    }
                    extractedCount += 1;
                }
                file.stream.Dispose();
            }
            return extractedCount;
        }

        public static void UpdateContainers()
        {
            if (exportableAssets.Count > 0)
            {
                Logger.Info("Updating Containers...");
                foreach (var asset in exportableAssets)
                {
                    if (int.TryParse(asset.Container, out var value))
                    {
                        var last = unchecked((uint)value);
                        var name = Path.GetFileNameWithoutExtension(asset.SourceFile.originalPath);
                        if (uint.TryParse(name, out var id))
                        {
                            var path = ResourceIndex.GetContainer(id, last);
                            if (!string.IsNullOrEmpty(path))
                            {
                                asset.Container = path;
                                if (asset.Type == ClassIDType.MiHoYoBinData)
                                {
                                    asset.Text = Path.GetFileNameWithoutExtension(path);
                                }
                            }
                        }
                    }
                }
                Logger.Info("Updated !!");
            }
        }

        public static void BuildAssetData(Regex[] nameFilters, Regex[] containerFilters, ref int i)
        {
            var objectAssetItemDic = new Dictionary<Object, AssetItem>();
            var mihoyoBinDataNames = new List<(PPtr<Object>, string)>();
            var containers = new List<(PPtr<Object>, string)>();
            foreach (var assetsFile in assetsManager.assetsFileList)
            {
                foreach (var asset in assetsFile.Objects)
                {
                    ProcessAssetData(asset, nameFilters, objectAssetItemDic, mihoyoBinDataNames, containers, ref i);
                }
            }
            foreach ((var pptr, var name) in mihoyoBinDataNames)
            {
                if (pptr.TryGet<MiHoYoBinData>(out var obj))
                {
                    var assetItem = objectAssetItemDic[obj];
                    if (int.TryParse(name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash))
                    {
                        assetItem.Text = name;
                        assetItem.Container = hash.ToString();
                    }
                    else assetItem.Text = $"BinFile #{assetItem.m_PathID}";
                }
            }
            if (!SkipContainer)
            {
                foreach ((var pptr, var container) in containers)
                {
                    if (pptr.TryGet(out var obj))
                    {
                        if (!objectAssetItemDic.TryGetValue(obj, out var item))
                        {
                            ProcessAssetData(obj, nameFilters, objectAssetItemDic, mihoyoBinDataNames, containers, ref i);
                            item = objectAssetItemDic[obj];
                        }
                        if (containerFilters.IsNullOrEmpty() || containerFilters.Any(x => x.IsMatch(container)))
                        {
                            item.Container = container;
                        }
                        else
                        {
                            exportableAssets.Remove(item);
                        }
                    }
                }
                containers.Clear();
                if (Game.Type.IsGISubGroup())
                {
                    UpdateContainers();
                }
            }
        }

        public static void ProcessAssetData(Object asset, Regex[] nameFilters, Dictionary<Object, AssetItem> objectAssetItemDic, List<(PPtr<Object>, string)> mihoyoBinDataNames, List<(PPtr<Object>, string)> containers, ref int i) 
        {
            var assetItem = new AssetItem(asset);
            objectAssetItemDic.Add(asset, assetItem);
            assetItem.UniqueID = "#" + i++;
            var exportable = false;
            switch (asset)
            {
                case GameObject m_GameObject:
                    assetItem.Text = m_GameObject.m_Name;
                    exportable = ModelOnly && m_GameObject.HasModel();
                    break;
                case Texture2D m_Texture2D:
                    if (!string.IsNullOrEmpty(m_Texture2D.m_StreamData?.path))
                        assetItem.FullSize = asset.byteSize + m_Texture2D.m_StreamData.size;
                    assetItem.Text = m_Texture2D.m_Name;
                    exportable = !ModelOnly;
                    break;
                case AudioClip m_AudioClip:
                    if (!string.IsNullOrEmpty(m_AudioClip.m_Source))
                        assetItem.FullSize = asset.byteSize + m_AudioClip.m_Size;
                    assetItem.Text = m_AudioClip.m_Name;
                    exportable = !ModelOnly;
                    break;
                case VideoClip m_VideoClip:
                    if (!string.IsNullOrEmpty(m_VideoClip.m_OriginalPath))
                        assetItem.FullSize = asset.byteSize + (long)m_VideoClip.m_ExternalResources.m_Size;
                    assetItem.Text = m_VideoClip.m_Name;
                    exportable = !ModelOnly;
                    break;
                case Shader m_Shader:
                    assetItem.Text = m_Shader.m_ParsedForm?.m_Name ?? m_Shader.m_Name;
                    exportable = !ModelOnly;
                    break;
                case Mesh _:
                case TextAsset _:
                case AnimationClip _:
                case Font _:
                case Sprite _:
                case Material _:
                    assetItem.Text = ((NamedObject)asset).m_Name;
                    exportable = !ModelOnly;
                    break;
                case Animator m_Animator:
                    if (m_Animator.m_GameObject.TryGet(out var gameObject))
                    {
                        assetItem.Text = gameObject.m_Name;
                    }
                    exportable = !ModelOnly;
                    break;
                case MonoBehaviour m_MonoBehaviour:
                    if (m_MonoBehaviour.m_Name == "" && m_MonoBehaviour.m_Script.TryGet(out var m_Script))
                    {
                        assetItem.Text = m_Script.m_ClassName;
                    }
                    else
                    {
                        assetItem.Text = m_MonoBehaviour.m_Name;
                    }
                    exportable = !ModelOnly;
                    break;
                case AssetBundle m_AssetBundle:
                    foreach (var m_Container in m_AssetBundle.m_Container)
                    {
                        var preloadIndex = m_Container.Value.preloadIndex;
                        var preloadSize = m_Container.Value.preloadSize;
                        var preloadEnd = preloadIndex + preloadSize;
                        for (int k = preloadIndex; k < preloadEnd; k++)
                        {
                            containers.Add((m_AssetBundle.m_PreloadTable[k], m_Container.Key));
                        }
                    }
                    assetItem.Text = m_AssetBundle.m_Name;
                    break;
                case IndexObject m_IndexObject:
                    foreach (var index in m_IndexObject.AssetMap)
                    {
                        mihoyoBinDataNames.Add((index.Value.Object, index.Key));
                    }
                    assetItem.Text = "IndexObject";
                    break;
                case MiHoYoBinData m_MiHoYoBinData:
                    exportable = !ModelOnly;
                    break;
                case ResourceManager m_ResourceManager:
                    foreach (var m_Container in m_ResourceManager.m_Container)
                    {
                        containers.Add((m_Container.Value, m_Container.Key));
                    }
                    break;
                case NamedObject m_NamedObject:
                    assetItem.Text = m_NamedObject.m_Name;
                    break;
            }
            if (assetItem.Text == "")
            {
                assetItem.Text = assetItem.TypeString + assetItem.UniqueID;
            }
            var isMatchRegex = nameFilters.Length == 0 || nameFilters.Any(x => x.IsMatch(assetItem.Text));
            if (isMatchRegex && exportable)
            {
                exportableAssets.Add(assetItem);
            }
        }

        public static void ExportAssets(string savePath, List<AssetItem> toExportAssets, AssetGroupOption assetGroupOption)
        {
            int toExportCount = toExportAssets.Count;
            int exportedCount = 0;
            foreach (var asset in toExportAssets)
            {
                string exportPath;
                switch (assetGroupOption)
                {
                    case AssetGroupOption.ByType: //type name
                        exportPath = Path.Combine(savePath, asset.TypeString);
                        break;
                    case AssetGroupOption.ByContainer: //container path
                        if (!string.IsNullOrEmpty(asset.Container))
                        {
                            exportPath = Path.HasExtension(asset.Container) ? Path.Combine(savePath, Path.GetDirectoryName(asset.Container)) : Path.Combine(savePath, asset.Container);
                        }
                        else
                        {
                            exportPath = Path.Combine(savePath, asset.TypeString);
                        }
                        break;
                    case AssetGroupOption.BySource: //source file
                        if (string.IsNullOrEmpty(asset.SourceFile.originalPath))
                        {
                            exportPath = Path.Combine(savePath, asset.SourceFile.fileName + "_export");
                        }
                        else
                        {
                            exportPath = Path.Combine(savePath, Path.GetFileName(asset.SourceFile.originalPath) + "_export", asset.SourceFile.fileName);
                        }
                        break;
                    default:
                        exportPath = savePath;
                        break;
                }
                exportPath += Path.DirectorySeparatorChar;
                Logger.Info($"[{exportedCount}/{toExportCount}] Exporting {asset.TypeString}: {asset.Text}");
                try
                {
                    if (ExportConvertFile(asset, exportPath))
                    {
                        exportedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Export {asset.Type}:{asset.Text} error\r\n{ex.Message}\r\n{ex.StackTrace}");
                }
            }

            var statusText = exportedCount == 0 ? "Nothing exported." : $"Finished exporting {exportedCount} assets.";

            if (toExportCount > exportedCount)
            {
                statusText += $" {toExportCount - exportedCount} assets skipped (not extractable or files already exist)";
            }

            Logger.Info(statusText);
        }

        public static void ExportAssetsMap(string savePath, List<AssetEntry> toExportAssets, string exportListName, ExportListType exportListType)
        {
            string filename;
            switch (exportListType)
            {
                case ExportListType.XML:
                    filename = Path.Combine(savePath, $"{exportListName}.xml");
                    var settings = new XmlWriterSettings() { Indent = true };
                    using (XmlWriter writer = XmlWriter.Create(filename, settings))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("Assets");
                        writer.WriteAttributeString("filename", filename);
                        writer.WriteAttributeString("createdAt", DateTime.UtcNow.ToString("s"));
                        foreach (var asset in toExportAssets)
                        {
                            writer.WriteStartElement("Asset");
                            writer.WriteElementString("Name", asset.Name);
                            writer.WriteElementString("Container", asset.Container);
                            writer.WriteStartElement("Type");
                            writer.WriteAttributeString("id", ((int)asset.Type).ToString());
                            writer.WriteValue(asset.Type.ToString());
                            writer.WriteEndElement();
                            writer.WriteElementString("PathID", asset.PathID.ToString());
                            writer.WriteElementString("Source", asset.Source);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                    break;
                case ExportListType.JSON:
                    filename = Path.Combine(savePath, $"{exportListName}.json");
                    using (StreamWriter file = File.CreateText(filename))
                    {
                        JsonSerializer serializer = new JsonSerializer() { Formatting = Newtonsoft.Json.Formatting.Indented };
                        serializer.Converters.Add(new StringEnumConverter());
                        serializer.Serialize(file, toExportAssets);
                    }
                    break;
            }

            var statusText = $"Finished exporting asset list with {toExportAssets.Count()} items.";

            Logger.Info(statusText);

            Logger.Info($"AssetMap build successfully !!");
        }

        public static TypeTree MonoBehaviourToTypeTree(MonoBehaviour m_MonoBehaviour)
        {
            return m_MonoBehaviour.ConvertToTypeTree(assemblyLoader);
        }
    }
}
