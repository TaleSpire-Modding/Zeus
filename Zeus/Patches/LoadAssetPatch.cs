using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine.EventSystems;
using PluginMasters;


// ReSharper disable once CheckNamespace
namespace Zeus.Patches
{
    [HarmonyPatch(typeof(UI_InventoryIcon), "OnPointerClick")]
    public class UIClickPatch
    {
        static bool Prefix(ref PointerEventData eventData,ref CreatureDataV2 ____creatureData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return true;
            CreatureManager.TryGetUniqueCreatureData(____creatureData.UniqueId, out ____creatureData);
            CreatureManager.MakeCreatureNonUnique(____creatureData.CreatureId);
            return false;
        }
    }

    [HarmonyPatch(typeof(CreatureBoardAsset), "OnBaseLoaded")]
    public class LoadAssetPatch
    {
        internal static Dictionary<string, string> ZeusDb;

        internal static Dictionary<string,Package> loadingAssetPacks = new Dictionary<string, Package>();

        internal static void LoadZeusDb()
        {
            const string requestUri = "https://dl.dropboxusercontent.com/s/j993jarhrwx0hb5/hashmap.json?dl=0";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);
            httpWebRequest.Method = WebRequestMethods.Http.Get;
            httpWebRequest.Accept = "application/json";

            var response = (HttpWebResponse)httpWebRequest.GetResponse();
            string content;
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                content = sr.ReadToEnd();
            }

            ZeusDb = JsonConvert.DeserializeObject<Dictionary<string, string>>(content) ?? new Dictionary<string, string>();
            Debug.Log(content);
        }

        static void Prompt(string assetPackId)
        {
            loadingAssetPacks[assetPackId] = new Package();

            switch (ZeusPlugin.AutoDownloadPacks.Value)
            {
                case AutoDownload.Null:
                    SystemMessage.SendSystemMessage($"Found missing packs: {assetPackId}",
                        "Do you want to download the missing pack?"
                        , "Download Pack", () =>
                        {
                            LoadPack(assetPackId);
                            ZeusPlugin.AutoDownloadPacks.Value = AutoDownload.Yes;
                        }, "Not Now", () =>
                        {
                            ZeusPlugin.AutoDownloadPacks.Value = AutoDownload.No;
                        }
                    );
                    break;
                case AutoDownload.Yes:
                {
                    LoadPack(assetPackId);
                    break;
                }
            }
        }
        

        private static string dirPlugin = BepInEx.Paths.PluginPath;

        static void LoadPack(string _assetPackId)
        {
            var assetPackId = _assetPackId;
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

            loadingAssetPacks[assetPackId] = JsonConvert.DeserializeObject<Package>(content);
            var package = loadingAssetPacks[assetPackId];

            var modManComd = $"ror2mm://v1/install/talespire.thunderstore.io/{package.owner}/{package.name}/{package.versions[0].version_number}/";
            System.Diagnostics.Process.Start(modManComd)?.WaitForExit();
            
            var dir = Path.Combine(dirPlugin, $"{package.owner}-{package.name}");
            
            CustomAssetsLibraryPlugin._self.RegisterAssets();
            if (File.Exists(Path.Combine(dir, "index")))
            {
                CustomAssetsLibrary.Patches.AssetLoadManagerOnInstanceSetupPatch.LoadDirectory(dir);
            }
        }

        static bool Prefix(ref CreatureBoardAsset __instance)
        {
            var id = __instance.BoardAssetId;

            var noCreatureId = !AssetDb.Creatures.ContainsKey(id);
            var zeusHadId = ZeusDb.ContainsKey(id.ToString());
            if (!noCreatureId || !zeusHadId) return true;
            
            var assetPackId = ZeusDb[id.ToString()];
            Debug.Log($"We found it in this asset pack: {assetPackId}");

            if (!loadingAssetPacks.ContainsKey(assetPackId))
            {
                Prompt(assetPackId);
            }
            return true;
        }

    }
}