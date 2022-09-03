using AMP.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Data {
    public class ServerConfig {
        public static INIFile settings;

        public static string path = null;

        public static bool pvpEnable = true;
        public static float pvpDamageMultiplier = 0.5f;
        public static int maxPlayers = 10;

        public static void Load() {
            if(path == null) path = Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "server.ini");

            settings = new INIFile(path);

            if(!settings.FileExists()) {
                Save();
            }

            pvpEnable = settings.GetOption("pvpEnable", pvpEnable);
            pvpDamageMultiplier = settings.GetOption("pvpDamageMultiplier", pvpDamageMultiplier);
            maxPlayers = settings.GetOption("maxPlayers", maxPlayers);

        }

        public static void Save() {
            settings.SetOption("pvpEnable", pvpEnable);
            settings.SetOption("pvpDamageMultiplier", pvpDamageMultiplier);
            settings.SetOption("maxPlayers", maxPlayers);

        }

    }
}
