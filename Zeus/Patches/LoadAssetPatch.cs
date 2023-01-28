using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine.EventSystems;
using System.Threading;


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

        internal static List<CreatureBoardAsset> ProblemCreatures = new List<CreatureBoardAsset>();

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
                            new Thread(() => LoadPack(assetPackId)).Start();
                            ZeusPlugin.AutoDownloadPacks.Value = AutoDownload.Yes;
                        }, "Not Now", () =>
                        {
                            ZeusPlugin.AutoDownloadPacks.Value = AutoDownload.No;
                        }
                    );
                    break;
                case AutoDownload.Yes:
                {
                    new Thread(() => LoadPack(assetPackId)).Start();
                    break;
                }
                case AutoDownload.No:
                    break;
                default:
                    Debug.Log("No idea wtf you were doing but okay...");
                    ZeusPlugin.AutoDownloadPacks.Value = AutoDownload.Null;
                    break;
            }
        }

        /// <summary>
        /// Pushing the asset pack loading onto a different non-main thread 
        /// </summary>
        /// <param name="assetPackId"></param>
        private static void LoadPack(string assetPackId)
        {
            ZeusPlugin.TrackedAssetsPool.WaitOne();
            if (!ZeusPlugin.TrackedPacks.Contains(assetPackId))
            {
                ZeusPlugin.TrackedPacks.Add(assetPackId);
                ZeusPlugin.PendingAssetsPool.WaitOne();
                ZeusPlugin.AssetsToDownload.Add(assetPackId);
                ZeusPlugin.PendingAssetsPool.Release();
            }
            ZeusPlugin.TrackedAssetsPool.Release();
        }

        static bool Prefix(ref CreatureBoardAsset __instance)
        {
            var id = __instance.BoardAssetId;

            var noCreatureId = !AssetDb.Creatures.ContainsKey(id);

            if (!noCreatureId) return true;
            
            var zeusHadId = ZeusDb.ContainsKey(id.ToString());
            
            if (!zeusHadId) return true;
            
            var assetPackId = ZeusDb[id.ToString()];
            Debug.Log($"We found it in this asset pack: {assetPackId}");

            ProblemCreatures.Add(__instance);

            if (!loadingAssetPacks.ContainsKey(assetPackId))
            {
                Prompt(assetPackId);
            }
            return true;
        }

    }
}