namespace AMP.Data {
    public class ServerConfig {
        public static INIFile settings;

        public static bool   pvpEnable           = true;
        public static float  pvpDamageMultiplier = 0.2f;
        public static int    maxPlayers          = 10;
        public static bool   allowMapChange      = true;
        public static string password            = "";

        public static void Load(string path) {
            settings = new INIFile(path);

            if(!settings.FileExists()) {
                Save();
            }

            pvpEnable           = settings.GetOption("pvpEnable",           pvpEnable          );
            pvpDamageMultiplier = settings.GetOption("pvpDamageMultiplier", pvpDamageMultiplier);
            maxPlayers          = settings.GetOption("maxPlayers",          maxPlayers         );
            allowMapChange      = settings.GetOption("allowMapChange",      allowMapChange     );
            password            = settings.GetOption("password",            password           );

            Save();
        }

        public static void Save() {
            settings.SetOption("pvpEnable",           pvpEnable          );
            settings.SetOption("pvpDamageMultiplier", pvpDamageMultiplier);
            settings.SetOption("maxPlayers",          maxPlayers         );
            settings.SetOption("allowMapChange",      allowMapChange     );
            settings.SetOption("password",            password           );

        }

    }
}
