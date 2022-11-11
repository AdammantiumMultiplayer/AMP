namespace AMP.Data {
    public class GameConfig {
        public static INIFile settings;

        public static bool showPlayerNames = true;
        public static bool showPlayerHealthBars = true;
        public static bool useBrowserIntegration = true;
        public static bool useAdvancedNpcSyncing = true;

        public static void Load(string path) {
            settings = new INIFile(path);

            if(!settings.FileExists()) {
                Save();
            }

            showPlayerNames = settings.GetOption("showPlayerNames", showPlayerNames);
            showPlayerHealthBars = settings.GetOption("showPlayerHealthBars", showPlayerHealthBars);
            useBrowserIntegration = settings.GetOption("useBrowserIntegration", useBrowserIntegration);
            useAdvancedNpcSyncing = settings.GetOption("useAdvancedNpcSyncing", useAdvancedNpcSyncing);

            Save();
        }

        public static void Save() {
            settings.SetOption("showPlayerNames", showPlayerNames);
            settings.SetOption("showPlayerHealthBars", showPlayerHealthBars);
            settings.SetOption("useBrowserIntegration", useBrowserIntegration);
            settings.SetOption("useAdvancedNpcSyncing", useAdvancedNpcSyncing);

        }

    }
}
