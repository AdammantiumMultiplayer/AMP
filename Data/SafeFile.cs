using AMP.Logging;
using Newtonsoft.Json;
using System;
using System.IO;

namespace AMP.Data {
    public class SafeFile {

        [JsonIgnore]
        public string filePath = "";

        #region Values
        public string username = "";

        public long lastNameRead = 0;

        public ModSettings     modSettings     = new ModSettings();
        public HostingSettings hostingSettings = new HostingSettings();
        public InputCache      inputCache      = new InputCache();
        #endregion

        #region Subclasses
        public class ModSettings {
            public bool useBrowserIntegration   = true;
            public bool useAdvancedNpcSyncing   = true;
            public bool useSpaceWarMode         = false;
            public float minPredictionTreshhold = 0.15f;

            public bool ShouldPredict(float compensationFactor) {
                return ModLoader.ClientsidePrediction && compensationFactor > minPredictionTreshhold;
            }
        }

        public class HostingSettings {
            public bool   pvpEnable             = true;
            public float  pvpDamageMultiplier   = 0.2f;
            public float  pvpPushbackMultiplier = 2f;
            public bool   allowMapChange        = true;
            public int    maxItemsPerPlayer     = 250;
            public int    maxCreaturesPerPlayer = 15;
            public string masterServerUrl       = "amp.adamite.de";
            public bool   allowVoiceChat        = true;
            public byte   baseTickRate          = 10;
            public byte   playerTickRate        = 30;

            public bool useModWhitelist         = false;
            public string[] modWhitelist        = new string[0];
            public bool useModBlacklist         = false;
            public string[] modBlacklist        = new string[0];
            public bool useModRequirelist       = false;
            public string[] modRequirelist      = new string[0];
        }

        public class InputCache {
            public string join_address            = "127.0.0.1";
            public ushort join_port               = 26950;
            public string join_password           = "";

            public ushort host_port               = 26950;
            public string host_password           = "";
            public uint   host_max_players        = 4;
            public bool   host_steam_friends_only = true;
        }
        #endregion
        
        #region Save and Load
        public void Save() {
            Save(filePath);
        }
        public void Save(string path) {
            Save(this, path);
        }
        
        public static void Save(SafeFile safeFile, string path) {
            if(path == null || path.Length == 0) return;
            string json = JsonConvert.SerializeObject(safeFile, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        
        public static SafeFile Load(string path) {
            SafeFile safeFile = new SafeFile();

            bool safe = true;
            if(File.Exists(path)) {
                string json = File.ReadAllText(path);
                try {
                    safeFile = JsonConvert.DeserializeObject<SafeFile>(json);
                } catch(Exception e) {
                    Log.Err(e);
                    safe = false;
                }
            }
            safeFile.filePath = path;

            if(safe) safeFile.Save();

            return safeFile;
        }
        #endregion
    }
}
