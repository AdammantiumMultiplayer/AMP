using AMP.Data;
using AMP.Logging;
using Steamworks;

namespace AMP.SteamNet {
    internal class SteamIntegration {

        private static SteamIntegration currentInstance;

        public static SteamIntegration Instance {
            get {
                if(currentInstance == null) currentInstance = new SteamIntegration();
                return currentInstance;
            }
        }

        public bool isInitialized = false;
        public string username = "";

        public SteamIntegration() {
            isInitialized = false;
            try {
                if(SteamAPI.RestartAppIfNecessary((AppId_t) Defines.STEAM_APPID)) { // First try using the BnS AppId
                    if(SteamAPI.RestartAppIfNecessary((AppId_t) Defines.STEAM_APPID_FB)) { // If BnS AppId didnt work, just use the fallback one (SpaceWar)
                        return;
                    } else {
                        Log.Info(Defines.STEAM_API, "AMP connected to steam using the SpaceWar AppId.");
                    }
                } else {
                    Log.Info(Defines.STEAM_API, "AMP connected to steam using the Blade & Sorcery AppId.");
                }
                isInitialized = true;

                username = SteamFriends.GetPersonaName();
            } catch(System.DllNotFoundException e) {
                Log.Err("[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" + e, this);
                return;
            }
        }
    }
}
