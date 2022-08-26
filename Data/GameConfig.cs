using AMP.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Data {
    public class GameConfig {
        public static INIFile settings;

        public static bool showPlayerNames = true;
        public static bool showPlayerHealthBars = true;

        public static void Load() {
            settings = new INIFile(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "config.ini"));

            if(!settings.FileExists()) {
                Save();
            }

            showPlayerNames = settings.GetOption("showPlayerNames", showPlayerNames);
            showPlayerHealthBars = settings.GetOption("showPlayerHealthBars", showPlayerHealthBars);

        }

        public static void Save() {
            settings.SetOption("showPlayerNames", showPlayerNames);
            settings.SetOption("showPlayerHealthBars", showPlayerHealthBars);

        }

    }
}
