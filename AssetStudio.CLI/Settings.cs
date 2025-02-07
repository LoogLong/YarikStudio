using System;
using System.ComponentModel;
using System.Configuration;

namespace AssetStudio.CLI.Properties {
    public static class AppSettings
    {
        public static string Get(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static TValue Get<TValue>(string key, TValue defaultValue)
        {
            try
            {
                var value = Get(key);

                if (string.IsNullOrEmpty(value)) 
                    return defaultValue;

                return (TValue)TypeDescriptor.GetConverter(typeof(TValue)).ConvertFromInvariantString(value);
            }
            catch (Exception)
            {
                Console.WriteLine($"Invalid value at \"{key}\", switching to default value [{defaultValue}] !!");
                return defaultValue;
            }
            
        }
    }

    public class Settings
    {
        private static Settings defaultInstance = new Settings();

        public static Settings Default => defaultInstance;

        public bool convertTexture => AppSettings.Get("convertTexture", true);
        public bool convertAudio => AppSettings.Get("convertAudio", true);
        public ImageFormat convertType => AppSettings.Get("convertType", ImageFormat.Png);
        public bool eulerFilter => AppSettings.Get("eulerFilter", true);
        public decimal filterPrecision => AppSettings.Get("filterPrecision", (decimal)0.25);
        public bool exportAllNodes => AppSettings.Get("exportAllNodes", true);
        public bool exportSkins => AppSettings.Get("exportSkins", true);
        public bool exportMaterials => AppSettings.Get("exportMaterials", false);
        public bool collectAnimations => AppSettings.Get("collectAnimations", true);
        public bool exportAnimations => AppSettings.Get("exportAnimations", true);
        public decimal boneSize => AppSettings.Get("boneSize", (decimal)10);
        public int fbxVersion => AppSettings.Get("fbxVersion", 3);
        public int fbxFormat => AppSettings.Get("fbxFormat", 0);
        public decimal scaleFactor => AppSettings.Get("scaleFactor", (decimal)1);
        public bool exportBlendShape => AppSettings.Get("exportBlendShape", true);
        public bool castToBone => AppSettings.Get("castToBone", false);
        public bool restoreExtensionName => AppSettings.Get("restoreExtensionName", true);
        public bool enableFileLogging => AppSettings.Get("enableFileLogging", false);
        public bool minimalAssetMap => AppSettings.Get("minimalAssetMap", true);
        public bool allowDuplicates => AppSettings.Get("allowDuplicates", false);
        public string types => AppSettings.Get("types", @"{""Animation"":{""Item1"":true,""Item2"":true},""AnimationClip"":{""Item1"":true,""Item2"":true},""Animator"":{""Item1"":true,""Item2"":true},""AnimatorController"":{""Item1"":true,""Item2"":true},""AnimatorOverrideController"":{""Item1"":true,""Item2"":true},""AssetBundle"":{""Item1"":true,""Item2"":false},""AudioClip"":{""Item1"":true,""Item2"":false},""Avatar"":{""Item1"":true,""Item2"":true},""Font"":{""Item1"":true,""Item2"":false},""GameObject"":{""Item1"":true,""Item2"":true},""IndexObject"":{""Item1"":true,""Item2"":false},""Material"":{""Item1"":true,""Item2"":true},""Mesh"":{""Item1"":true,""Item2"":true},""MeshFilter"":{""Item1"":true,""Item2"":true},""MeshRenderer"":{""Item1"":true,""Item2"":true},""MiHoYoBinData"":{""Item1"":true,""Item2"":true},""MonoBehaviour"":{""Item1"":true,""Item2"":true},""MonoScript"":{""Item1"":true,""Item2"":false},""MovieTexture"":{""Item1"":true,""Item2"":true},""PlayerSettings"":{""Item1"":true,""Item2"":false},""RectTransform"":{""Item1"":true,""Item2"":false},""Shader"":{""Item1"":false,""Item2"":false},""SkinnedMeshRenderer"":{""Item1"":true,""Item2"":true},""Sprite"":{""Item1"":true,""Item2"":false},""SpriteAtlas"":{""Item1"":true,""Item2"":false},""TextAsset"":{""Item1"":true,""Item2"":false},""Texture2D"":{""Item1"":true,""Item2"":true},""Transform"":{""Item1"":true,""Item2"":false},""VideoClip"":{""Item1"":true,""Item2"":false},""ResourceManager"":{""Item1"":true,""Item2"":false}}");
        public string texs => AppSettings.Get("texs", "{}");
        public string uvs => AppSettings.Get("uvs", "{\"UV0\":{\"Item1\":true,\"Item2\":0},\"UV1\":{\"Item1\":true,\"Item2\":1},\"UV2\":{\"Item1\":fal" +
            "se,\"Item2\":0},\"UV3\":{\"Item1\":false,\"Item2\":0},\"UV4\":{\"Item1\":false,\"Item2\":0},\"U" +
            "V5\":{\"Item1\":false,\"Item2\":0},\"UV6\":{\"Item1\":false,\"Item2\":0},\"UV7\":{\"Item1\":fal" +
            "se,\"Item2\":0}}");

    }
}
