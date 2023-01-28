using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using PluginMasters;
using UnityEngine;
using Zeus.Patches;

namespace Zeus
{
    public enum LogLevel
    {
        None,
        Low,
        Medium,
        High,
        All,
    }

    public enum AutoDownload
    {
        Null,
        No,
        Yes
    }

    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(CustomAssetsLoader.CustomAssetLoader.Guid)]
    [BepInDependency(CustomAssetsLibraryPlugin.Guid)]
    public sealed class ZeusPlugin : BaseUnityPlugin
    {
        // constants
        public const string Guid = "org.HF.plugins.Zeus";
        public const string Version = "1.2.0.0";
        private const string Name = "HolloFoxes' Zeus";
        private static readonly string dirPlugin = Paths.PluginPath;


        internal static ConfigEntry<LogLevel> LogLevel { get; set; }
        internal static ConfigEntry<AutoDownload> AutoDownloadPacks { get; set; }
        internal static Harmony harmony;

        internal static Semaphore TrackedAssetsPool = new Semaphore(initialCount: 1, maximumCount: 1);
        internal static List<string> TrackedPacks = new List<string>();

        internal static Semaphore PendingAssetsPool = new Semaphore(initialCount: 1, maximumCount: 1);
        internal static List<string> AssetsToDownload = new List<string>();

        internal static Semaphore CompilingPool = new Semaphore(initialCount: 1, maximumCount: 1);
        internal static List<string> AssetsToCompile = new List<string>();
        
        internal static List<string> AssetsToLoad = new List<string>();

        public static void DoPatching()
        {
            harmony = new Harmony(Guid);
            harmony.PatchAll();
            if (LogLevel.Value > Zeus.LogLevel.None) Debug.Log($"Zeus: Patched.");
        }

        public static void UnPatch()
        {
            harmony.UnpatchSelf();
            if (LogLevel.Value > Zeus.LogLevel.None) Debug.Log($"Zeus: UnPatched.");
        }

        public static void DoConfig(ConfigFile Config)
        {
            LogLevel = Config.Bind("Logging", "Level", Zeus.LogLevel.Low);
            AutoDownloadPacks = Config.Bind("Download", "Auto Download", AutoDownload.Null);
            if (LogLevel.Value > Zeus.LogLevel.None) Debug.Log($"Zeus: Config Bound.");
        }

        private void Awake()
        {
            DoConfig(Config);
            DoPatching();
            if (LogLevel.Value > Zeus.LogLevel.None) Debug.Log($"Zeus:{Name} is Active.");

            // non-blocking awake in event of future large downloads
            var loadThread = new Thread(LoadAssetPatch.LoadZeusDb);
            loadThread.Start();

            InvokeRepeating(nameof(InvokedUpdate),1,2);
        }

        private void InvokedUpdate()
        {
            if (PendingAssetsPool.WaitOne(1))
            {
                if (AssetsToDownload.Any())
                    new Thread(DownloadPacks).Start();
                PendingAssetsPool.Release();
            }

            if (CompilingPool.WaitOne(1))
            {
                if (AssetsToCompile.Any())
                    CompilingPacks();
                CompilingPool.Release();
            }

            if (AssetsToLoad.Any())
                    LoadPacks();
        }

        private static void DownloadPacks()
        {
            PendingAssetsPool.WaitOne();

            var dir = new List<string>();

            foreach (var assetPackId in AssetsToDownload)
            {
                
                var requestUri = "https://talespire.thunderstore.io/api/v1/package/" + assetPackId;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);
                httpWebRequest.Method = WebRequestMethods.Http.Get;
                httpWebRequest.Accept = "application/json";

                var response = (HttpWebResponse)httpWebRequest.GetResponse();
                string content;
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    content = sr.ReadToEnd();
                }
                
                var package = JsonConvert.DeserializeObject<Package>(content);

                var modManComd = $"ror2mm://v1/install/talespire.thunderstore.io/{package.owner}/{package.name}/{package.versions[0].version_number}/";
                System.Diagnostics.Process.Start(modManComd)?.WaitForExit();
                
                dir.Add( Path.Combine(dirPlugin, $"{package.owner}-{package.name}"));
            }
            AssetsToDownload.Clear();
            PendingAssetsPool.Release();

            CompilingPool.WaitOne();
            AssetsToCompile.AddRange(dir);
            CompilingPool.Release();
        }

        private static void CompilingPacks()
        {
            CustomAssetsLibraryPlugin._self.RegisterAssets();
            AssetsToLoad.AddRange(AssetsToCompile.ToList());
            AssetsToCompile.Clear();
        }

        private static void LoadPacks()
        {
            foreach (var d in AssetsToLoad.Where(d => File.Exists(Path.Combine(d, "index"))))
            {
                CustomAssetsLibrary.Patches.AssetLoadManagerOnInstanceSetupPatch.LoadDirectory(d);
            }
            AssetsToLoad.Clear();

            var indexLoadedCreatures = new List<CreatureBoardAsset>();

            foreach (var problem in LoadAssetPatch.ProblemCreatures)
            {
                if (!problem.TryGetActiveMorph(out var c) ||
                    !AssetDb.TryGetIndexData(problem.BoardAssetId, out var indexData)) continue;
                c.OnIndexLoaded(indexData);
                indexLoadedCreatures.Add(problem);
            }

            LoadAssetPatch.ProblemCreatures.RemoveAll(t => indexLoadedCreatures.Contains(t));
        }
    }
}