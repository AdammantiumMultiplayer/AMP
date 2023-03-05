namespace AMP.Data {
    public class Defines {

        public static string MOD_DEV_STATE = "Alpha";
        public static string MOD_VERSION   = MOD_DEV_STATE + " 0.7.0";
        public static string MOD_SUFFIX    = "";
        public static string MOD_NAME      = "AMP " + MOD_VERSION + MOD_SUFFIX;

        public const string AMP           = "AMP";
        public const string SERVER        = "Server";
        public const string CLIENT        = "Client";
        public const string WEB_INTERFACE = "Web";
        public const string DISCORD_SDK   = "DiscordSDK";
        public const string STEAM_API     = "Steamworks.NET";

        public const uint   STEAM_APPID              = 629730;
        public const uint   STEAM_APPID_FALLBACK     = 480;
        public const int    STEAM_RELIABLE_CHANNEL   = 0;
        public const int    STEAM_UNRELIABLE_CHANNEL = 1;

        public const int    MAX_PLAYERS    = 10;
    }
}
