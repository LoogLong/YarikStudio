
using AssetStudio;
using AssetStudio.CLI;
using System.Collections.Generic;
using System;
using System.IO;
using static AssetStudio.CLI.Exporter;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Linq;
using Object = AssetStudio.Object;
using System.Diagnostics.Metrics;

internal class Program
{
    private static void ExportZZZ()
    {
        string ExportDir = "D:\\miHoYoExport\\ZenlessZoneZero";
        string SourceGamePath = "D:\\ZenlessZoneZero Game\\ZenlessZoneZero_Data";

        var assetMapFilePath = "D:\\miHoYoExport\\ZenlessZoneZero\\zzz.map"; // object与文件的对应关系
        var cabMapFilePath = "D:\\miHoYoExport\\ZenlessZoneZero\\zzz.bin"; // 资产的依赖关系

        GameType gameType = GameType.ZZZ;
        Studio.Game = GameManager.GetGame(gameType);
        // build asset map
        if (false)
        {
            var name = "zzz";
            var version = "";
            var exportListType = ExportListType.MessagePack;

            Logger.Info("Scanning for files...");
            var files = Directory.GetFiles(SourceGamePath, "*.*", SearchOption.AllDirectories).ToArray();
            Logger.Info($"Found {files.Length} files");
            AssetsHelper.SetUnityVersion(version);
            var task = AssetsHelper.BuildBoth(files, name, SourceGamePath, Studio.Game, ExportDir, exportListType);
            task.Wait();
            return;
        }

        // 1. load asset map
        Console.WriteLine($"Loading AssetMap...");
        ResourceMap.FromFile(assetMapFilePath);
        AssetsHelper.LoadCABMap(cabMapFilePath);

        bool bExportList = false;
        if (bExportList)
        {
            var entries = ResourceMap.GetEntries();
            //{
            //    var animEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.AnimationClip);
            //    var animFileNames = animEntries.Select(x => x.Name).Distinct().ToList();
            //    Console.WriteLine("ErrorAnims count:{0}", animFileNames.Count);
            //    var settings = new JsonSerializerSettings();
            //    settings.Converters.Add(new StringEnumConverter());
            //    var str = JsonConvert.SerializeObject(animFileNames, Formatting.Indented, settings);
            //    var exportFullPath = Path.Combine(ExportDir, "animFileNames.json");
            //    File.WriteAllText(exportFullPath, str);
            //}
            //{
            //    var gameObjectEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.GameObject);
            //    var gameObjectFileNames = gameObjectEntries.Select(x => x.Name).Distinct().ToList();
            //    Console.WriteLine("ErrorAnims count:{0}", gameObjectFileNames.Count);
            //    var settings1 = new JsonSerializerSettings();
            //    settings1.Converters.Add(new StringEnumConverter());
            //    var str = JsonConvert.SerializeObject(gameObjectFileNames, Formatting.Indented, settings1);
            //    var exportFullPath = Path.Combine(ExportDir, "gameObjectFileNames.json");
            //    File.WriteAllText(exportFullPath, str);
            //}
            var meshEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.Avatar);
            var typeNames = entries.Select(x => x.Type).Distinct().ToList();
            Dictionary<long, string> meshNames = new Dictionary<long, string>();
            List<string> meshNamesList = new List<string>();
            List<long> meshPathList = new List<long>();
            foreach ( var entry in entries)
            {
                if (!meshNames.TryAdd(entry.PathID, entry.Name))
                {
                    meshNamesList.Add(entry.Name);
                    meshPathList.Add(entry.PathID);
                }
            }
            var pp = meshEntries.ToList();
            Console.WriteLine("entries count:{0}", pp.Count);
            var settings1 = new JsonSerializerSettings();
            settings1.Converters.Add(new StringEnumConverter());
            var str = JsonConvert.SerializeObject(pp, Formatting.Indented, settings1);
            var exportFullPath = Path.Combine(ExportDir, "avatar.json");
            File.WriteAllText(exportFullPath, str);
            return;
        }

        bool bPrintAvatars = false;
        if (bPrintAvatars)
        {
            var entries = ResourceMap.GetEntries();
            var avatarEnties = entries.AsParallel().Where(x => x.Type == ClassIDType.Avatar);
            var sourcePath = avatarEnties.Select(x => x.Source).Distinct().ToList();

            var files = sourcePath.ToArray();

            var fileCount = files.Length;
            var fileIndex = 1;

            HashSet<string> avatarNames = new();
            foreach (var file in files)
            {
                Console.WriteLine("Process Asset File: {0} / {1}", fileIndex++, fileCount);

                AssetsManager assetsManager = new AssetsManager();
                assetsManager.SpecifyUnityVersion = "";
                assetsManager.Game = Studio.Game;
                assetsManager.ResolveDependencies = false;
                assetsManager.SkipProcess = false;
                assetsManager.Silent = true;
                assetsManager.LoadFiles(file);

                Console.WriteLine("\tReading Asset......");

                if (assetsManager.assetsFileList.Count > 0)
                {
                    foreach (var item in assetsManager.assetsFileList)
                    {
                        foreach (var obj in item.Objects)
                        {
                            switch (obj)
                            {
                                case Avatar avatar:
                                    avatarNames.Add(obj.Name);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                assetsManager.Clear();
            }
            var bExport = true;
            if (bExport && avatarNames.Count > 0)
            {
                Console.WriteLine("avatarNames count:{0}", avatarNames.Count);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                var str = JsonConvert.SerializeObject(avatarNames, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "avatarNames.json");
                File.WriteAllText(exportFullPath, str);
            }
        }

        // first animation asset pass
        bool bAnimationPass = false;
        if (bAnimationPass)
        {
            Dictionary<long, string> GlobalSuccessAnims = new();

            Console.WriteLine("Start Export Animations...");
            var files = Directory.GetFiles(SourceGamePath, "*.blk", SearchOption.AllDirectories);
            {
                var entries = ResourceMap.GetEntries();

                List<AssetEntry> pendingExportEntries = new List<AssetEntry>();

                var animEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.AnimationClip);
                

                var heroAnimEntries = animEntries.Where(x => x.Name.StartsWith("Avatar_")).Distinct().ToList();
                var sourcePath = heroAnimEntries.Select(x => x.Source).Distinct().ToList();

                files = sourcePath.ToArray();
            }
            Console.WriteLine($"Found {files.Length} blk files");

            Dictionary<long, string> ErrorAnims = new();
            Queue<string> IgnoreAnims = new();

            var fileCount = files.Length;
            var fileIndex = 1;
            var p = "D:\\ZenlessZoneZero Game\\ZenlessZoneZero_Data\\StreamingAssets\\Blocks\\2839198260.blk";//anim
            var p2 = "D:\\ZenlessZoneZero Game\\ZenlessZoneZero_Data\\StreamingAssets\\Blocks\\29307005.blk";// avatar

            files = new[] { p, p2 };
            foreach (var file in files)
            {
                Console.WriteLine("Process Asset File: {0} / {1}", fileIndex++, fileCount);

                AssetsManager assetsManager = new AssetsManager();
                assetsManager.SpecifyUnityVersion = "";
                assetsManager.Game = Studio.Game;
                assetsManager.ResolveDependencies = false;
                assetsManager.SkipProcess = false;
                assetsManager.Silent = true;
                assetsManager.LoadFiles(files);
                var relativePath = Path.GetRelativePath(SourceGamePath, file);

                Console.WriteLine("\tReading Asset......");
                Dictionary<string, AnimationClip> PendingAnims = new();


                if (assetsManager.assetsFileList.Count > 0)
                {
                    List<Avatar> avatars = new List<Avatar>();
                    List<Animator> animators = new List<Animator>();
                    List<Animation> animations = new List<Animation>();

                    foreach (var item in assetsManager.assetsFileList)
                    {
                        foreach (var obj in item.Objects)
                        {
                            switch (obj)
                            {
                                case AnimationClip ac:
                                    {
                                        if (!ac.m_Name.Contains("Avatar_"))// 过滤 只导出角色的
                                        {
                                            IgnoreAnims.Enqueue(ac.m_Name);
                                            continue;
                                        }
                                        var exportFileName = Path.Combine(ExportDir, relativePath, item.fileName, ac.m_Name + ".anim");
                                        if (File.Exists(exportFileName))
                                        {
                                            ErrorAnims.TryAdd(ac.m_PathID, exportFileName);
                                            continue;
                                        }
                                        PendingAnims.TryAdd(exportFileName, ac);
                                        GlobalSuccessAnims.TryAdd(ac.m_PathID, exportFileName);
                                    }
                                    break;
                                case Avatar avatar:
                                    avatars.Add(avatar);
                                    break;
                                case Animator animator:
                                    animators.Add(animator);
                                    break;
                                case Animation animation:
                                    animations.Add(animation);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    var bExport = true;
                    if (bExport && PendingAnims.Count > 0)
                    {
                        Console.WriteLine("\tExport {0} Assets.....", PendingAnims.Count);

                        // find tos
                        var tos = new Dictionary<uint, string>() { { 0, string.Empty } };
                        if (avatars.Count > 0)
                        {
                            foreach (var avatar in avatars)
                            {
                                foreach (var item in avatar.m_TOS)
                                {
                                    tos.TryAdd(item.Key, item.Value);
                                }
                            }
                        }

                        PendingAnims.AsParallel().ForAll((item) =>
                        {
                            var exportFileName = item.Key;
                            var ac = item.Value;

                            ac.m_TOS = tos;

                            Directory.CreateDirectory(Path.GetDirectoryName(exportFileName));
                            var str = ac.Convert();
                            if (string.IsNullOrEmpty(str))
                            {
                                return;
                            }
                            File.WriteAllText(exportFileName, str);
                        });
                    }
                }
                assetsManager.Clear();
            }

            {
                Console.WriteLine("IgnoreAnims count:{0}", IgnoreAnims.Count);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                var str = JsonConvert.SerializeObject(IgnoreAnims, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "IgnoreAnims.json");
                File.WriteAllText(exportFullPath, str);
            }

            {
                Console.WriteLine("ErrorAnims count:{0}", ErrorAnims.Count);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                var str = JsonConvert.SerializeObject(ErrorAnims, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "ErrorAnims.json");
                File.WriteAllText(exportFullPath, str);
            }
            {
                Console.WriteLine("SuccessAnims count:{0}", GlobalSuccessAnims.Count);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                var str = JsonConvert.SerializeObject(GlobalSuccessAnims, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "SuccessAnims.json");
                File.WriteAllText(exportFullPath, str);
            }
        }



        // model pass
        var bModelPass = true;
        if (bModelPass)
        {
            Console.WriteLine("Start Export Models...");

            var entries = ResourceMap.GetEntries();

            // find mesh objects
            Dictionary<string, Mesh> heroSeparateMeshObjects = new(); // mesh name : mesh object
            if(true)
            {
                Console.WriteLine("Find Mesh Objects ...");
                var meshEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.Mesh);
                var heroSeparateMeshes = meshEntries.Where(x => x.Name.StartsWith("SeparateMesh_Avatar_"));
                var heroSeparateMeshesList = heroSeparateMeshes.Select(x => x.Source).Distinct().ToArray();
                AssetsManager meshAssetsManager = new AssetsManager();
                meshAssetsManager.SpecifyUnityVersion = "";
                meshAssetsManager.Game = Studio.Game;
                meshAssetsManager.ResolveDependencies = false;
                meshAssetsManager.SkipProcess = false;
                meshAssetsManager.Silent = true;
                meshAssetsManager.LoadFiles(heroSeparateMeshesList);

                if (meshAssetsManager.assetsFileList.Count > 0)
                {
                    var assetsCount = meshAssetsManager.assetsFileList.Count;
                    var assetIndex = 1;
                    foreach (var item in meshAssetsManager.assetsFileList)
                    {
                        Console.WriteLine("Process mesh files: {0} / {1}", assetIndex++, assetsCount);
                        foreach (var obj in item.Objects)
                        {
                            switch (obj)
                            {
                                case Mesh mesh:
                                    {
                                        if (!mesh.m_Name.StartsWith("SeparateMesh_Avatar_"))// 过滤 只导出角色的
                                        {
                                            continue;
                                        }
                                        heroSeparateMeshObjects.TryAdd(mesh.m_Name, mesh);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
                meshAssetsManager.Clear();
                // debug output
                //var settings1 = new JsonSerializerSettings();
                //settings1.Converters.Add(new StringEnumConverter());
                //var heroSeparateMeshNames = heroSeparateMeshObjects.Select(x => x.Key).ToList();
                //var str = JsonConvert.SerializeObject(heroSeparateMeshNames, Formatting.Indented, settings1);
                //var exportFullPath = Path.Combine(ExportDir, "heroSeparateMeshNames.json");
                //File.WriteAllText(exportFullPath, str);
            }

            Dictionary<string, RuntimeAnimatorController> controllerObjects = new(); // name : controller object
            AssetsManager controllerAssetsManager = new AssetsManager();
            if (false)
            {
                Console.WriteLine("Find Controller Objects ...");
                var controllerEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.AnimatorController || x.Type == ClassIDType.AnimatorOverrideController);
                
                var controllerSourceList = controllerEntries.Select(x => x.Source).Distinct().ToArray();

                controllerAssetsManager.SpecifyUnityVersion = "";
                controllerAssetsManager.Game = Studio.Game;
                controllerAssetsManager.ResolveDependencies = true;
                controllerAssetsManager.SkipProcess = false;
                controllerAssetsManager.Silent = false;
                controllerAssetsManager.LoadFiles(controllerSourceList);
                if (controllerAssetsManager.assetsFileList.Count > 0)
                {
                    var assetsCount = controllerAssetsManager.assetsFileList.Count;
                    var assetIndex = 1;
                    
                    foreach (var item in controllerAssetsManager.assetsFileList)
                    {
                        Console.WriteLine("Process controller files: {0} / {1}", assetIndex++, assetsCount);
                        foreach (var obj in item.Objects)
                        {
                            switch (obj)
                            {
                                case AnimatorController controller:
                                    {
                                        controllerObjects.TryAdd(controller.m_Name, controller);
                                        Dictionary<long, string> animationClips = new();
                                        foreach (var pptr in controller.m_AnimationClips)
                                        {
                                            if (pptr.TryGet(out var animationClip))
                                            {
                                                animationClips.TryAdd(animationClip.m_PathID, animationClip.Name);
                                            }
                                            else
                                            {
                                                animationClips.TryAdd(pptr.m_PathID, pptr.assetsFile.fileName);
                                            }
                                        }

                                        var settings1 = new JsonSerializerSettings();
                                        settings1.Converters.Add(new StringEnumConverter());
                                        var str = JsonConvert.SerializeObject(animationClips, Formatting.Indented, settings1);
                                        var exportFullPath = Path.Combine(ExportDir, "AnimatorController", item.fileName, controller.m_Name + ".controller");
                                        if (File.Exists(exportFullPath))
                                        {
                                            continue;
                                        }
                                        Directory.CreateDirectory(Path.GetDirectoryName(exportFullPath));
                                        File.WriteAllText(exportFullPath, str);
                                    }
                                    break;
                                case AnimatorOverrideController overrideController:
                                    {
                                        if (overrideController.m_Controller.TryGet<AnimatorController>(out var sourceAnimatorController))
                                        {
                                            Dictionary<long, string> animationClips = new();
                                            foreach (var pptr in sourceAnimatorController.m_AnimationClips)
                                            {
                                                if (pptr.TryGet(out var animationClip))
                                                {
                                                    animationClips.TryAdd(animationClip.m_PathID, animationClip.Name);
                                                }
                                                else
                                                {
                                                    animationClips.TryAdd(pptr.m_PathID, pptr.assetsFile.fileName);
                                                }
                                            }
                                            var settings1 = new JsonSerializerSettings();
                                            settings1.Converters.Add(new StringEnumConverter());
                                            var str = JsonConvert.SerializeObject(animationClips, Formatting.Indented, settings1);
                                            var exportFullPath = Path.Combine(ExportDir, "AnimatorController", item.fileName, overrideController.m_Name + ".controller");
                                            if (File.Exists(exportFullPath))
                                            {
                                                continue;
                                            }
                                            Directory.CreateDirectory(Path.GetDirectoryName(exportFullPath));
                                            File.WriteAllText(exportFullPath, str);
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                // debug output
                //var settings1 = new JsonSerializerSettings();
                //settings1.Converters.Add(new StringEnumConverter());
                //var controllerNames = controllerObjects.Select(x => x.Key).ToList();
                //var str = JsonConvert.SerializeObject(controllerNames, Formatting.Indented, settings1);
                //var exportFullPath = Path.Combine(ExportDir, "controllerNames.json");
                //File.WriteAllText(exportFullPath, str);                        
            }


            var gameObjectEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.GameObject);

            var heroModelEntries = gameObjectEntries.AsParallel().Where(x => x.Name.StartsWith("Avatar_")); //  && x.Name.EndsWith("_Model")
            var sourcePath = heroModelEntries.Select(x => x.Source).Distinct().ToList();
            
            var files = sourcePath.ToArray();

            Console.WriteLine($"Found {heroModelEntries.Count()} heroModelEntries");
            Console.WriteLine($"Found {files.Length} blk files");

            Queue<string> ErrorCABFiles = new();
            Queue<string> RootNames = new();
            Dictionary<long, string> ErrorFbxObject = new();
            Dictionary<long, string> IgnoreFbxObject = new();
            Dictionary<long, string> SuccessFbxObject = new();

            

            HashSet<string> LoadedFiles = new();

            Dictionary<long, string> successAnimationClips = new();
            Dictionary<long, string> failedAnimationClips = new();

            var fileCount = files.Length;
            var fileIndex = 1;
            foreach (var file in files)
            {
                Console.WriteLine("Process blk File: {0} / {1}", fileIndex++, fileCount);

                if (LoadedFiles.Contains(file))
                {
                    continue;
                }

                AssetsManager assetsManager = new AssetsManager();
                assetsManager.SpecifyUnityVersion = "";
                assetsManager.Game = Studio.Game;
                assetsManager.ResolveDependencies = false;
                assetsManager.SkipProcess = false;
                assetsManager.Silent = true;
                
                var toLoadingFiles = assetsManager.LoadFiles(file);

                foreach (var item in toLoadingFiles)
                {
                    LoadedFiles.Add(item);
                }

                var relativePath = Path.GetRelativePath(SourceGamePath, file);


                Console.WriteLine("\tReading Asset......");
                //Dictionary<ClassIDType, List<Object>> typeNames = new();
                Console.WriteLine("                   ");

                if (assetsManager.assetsFileList.Count > 0)
                {
                    var assetsCount = assetsManager.assetsFileList.Count;
                    var assetIndex = 1;
                    foreach (var item in assetsManager.assetsFileList)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        Console.WriteLine("Process assetsFileList: {0} / {1}", assetIndex++, assetsCount);
                        
                        List<GameObject> TopMostFathers = new();
                        //List<Avatar> AvatarObj = null;
                        foreach (var obj in item.Objects)
                        {
                            //typeNames.TryAdd(obj.type, new List<Object>());
                            //typeNames[obj.type].Add(obj);
                            switch (obj)
                            {
                                //case PBDSkinnedMeshRenderer m:
                                //    {
                                //        m.m_GameObject.TryGet(out var gameObject);
                                //        if (gameObject == null)
                                //        {
                                //            break;
                                //        }
                                //    }
                                //    break;
                                case SkinnedMeshRenderer skinnedMeshRenderer:
                                    {
                                        skinnedMeshRenderer.m_GameObject.TryGet(out var gameObject);
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
                                    }
                                    break;
                                //case Animator animator:
                                //    {
                                //        if(animator.m_Avatar.TryGet(out var avatar))
                                //        {
                                //            AvatarObj.Add(avatar);
                                //        }
                                //    }
                                //    break;
                                case MeshFilter meshFilter:
                                    {
                                        meshFilter.m_Mesh.TryGet(out var mesh);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        //if (AvatarObj != null) // 只导出有动画组件的模型
                        {
                            foreach (var rootGameObject in TopMostFathers)
                            {
                                RootNames.Enqueue((string)rootGameObject.m_Name);
                                if (!rootGameObject.Name.StartsWith("Avatar_"))// 过滤 只导出角色的   || !rootGameObject.Name.EndsWith("_Model")
                                {
                                    IgnoreFbxObject.TryAdd((long)rootGameObject.m_PathID, (string)rootGameObject.m_Name);
                                    continue;
                                }
                                if (rootGameObject.m_Animator == null || rootGameObject.m_Animator.m_Avatar.TryGet(out var avatar))
                                {
                                    IgnoreFbxObject.TryAdd((long)rootGameObject.m_PathID, (string)rootGameObject.m_Name);
                                    continue;
                                }

                                var exportFileName = Path.Combine(ExportDir, relativePath, item.fileName, (string)(rootGameObject.m_Name + ".fbx"));
                                SuccessFbxObject.TryAdd((long)rootGameObject.m_PathID, exportFileName);
                                if (File.Exists(exportFileName))
                                {
                                    SuccessFbxObject.TryAdd((long)rootGameObject.m_PathID, exportFileName);
                                    continue;
                                }
                                
                                var comList = new List<Component>();
                                var childGameObject = new List<GameObject>();
                                foreach (var pPtr in rootGameObject.m_Components)
                                {
                                    pPtr.TryGet(out var com);
                                    comList.Add(com);
                                }
                                foreach (var childPtr in rootGameObject.m_Transform.m_Children)
                                {
                                    childPtr.TryGet(out var child);
                                    child.m_GameObject.TryGet(out var childgo);
                                    childGameObject.Add(childgo);
                                }
                                // example: SeparateMesh_Avatar_Male_Size03_Ben_Model_Ben_Face
                                var modelName = rootGameObject.Name;
                                foreach (var child in childGameObject)
                                {
                                    if (child.m_SkinnedMeshRenderer != null)
                                    {
                                        var meshName = "SeparateMesh_" + modelName + "_" + child.Name;
                                        heroSeparateMeshObjects.TryGetValue(meshName, out var separateMesh);
                                        if (separateMesh != null)
                                        {
                                            child.m_SkinnedMeshRenderer.m_out_mesh = separateMesh;
                                        }
                                    }
                                }

                                var success = ExportRootGameObjectToFbx((GameObject)rootGameObject, exportFileName, avatar, out var clips);
                                if (success)
                                {
                                    SuccessFbxObject.TryAdd((long)rootGameObject.m_PathID, exportFileName);
                                }
                                else
                                {
                                    ErrorFbxObject.TryAdd((long)rootGameObject.m_PathID, exportFileName);
                                }

                                continue; // jump animation export

                                // example :Avatar_Female_Size03_JaneDoe_Controller
                                // example :Avatar_Female_Size03_JaneDoe_Controller_NPC
                                // example :Avatar_Female_Size03_JaneDoe_Controller_UI
                                // example :Avatar_Female_Size03_JaneDoe_Controller_MainCity
                                List<PPtr<AnimationClip>> animationClips = new();
                                List<RuntimeAnimatorController> controllerList = new();

                                var mainControllerName = modelName + "_Controller";
                                if (controllerObjects.TryGetValue(mainControllerName, out var mainController))
                                {
                                    controllerList.Add(mainController);
                                }
                                else
                                {
                                    var replacement = mainControllerName.Replace("_Model_", "_");
                                    if (controllerObjects.TryGetValue(replacement, out var mainController1))
                                    {
                                        controllerList.Add(mainController1);
                                    }
                                }
                                var npcControllerName = modelName + "_Controller_NPC";
                                if (controllerObjects.TryGetValue(mainControllerName, out var npcController))
                                {
                                    controllerList.Add(npcController);
                                }
                                else
                                {
                                    var replacement = npcControllerName.Replace("_Model_", "_");
                                    if (controllerObjects.TryGetValue(replacement, out var npcController1))
                                    {
                                        controllerList.Add(npcController1);
                                    }
                                }
                                var uiControllerName = modelName + "_Controller_UI";
                                if (controllerObjects.TryGetValue(mainControllerName, out var uiController))
                                {
                                    controllerList.Add(uiController);
                                }
                                else
                                {
                                    var replacement = uiControllerName.Replace("_Model_", "_");
                                    if (controllerObjects.TryGetValue(replacement, out var uiController1))
                                    {
                                        controllerList.Add(uiController1);
                                    }
                                }
                                var maincityControllerName = modelName + "_Controller_MainCity";
                                if (controllerObjects.TryGetValue(mainControllerName, out var maincityController))
                                {
                                    controllerList.Add(maincityController);
                                }
                                else
                                {
                                    var replacement = maincityControllerName.Replace("_Model_", "_");
                                    if (controllerObjects.TryGetValue(replacement, out var maincityController1))
                                    {
                                        controllerList.Add(maincityController1);
                                    }
                                }

                                foreach (var animController in controllerList)
                                {
                                    if (animController is AnimatorController animatorController)
                                    {
                                        foreach (var pptr in animatorController.m_AnimationClips)
                                        {
                                            animationClips.Add(pptr);
                                        }
                                    }
                                    else if (animController is AnimatorOverrideController overrideController)
                                    {
                                        if (overrideController.m_Controller.TryGet<AnimatorController>(out var sourceAnimatorController))
                                        {
                                            foreach (var pptr in sourceAnimatorController.m_AnimationClips)
                                            {
                                                animationClips.Add(pptr);
                                            }
                                        }
                                    }
                                }

                                if (animationClips.Count > 0)
                                {
                                    Dictionary<string, AnimationClip> PendingAnims = new();
                                    Dictionary<long, string> referencedAnimations = new();
                                    foreach (var clipPtr in animationClips)
                                    {
                                        clipPtr.TryGet(out var clip);

                                        if (clip == null)
                                        {
                                            referencedAnimations.TryAdd(clipPtr.m_PathID, clipPtr.assetsFile.fileName);
                                            failedAnimationClips.TryAdd(clipPtr.m_PathID, clipPtr.assetsFile.fileName);
                                            continue;
                                        }
                                        referencedAnimations.TryAdd(clip.m_PathID, clip.Name);

                                        var exportAnimFileName = Path.Combine(ExportDir, relativePath, clipPtr.assetsFile.fileName, clip.m_Name + ".anim");
                                        successAnimationClips.TryAdd(clipPtr.m_PathID, exportAnimFileName);
                                        if (File.Exists(exportAnimFileName))
                                        {
                                            continue;
                                        }
                                        PendingAnims.TryAdd(exportAnimFileName, clip);
                                    }
                                    {
                                        var controllerPath = Path.ChangeExtension(exportFileName, "controller");
                                        var settings1 = new JsonSerializerSettings();
                                        settings1.Converters.Add(new StringEnumConverter());
                                        var str = JsonConvert.SerializeObject(referencedAnimations, Formatting.Indented, settings1);
                                        File.WriteAllText(controllerPath, str);
                                    }
                                    //PendingAnims.AsParallel().ForAll((item) =>
                                    //{
                                    //    var exportFileName = item.Key;
                                    //    var ac = item.Value;

                                    //    ac.m_TOS = AvatarObj.m_TOS;

                                    //    Directory.CreateDirectory(Path.GetDirectoryName(exportFileName));
                                    //    var str = ac.Convert();
                                    //    if (string.IsNullOrEmpty(str))
                                    //    {
                                    //        return;
                                    //    }
                                    //    File.WriteAllText(exportFileName, str);
                                    //});
                                    //foreach (var itemanim in PendingAnims)
                                    //{
                                    //    var exportFileName1 = itemanim.Key;
                                    //    var ac = itemanim.Value;

                                    //    ac.m_TOS = ac.FindTOS();

                                    //    Directory.CreateDirectory(Path.GetDirectoryName(exportFileName1));
                                    //    var str = ac.Convert();
                                    //    if (string.IsNullOrEmpty(str))
                                    //    {
                                    //        return;
                                    //    }
                                    //    File.WriteAllText(exportFileName1, str);
                                    //}
                                }
                            }
                        }
                    }
                }
                assetsManager.Clear();
            }
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            {
                var str = JsonConvert.SerializeObject(ErrorCABFiles, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "ErrorCABFiles.json");
                File.WriteAllText(exportFullPath, str);
                Console.WriteLine($"ErrorCABFiles count:{0}", ErrorCABFiles.Count);
            }
            {
                var str = JsonConvert.SerializeObject(ErrorFbxObject, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "ErrorFbxObject.json");
                File.WriteAllText(exportFullPath, str);
            }
            {
                var str = JsonConvert.SerializeObject(SuccessFbxObject, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "SuccessFbxObject.json");
                File.WriteAllText(exportFullPath, str);
            }
            {
                var str = JsonConvert.SerializeObject(successAnimationClips, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "successAnimationClips.json");
                File.WriteAllText(exportFullPath, str);
            }
            {
                var str = JsonConvert.SerializeObject(failedAnimationClips, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "failedAnimationClips.json");
                File.WriteAllText(exportFullPath, str);
            }
            {
                var str = JsonConvert.SerializeObject(IgnoreFbxObject, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "IgnoreFbxObject.json");
                File.WriteAllText(exportFullPath, str);
            }
            controllerAssetsManager.Clear();

        }
    }

    private static void ExportGI()
    {
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


        // first animation asset pass
        bool bAnimationPass = true;
        Dictionary<long, string> GlobalSuccessAnims = new();
        if (bAnimationPass)
        {
            Console.WriteLine("Start Export Animations...");
            var files = Directory.GetFiles(SourceGamePath, "*.blk", SearchOption.AllDirectories);
            {
                var entries = ResourceMap.GetEntries();

                List<AssetEntry> pendingExportEntries = new List<AssetEntry>();

                var animEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.AnimationClip);
                var heroAnimEntries = animEntries.AsParallel().Where(x => x.Name.StartsWith("Ani_Avatar_")).Distinct().ToList();
                var sourcePath = heroAnimEntries.Select(x => x.Source).Distinct().ToList();

                files = sourcePath.ToArray();
            }
            Console.WriteLine($"Found {files.Length} blk files");

            Dictionary<long, string> ErrorAnims = new();
            Queue<string> IgnoreAnims = new();

            var fileCount = files.Length;
            var fileIndex = 1;
            foreach (var file in files)
            {
                Console.WriteLine("Process Asset File: {0} / {1}", fileIndex++, fileCount);

                AssetsManager assetsManager = new AssetsManager();
                assetsManager.SpecifyUnityVersion = "";
                assetsManager.Game = Studio.Game;
                assetsManager.ResolveDependencies = false;
                assetsManager.SkipProcess = false;
                assetsManager.Silent = true;
                assetsManager.LoadFiles(file);
                var relativePath = Path.GetRelativePath(SourceGamePath, file);

                Console.WriteLine("\tReading Asset......");
                Dictionary<string, AnimationClip> PendingAnims = new();


                if (assetsManager.assetsFileList.Count > 0)
                {
                    List<Avatar> avatars = new List<Avatar>();
                    List<Animator> animators = new List<Animator>();
                    List<Animation> animations = new List<Animation>();

                    foreach (var item in assetsManager.assetsFileList)
                    {
                        foreach (var obj in item.Objects)
                        {
                            switch (obj)
                            {
                                case AnimationClip ac:
                                    {
                                        if (!ac.m_Name.Contains("Ani_Avatar_"))// 过滤 只导出角色的
                                        {
                                            IgnoreAnims.Enqueue(ac.m_Name);
                                            continue;
                                        }
                                        var exportFileName = Path.Combine(ExportDir, relativePath, item.fileName, ac.m_Name + ".anim");
                                        if (File.Exists(exportFileName))
                                        {
                                            ErrorAnims.TryAdd(ac.m_PathID, exportFileName);
                                            continue;
                                        }
                                        PendingAnims.TryAdd(exportFileName, ac);
                                        GlobalSuccessAnims.TryAdd(ac.m_PathID, exportFileName);
                                    }
                                    break;
                                case Avatar avatar:
                                    avatars.Add(avatar);
                                    break;
                                case Animator animator:
                                    animators.Add(animator);
                                    break;
                                case Animation animation:
                                    animations.Add(animation);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    var bExport = true;
                    if (bExport && PendingAnims.Count > 0)
                    {
                        Console.WriteLine("\tExport {0} Assets.....", PendingAnims.Count);

                        // find tos
                        var tos = new Dictionary<uint, string>() { { 0, string.Empty } };
                        if (avatars.Count > 0)
                        {
                            foreach (var avatar in avatars)
                            {
                                foreach (var item in avatar.m_TOS)
                                {
                                    tos.TryAdd(item.Key, item.Value);
                                }
                            }
                        }

                        PendingAnims.AsParallel().ForAll((item) =>
                        {
                            var exportFileName = item.Key;
                            var ac = item.Value;

                            ac.m_TOS = tos;

                            Directory.CreateDirectory(Path.GetDirectoryName(exportFileName));
                            var str = ac.Convert();
                            if (string.IsNullOrEmpty(str))
                            {
                                return;
                            }
                            File.WriteAllText(exportFileName, str);
                        });
                    }
                }
                assetsManager.Clear();
            }

            {
                Console.WriteLine("IgnoreAnims count:{0}", IgnoreAnims.Count);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                var str = JsonConvert.SerializeObject(IgnoreAnims, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "IgnoreAnims.json");
                File.WriteAllText(exportFullPath, str);
            }

            {
                Console.WriteLine("ErrorAnims count:{0}", ErrorAnims.Count);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                var str = JsonConvert.SerializeObject(ErrorAnims, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "ErrorAnims.json");
                File.WriteAllText(exportFullPath, str);
            }
            {
                Console.WriteLine("SuccessAnims count:{0}", GlobalSuccessAnims.Count);
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                var str = JsonConvert.SerializeObject(GlobalSuccessAnims, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "SuccessAnims.json");
                File.WriteAllText(exportFullPath, str);
            }
        }
        else
        {
            var exportFullPath = Path.Combine(ExportDir, "SuccessAnims.json");
            var str = File.ReadAllText(exportFullPath);
            GlobalSuccessAnims = JsonConvert.DeserializeObject<Dictionary<long, string>>(str);
        }



        // model pass
        var bModelPass = true;
        if (bModelPass)
        {
            Console.WriteLine("Start Export Models...");

            var entries = ResourceMap.GetEntries();
            var gameObjectEntries = entries.AsParallel().Where(x => x.Type == ClassIDType.GameObject);
            var heroModelEntries = gameObjectEntries.AsParallel().Where(x => x.Name.StartsWith("Avatar_")).Distinct().ToList();
            var sourcePath = heroModelEntries.Select(x => x.Source).Distinct().ToList();

            var files = sourcePath.ToArray();

            Console.WriteLine($"Found {files.Length} blk files");

            Queue<string> ErrorCABFiles = new();
            Dictionary<long, string> ErrorFbxObject = new();
            Dictionary<long, string> SuccessFbxObject = new();

            var fileCount = files.Length;
            var fileIndex = 1;

            HashSet<string> LoadedFiles = new();

            foreach (var file in files)
            {
                Console.WriteLine("Process Asset File: {0} / {1}", fileIndex++, fileCount);

                if (LoadedFiles.Contains(file))
                {
                    continue;
                }

                AssetsManager assetsManager = new AssetsManager();
                assetsManager.SpecifyUnityVersion = "";
                assetsManager.Game = Studio.Game;
                assetsManager.ResolveDependencies = true;
                assetsManager.SkipProcess = false;
                assetsManager.Silent = true;
                var toLoadingFiles = assetsManager.LoadFiles(file);

                foreach (var item in toLoadingFiles)
                {
                    LoadedFiles.Add(item);
                }

                var relativePath = Path.GetRelativePath(SourceGamePath, file);


                Console.WriteLine("\tReading Asset......");
                Dictionary<string, GameObject> PendingRootObjects = new();
                if (assetsManager.assetsFileList.Count > 0)
                {
                    foreach (var item in assetsManager.assetsFileList)
                    {
                        List<GameObject> TopMostFathers = new();
                        Avatar AvatarObj = null;
                        foreach (var obj in item.Objects)
                        {
                            switch (obj)
                            {
                                case SkinnedMeshRenderer skinnedMeshRenderer:
                                    {
                                        skinnedMeshRenderer.m_GameObject.TryGet(out var gameObject);
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
                                    }
                                    break;
                                case Animator animator:
                                    {
                                        animator.m_Avatar.TryGet(out AvatarObj);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (AvatarObj != null) // 只导出有动画组件的模型
                        {
                            if (TopMostFathers.Count == 1)
                            {
                                var rootGameObject = TopMostFathers[0];
                                if (!rootGameObject.Name.StartsWith("Avatar_"))// 过滤 只导出角色的
                                {
                                    continue;
                                }
                                var exportFileName = Path.Combine(ExportDir, relativePath, item.fileName, rootGameObject.m_Name + ".fbx");
                                if (File.Exists(exportFileName))
                                {
                                    ErrorFbxObject.TryAdd(rootGameObject.m_PathID, exportFileName);
                                    continue;
                                }
                                ExportRootGameObjectToFbx(rootGameObject, exportFileName, AvatarObj, out var clips);
                                SuccessFbxObject.TryAdd(rootGameObject.m_PathID, exportFileName);
                            }
                            else
                            {
                                ErrorCABFiles.Enqueue(item.fileName);
                            }
                        }
                    }
                }
                assetsManager.Clear();
            }
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new StringEnumConverter());
            {
                var str = JsonConvert.SerializeObject(ErrorCABFiles, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "ErrorCABFiles.json");
                File.WriteAllText(exportFullPath, str);
                Console.WriteLine($"ErrorCABFiles count:{0}", ErrorCABFiles.Count);
            }
            {
                var str = JsonConvert.SerializeObject(ErrorFbxObject, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "ErrorFbxObject.json");
                File.WriteAllText(exportFullPath, str);
            }
            {
                var str = JsonConvert.SerializeObject(SuccessFbxObject, Formatting.Indented, settings);
                var exportFullPath = Path.Combine(ExportDir, "SuccessFbxObject.json");
                File.WriteAllText(exportFullPath, str);
            }
        }
    }
    private static void Main(string[] args)
    {
        //ExportGI();
        ExportZZZ();
    }
}

/*
{
    for (int i = 0; i < files.Length; i++)
    {
        Console.WriteLine("BuildAssetData: {0} / {1}", i + 1, files.Length);

        if (exportedAbsFiles.Contains(files[i])) 
        { 
            continue; 
        }
        AssetsManager assetsManager = new AssetsManager();
        assetsManager.SpecifyUnityVersion = "";
        assetsManager.Game = Studio.Game;
        assetsManager.ResolveDependencies = false;
        assetsManager.SkipProcess = false;
        assetsManager.Silent = true;
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
                    if (!rootGameObject.Name.Contains("Avatar_Girl_Pole_XianglingCostumeWinter", StringComparison.OrdinalIgnoreCase))
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
                        ExportRootGameObjectToFbx(rootGameObject, exportFullPath, AvatarObj);
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
                            if (!rootGameObject.Name.Contains("XiangLin", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            ExportRootGameObjectToFbx(rootGameObject, exportFullPath, AvatarObj);
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
                            ExportPrefab(rootGameObject, exportFullPath);
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
*/



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