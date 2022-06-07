using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
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
    [BepInDependency(CustomAssetsLibrary.CustomAssetLib.Guid)]
    public class ZeusPlugin : BaseUnityPlugin
    {
        // constants
        public const string Guid = "org.HF.plugins.Zeus";
        public const string Version = "1.0.0.0";
        private const string Name = "HolloFoxes' Zeus";

        internal static ConfigEntry<LogLevel> LogLevel { get; set; }
        internal static ConfigEntry<AutoDownload> AutoDownloadPacks { get; set; }
        internal static Harmony harmony;

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
        }

        private void Update()
        {
            if (LoadAssetPatch.DownloadedAssets.TryTake(out var dir))
            {
                CustomAssetsLibrary.Patches.AssetDbOnSetupInternalsPatch.LoadDirectory(dir);
            }
        }
    }
}