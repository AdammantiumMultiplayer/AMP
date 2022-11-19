using AMP.Logging;
using AMP.Network.Handler;
using Steamworks;
using System;
using System.IO;
using UnityEngine;

namespace AMP.Steam {
    internal class SteamIntegration {

        private static readonly string STEAM_API_FILE = Path.Combine(Application.dataPath, "Plugins", "x86_64", "steam_api64.dll");
        public static void TryToInitSteam() {
            CheckForSteamAPI();

            try {
                if(SteamManager.Initialized) {
                    string name = SteamFriends.GetPersonaName();
                    Log.Info("AMP", $"Established connection to Steam with user \"{name}\".");
                } else {
                    Log.Err("AMP", $"Couldn't initialize Steam, so no Steam Networking is available.");
                }
            }catch(Exception e) {
                Log.Err("AMP", $"Couldn't initialize Steam, so no Steam Networking is available:\n{e}");
            }
        }

        private static void CheckForSteamAPI() {
            if(!File.Exists(STEAM_API_FILE)) {
                Log.Warn("AMP", "Couldn't find Steam API, extracting it now.");
                using(var file = new FileStream(STEAM_API_FILE, FileMode.Create, FileAccess.Write)) {
                    file.Write(Properties.Resources.steam_api64, 0, Properties.Resources.steam_api64.Length);
                }
            }
        }

    }
}
