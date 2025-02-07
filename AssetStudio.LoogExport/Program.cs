
using AssetStudio;
using AssetStudio.CLI;
using System.Collections.Generic;
using System;
using System.IO;
using static AssetStudio.CLI.Exporter;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

string ExportDir = "D:\\genshin\\Exports";
string SourceGamePath = "C:\\Program Files\\miHoYo Launcher\\games\\Genshin Impact Game\\YuanShen_Data";
//string SourceGamePath = "C:\\Program Files\\miHoYo Launcher\\games\\Genshin Impact Game\\YuanShen_Data\\StreamingAssets\\AssetBundles\\blocks";

var assetMapFilePath = "D:\\genshin\\genshin.map"; // object与文件的对应关系
var cabMapFilePath = "D:\\genshin\\genshin.bin"; // 资产的依赖关系

GameType gameType = GameType.GI;
Studio.Game = new Game(gameType);

// 1. load asset map
Console.WriteLine($"Loading AssetMap...");
ResourceMap.FromFile(assetMapFilePath);
AssetsHelper.LoadCABMap(cabMapFilePath);
AssetsManager assetsManager = new AssetsManager();
assetsManager.SpecifyUnityVersion = "";
assetsManager.Game = Studio.Game;
assetsManager.ResolveDependencies = true;
assetsManager.SkipProcess = false;
assetsManager.Silent = false;


Console.WriteLine("Start Export...");

Console.WriteLine("Scanning for files...");
var files = Directory.GetFiles(SourceGamePath, "*.blk", SearchOption.AllDirectories);
{

    var xiangling = "C:\\Program Files\\miHoYo Launcher\\games\\Genshin Impact Game\\YuanShen_Data\\StreamingAssets\\AssetBundles\\blocks\\00\\09499318.blk";
    List<string> dd = new();
    dd.Add(xiangling);
    files = dd.ToArray();
}
Console.WriteLine($"Found {files.Length} files");


HashSet<string> exportedFiles = new();// cab path
HashSet<string> exportedAbsFiles = new();// abs path
Dictionary<long, string> pathObjects = new();//path id->entries
List<KeyValuePair<long, string>> dupPathIDs = new();


{
    HashSet<long> objectIdentity = new();//type->entries
    for (int i = 0; i < files.Length; i++)
    {
        Console.WriteLine("BuildAssetData: {0} / {1}", i + 1, files.Length);

        if (exportedAbsFiles.Contains(files[i])) 
        { 
            continue; 
        }

        assetsManager.LoadFiles(files[i]);
        if (assetsManager.assetsFileList.Count > 0)
        {
            foreach (var item in assetsManager.assetsFileList)
            {
                if (!exportedFiles.Add(item.fileName))
                {
                    continue;
                }
                exportedAbsFiles.Add(item.originalPath);
                
                // find top most father
                bool bHasTransformComponent = false;
                bool bHasGameObject = false;
                bool bHasAnimatorController = false;
                bool bHasScriptOrScriptCom = false;
                bool bNamedObject = false;
                bool bHasTexture = false;
                bool bHasShader = false;
                bool bHasTextAsset = false;
                bool bHasAnimSequence = false;
                bool bHasMesh = false;
                List<Mesh> meshList = new List<Mesh>();
                List<AnimationClip> clipList = new List<AnimationClip>();
                bool bHasMaterial = false;
                bool bHasRenderer = false;
                bool bHasAnimation = false;

                List<GameObject> TopMostFathers = new();
                Avatar AvatarObj = null;

                foreach (var obj in item.Objects)
                {
                    switch (obj)
                    {
                        case Avatar avata:
                            AvatarObj = avata;
                            break;
                        case MeshRenderer mr:
                            if (mr.m_AdditionalVertexStreams.TryGet(out var mesh))
                            {
                                bHasRenderer = true;
                            }
                            break;
                        case SkinnedMeshRenderer smr:
                            if (smr.m_Mesh.TryGet(out var sm))
                            {
                                bHasRenderer = true;
                            }
                            break;
                        case MeshFilter mf:
                            if (mf.m_Mesh.TryGet(out var meshf))
                            {
                                bHasRenderer = true;
                            }
                            break;
                        case Animator animator:
                            animator.m_Avatar.TryGet(out AvatarObj);
                            bHasAnimation = true;
                            break;
                        case Animation:
                            bHasAnimation = true;
                            break;
                        case Transform transformCom:
                            bHasTransformComponent = true;
                            transformCom.m_GameObject.TryGet(out var gameObject);
                            if (gameObject == null)
                            {
                                break;
                            }
                            gameObject.m_Transform.m_Father.TryGet(out var father);
                            var currentTopMostTransform = gameObject.m_Transform;
                            while (father != null)
                            {
                                currentTopMostTransform = father;
                                father.m_Father.TryGet(out father);
                            }
                            if (currentTopMostTransform != null)
                            {
                                if (currentTopMostTransform.m_GameObject.TryGet(out var currentTopMost))
                                {
                                    bool bHasTopMostFather = false;
                                    foreach (var topMostFather in TopMostFathers)
                                    {
                                        if (topMostFather.m_PathID == currentTopMost.m_PathID)
                                        {
                                            bHasTopMostFather = true;
                                            break;
                                        }
                                    }
                                    if (!bHasTopMostFather)
                                    {
                                        TopMostFathers.Add(currentTopMost);
                                    }
                                }
                            }
                            break;
                        case GameObject go:
                            bHasGameObject = true;
                            break;
                        case AssetBundle ab:
                            break;
                        case Texture t:
                            bHasTexture = true;
                            break;
                        case MonoScript ms:
                        case MonoBehaviour mb:
                            bHasScriptOrScriptCom = true;
                            break;
                        case AnimatorController ac:
                            bHasAnimatorController = true;
                            break;
                        case Shader s:
                        case ShaderVariantCollection svc:
                            bHasShader = true;
                            break;
                        case TextAsset:
                            bHasTextAsset = true;
                            break;
                        case AnimationClip ac:
                            clipList.Add(ac);
                            bHasAnimSequence = true;
                            break;
                        case Mesh msh:
                            meshList.Add(msh);
                            bHasMesh = true;
                            break;
                        case Material:
                            bHasMaterial = true;
                            break;
                        case NamedObject:
                            bNamedObject = true;
                            break;
                        default:
                            break;
                    }
                }
                if (bHasGameObject && TopMostFathers.Count == 0)
                {
                    continue;
                }

                var exportPath = Path.Combine(ExportDir, item.fileName);
                var exportFullPath = exportPath + Path.DirectorySeparatorChar;
                
                if (bHasTransformComponent && bHasAnimation)
                {
                    if (!TopMostFathers[0].Name.Contains("Avatar_Girl_Pole_XianglingCostumeWinter", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    // export to FBX file
                    if (TopMostFathers.Count > 1)
                    {
                        exportFullPath = exportFullPath + FixFileName(item.fileName);
                        ExportMergeGameObjectToFbx(item.fileName, TopMostFathers, exportFullPath, clipList, meshList);
                    }
                    else
                    {
                        ExportRootGameObjectToFbx(TopMostFathers[0], exportFullPath, clipList, meshList, AvatarObj);
                    }
                }
                continue;
                if (bHasTransformComponent)
                {
                    if (!bHasAnimation)
                    {
                        continue;
                    }
                    if (bHasRenderer)
                    {
                        // export to FBX file
                        if (TopMostFathers.Count > 1)
                        {
                            exportFullPath = exportFullPath + FixFileName(item.fileName);
                            ExportMergeGameObjectToFbx(item.fileName, TopMostFathers, exportFullPath, clipList, meshList);
                        }
                        else
                        {
                            if (!TopMostFathers[0].Name.Contains("XiangLin", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            ExportRootGameObjectToFbx(TopMostFathers[0], exportFullPath, clipList, meshList, AvatarObj);
                        }
                    }
                    else
                    {
                        if (TopMostFathers.Count > 1)
                        {
                            exportFullPath = exportFullPath + FixFileName(item.fileName);
                        }
                        else
                        {
                            ExportPrefab(TopMostFathers[0], exportFullPath);
                        }
                        //var bPrefab = true;
                    }
                }
                else if (bHasAnimatorController)
                {
                    var bAnimator = true;//状态机文件
                }
                else if (bHasScriptOrScriptCom) { 
                    var bScript = true;//纯脚本文件
                }
                else if (bHasTexture)
                {
                    var bTexture = true;//纯贴图文件
                }
                else if (bHasShader)
                {
                    var bShader = true; // Shader文件
                }
                else if (bHasTextAsset)
                {
                    var bText = true;//纯文本文件
                }
                else if (bHasAnimSequence)
                {
                    //动画序列文件
                    //exportFullPath = exportFullPath + FixFileName(item.fileName);
                    //ExportAnimSequenceToFbx(clipList, exportFullPath);
                }
                else if (bHasMesh)
                {
                    var bMesh = true; // 纯网格文件？
                }
                else if (bHasMaterial)
                {
                    var bMaterial = true; // 纯材质文件
                }
                else if (bNamedObject)
                {
                    var bPureAsset = true;
                }
                else
                {
                    var bUnkow = true;
                }



                //var toExportAssets = new List<AssetItem>();
                //BuildAssetData(toExportAssets, item);
                //ExportAssets(ExportDir, toExportAssets, AssetGroupOption.ByFileName, ExportType.Convert, SourceGamePath);
                
                //foreach (var obj in toExportAssets)
                //{
                //    if (!pathObjects.TryAdd(obj.m_PathID, obj.ExportFileName))
                //    {
                //        dupPathIDs.Add(new KeyValuePair<long, string>(obj.m_PathID, obj.ExportFileName));
                //    }
                //}
            }
        }
        assetsManager.Clear();
    }
}

Console.WriteLine($"pathObjects count:{0}", pathObjects.Count);

{ // write pathid map
    var settings = new JsonSerializerSettings();
    settings.Converters.Add(new StringEnumConverter());
    var str = JsonConvert.SerializeObject(pathObjects, Formatting.Indented, settings);
    var exportFullPath = Path.Combine(ExportDir, "PathIDMap.json");
    File.WriteAllText(exportFullPath, str);
}
{
    var settings = new JsonSerializerSettings();
    settings.Converters.Add(new StringEnumConverter());
    var str = JsonConvert.SerializeObject(dupPathIDs, Formatting.Indented, settings);
    var exportFullPath = Path.Combine(ExportDir, "ErrorPathID.json");
    File.WriteAllText(exportFullPath, str);
}

return;


#if tt

// 2. get export entries
//List<string> files = new List<string>();
//var  = new List<string>(entries.Select(x => x.Source).ToHashSet());
var entries = ResourceMap.GetEntries();
//var regex = new Regex("XiangLin", RegexOptions.IgnoreCase);
List<AssetEntry> pendingExportEntries = new List<AssetEntry>();


// 检查所有包含 SkinnedMeshRenderer 的 GameObject
Console.WriteLine("检查一下那些source file包含game object");
Console.WriteLine("");

var gameObjectSourceFiles = new HashSet<string>();
var sourcePath = entries.AsParallel().Where(x => x.Type == ClassIDType.GameObject).Select(x => x.Source).Distinct().ToList();
for (int i = 0; i < sourcePath.Count; i++)
{
    Console.SetCursorPosition(0, Console.CursorTop - 1);
    Console.WriteLine("Filter GameObject Asset: {0} / {1}", i + 1, sourcePath.Count);
    gameObjectSourceFiles.Add(sourcePath[i]);
}

Console.WriteLine("读入资产文件,并解析资产，看看是否有SkinnedMeshRenderer");
Console.WriteLine("");

{
    int fileIndex = 0;
    foreach (var file in gameObjectSourceFiles)
    {
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.WriteLine("Read Asset: {0} / {1} , {2}", fileIndex + 1, gameObjectSourceFiles.Count, file);
        fileIndex++;

        assetsManager.LoadFiles(file);

        if (assetsManager.assetsFileList.Count > 0)
        {
            foreach (var item in assetsManager.assetsFileList)
            {
                bool bHasSkinnedMesh = false;
                foreach (var obj in item.Objects)
                {
                    if (obj.type == ClassIDType.PrefabImporter || obj.type == ClassIDType.PrefabInstance)
                    {
                        var gameObject = obj as GameObject;
                    }
                    if (obj.type == ClassIDType.GameObject)
                    {
                        var gameObject = obj as GameObject;
                        
                        if (gameObject.m_SkinnedMeshRenderer != null)
                        {
                            bHasSkinnedMesh = true;
                        }
                    }
                }
                if (bHasSkinnedMesh)
                {
                    var toExportAssets = new List<AssetItem>();
                    BuildAssetData(toExportAssets, item);
                    ExportAssets(ExportDir, toExportAssets, AssetGroupOption.ByFileName, ExportType.JSON, SourceGamePath);
                }
            }
        }
        assetsManager.Clear();
        break;
    }
}

return;

Dictionary<string, int> exportFiles = new(); // uid -> index
var exportItems = new List<AssetItem>();

for (int i = 0; i < pendingExportEntries.Count; i++)
{
    Console.WriteLine("BuildAssetData: {0} / {1}", i + 1, pendingExportEntries.Count);

    var toExportAssets = new List<AssetItem>();

    var file = pendingExportEntries[i].Source;
    assetsManager.LoadFiles(file);
    if (assetsManager.assetsFileList.Count > 0)
    {
        //BuildAssetData(toExportAssets);
    }

    foreach (var item in toExportAssets)
    {
        var uid = string.Format("{0}{1}", item.m_PathID, item.SourceFile.fileName);
        bool bHasUid = exportFiles.ContainsKey(uid);
        if (bHasUid)
        {
            // path id 和 filename 都相同的情况下，认为是同一个资源
            continue;
        }
        exportFiles.Add(uid, exportItems.Count);
        exportItems.Add(item);
    }
    toExportAssets.Clear();
    assetsManager.Clear();

    if (i > 1)
    {
        break;
    }
}

#if TestFileIDAndFileName
var exportAssetCount = 0;
Dictionary<string, int> exportFiles = new(); // uid -> index
Dictionary<string, long> exportPathID = new(); // filename -> path id
var exportItems = new List<AssetItem>();

for (int i = 0; i < pendingExportEntries.Count; i++)
{
    Console.WriteLine("BuildAssetData: {0} / {1}", i + 1, pendingExportEntries.Count);

    var toExportAssets = new List<AssetItem>();

    var file = pendingExportEntries[i].Source;
    assetsManager.LoadFiles(file);
    if (assetsManager.assetsFileList.Count > 0)
    {
        BuildAssetData(toExportAssets);
    }
    exportItems.AddRange(toExportAssets);

    for (int assetIndex = 0; assetIndex < toExportAssets.Count; assetIndex++)
    {
        var item = toExportAssets[assetIndex];

        var uid = string.Format("{0}{1}", item.m_PathID, item.SourceFile.fileName);
        bool bHasUid = exportFiles.ContainsKey(uid);
        if (bHasUid)
        {
            // path id 和 filename 都相同的情况下，认为是同一个资源
            continue;
        }
        bool bHasPathID = exportPathID.ContainsKey(item.SourceFile.fileName);
        if (bHasPathID)
        {
            // path id不同，但是filename相同
            var lastPathID = exportPathID[item.SourceFile.fileName];
            var lastUid = string.Format("{0}{1}", lastPathID, item.SourceFile.fileName);
            var lastItem = exportItems[exportFiles[lastUid]];

            Console.WriteLine("error!");
        }
        exportFiles.Add(uid, assetIndex + exportAssetCount);
        exportPathID.Add(item.SourceFile.fileName, item.m_PathID);
    }

    exportAssetCount += toExportAssets.Count;

    toExportAssets.Clear();
    assetsManager.Clear();


    if (i > 10)
    {
        break;
    }
}
#endif

#if CheckUid
var exportAssetCount = 0;
Dictionary<string, int> exportFiles = new();
var exportItems = new List<AssetItem>();


for (int i = 0; i < pendingExportEntries.Count; i++)
{
    Console.WriteLine("BuildAssetData: {0} / {1}", i + 1, pendingExportEntries.Count);

    var toExportAssets = new List<AssetItem>();

    var file = pendingExportEntries[i].Source;
    assetsManager.LoadFiles(file);
    if (assetsManager.assetsFileList.Count > 0)
    {
        BuildAssetData(toExportAssets);
    }
    exportItems.AddRange(toExportAssets);

    for (int assetIndex = 0; assetIndex < toExportAssets.Count; assetIndex++)
    {
        var item = toExportAssets[assetIndex];

        var uid = string.Format("{0}{1}", item.m_PathID, item.SourceFile.fileName);
        bool bHasUid = exportFiles.ContainsKey(uid);
        if (bHasUid)
        {
            var lastItem = exportItems[exportFiles[uid]];
            if (lastItem != item)
            {
                Console.WriteLine("error!");
            }
            continue;
        }
        exportFiles.Add(uid, assetIndex + exportAssetCount);
    }
    /*
    foreach (var item in toExportAssets)
    {
        switch (item.Asset)
        {
            case AnimationClip animationClip:
                {
                    var str = animationClip.Convert();
                    Console.WriteLine(str);
                }
                break;
            case Material material:
                { 
                    Console.WriteLine(material.ToString());
                } break;
            case AssetBundle assetBundle:
                {
                    
                }
                break;
            case MonoBehaviour monoBehaviour:
                {
                    //monoBehaviour.m_GameObject;
                }
                break;
            default:
                break;
        }
    }
    */

    exportAssetCount += toExportAssets.Count;

    toExportAssets.Clear();
    assetsManager.Clear();


    if (i > 10)
    {
        break;
    }
}
#endif

Console.WriteLine("Export Asset Count {0}", exportItems.Count);

// 3. filter
List<AssetItem> filteredItems = new List<AssetItem>();
Dictionary<ClassIDType, int> typeCounter = new();
foreach (var item in exportItems) 
{
    //if (item.Type == ClassIDType.MeshRenderer || item.Type == ClassIDType.SkinnedMeshRenderer)
    //{
    //    filteredItems.Add(item);
    //    break;
    //}
    if (typeCounter.ContainsKey(item.Type) == false)
    {
        typeCounter.Add(item.Type, 0);
    }
    typeCounter[item.Type]++;
}

ExportAssets(ExportDir, exportItems, AssetGroupOption.ByType, ExportType.JSON);



Console.WriteLine("Export Success...");


#endif