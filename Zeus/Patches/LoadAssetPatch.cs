using System.Collections.Concurrent;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HarmonyLib;
using Newtonsoft.Json;
using System.Threading;
using CustomAssetsLibrary.ReflecExt;


// ReSharper disable once CheckNamespace
namespace Zeus.Patches
{
    [HarmonyPatch(typeof(CreatureBoardAsset), "OnBaseLoaded")]
    public class LoadAssetPatch
    {
        internal static Dictionary<string, string> ZeusDb;

        internal static Dictionary<string,Package> loadingAssetPacks = new Dictionary<string, Package>();

        internal static ConcurrentBag<string> DownloadedAssets = new ConcurrentBag<string>();

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

            if (ZeusPlugin.AutoDownloadPacks.Value == AutoDownload.Null)
            {

                SystemMessage.SendSystemMessage($"Found missing packs: {assetPackId}",
                    "Do you want to download the missing pack?"
                    , "Download Pack", () =>
                    {
                        _assetPackId = assetPackId;
                        var t = new Thread(LoadPack);
                        t.Start();
                        ZeusPlugin.AutoDownloadPacks.Value = AutoDownload.Yes;
                    }, "Not Now", () =>
                    {
                        ZeusPlugin.AutoDownloadPacks.Value = AutoDownload.No;
                    }
                    );
            } else if (ZeusPlugin.AutoDownloadPacks.Value == AutoDownload.Yes)
            {
                _assetPackId = assetPackId;
                var t = new Thread(LoadPack);
                t.Start();
            }
        }

        private static string _assetPackId;

        private static string dirPlugin = BepInEx.Paths.PluginPath;

        static void LoadPack()
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
            System.Diagnostics.Process.Start(modManComd).WaitForExit();
            
            DownloadedAssets.Add(Path.Combine(dirPlugin, $"{package.owner}-{package.name}"));
        }

        static bool Prefix(ref CreatureBoardAsset __instance)
        {
            var id = __instance.BoardAssetId;

            var noCreatureId = !AssetDb.Creatures.ContainsKey(id);
            var zeusHadId = ZeusDb.ContainsKey(id.ToString());
            if ( noCreatureId && zeusHadId)
            {
                var assetPackId = ZeusDb[id.ToString()];
                Debug.Log($"We found it in this asset pack: {assetPackId}");

                if (!loadingAssetPacks.ContainsKey(assetPackId))
                {
                    Prompt(assetPackId);
                }
            }
            return true;
            
        }

    }
}