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

        public static bool pvpEnable = true;
        public static float pvpDamageMultiplier = 0.5f;

        public static void Load() {
            settings = new INIFile(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "server.ini"));

            if(!settings.FileExists()) {
                Save();
            }

            pvpEnable = settings.GetOption("pvpEnable", pvpEnable);
            pvpDamageMultiplier = settings.GetOption("pvpDamageMultiplier", pvpDamageMultiplier);

        }

        public static void Save() {
            settings.SetOption("pvpEnable", pvpEnable);
            settings.SetOption("pvpDamageMultiplier", pvpDamageMultiplier);

        }

    }
}
