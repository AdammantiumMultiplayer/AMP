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

        public ModSettings     modSettings     = new ModSettings();
        public HostingSettings hostingSettings = new HostingSettings();
        public InputCache      inputCache      = new InputCache();
        #endregion

        #region Subclasses
        public class ModSettings {
            public bool showPlayerNames       = true;
            public bool showPlayerHealthBars  = true;
            public bool useBrowserIntegration = true;
            public bool useAdvancedNpcSyncing = true;
        }

        public class HostingSettings {
            public bool   pvpEnable           = true;
            public float  pvpDamageMultiplier = 0.2f;
            public bool   allowMapChange      = true;
        }

        public class InputCache {
            public string join_ip          = "127.0.0.1";
            public ushort join_port        = 26950;
            public string join_password    = "";

            public ushort host_port        = 26950;
            public string host_password    = "";
            public uint   host_max_players = 4;
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
