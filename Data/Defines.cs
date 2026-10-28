namespace AMP.Data {
    public class Defines {
        
        public static readonly string MOD_DEV_STATE    = "Beta";
        public static readonly string MOD_VERSION      = MOD_DEV_STATE + " 7";
        public static readonly string MOD_SUFFIX       = "-9";
        public static readonly string FULL_MOD_VERSION = MOD_VERSION + MOD_SUFFIX;
        public static readonly string MOD_NAME         = "AMP " + FULL_MOD_VERSION;
        
        public static readonly string AMP           = "AMP";
        public static readonly string SERVER        = "Server";
        public static readonly string CLIENT        = "Client";
        public static readonly string WEB_INTERFACE = "Web";
        public static readonly string DISCORD_SDK   = "DiscordSDK";
        
        public static readonly uint   STEAM_APPID          = 629730;
        public static readonly uint   STEAM_APPID_SPACEWAR = 480;
        
        public static readonly int    MAX_PLAYERS   = 10;
    }
}
