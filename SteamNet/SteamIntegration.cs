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
        public string mySteamName = "";
        public CSteamID mySteamId;

        public SteamNetHandler steamNet = null;

        public SteamIntegration() {
            isInitialized = false;
            try {
                if(SteamAPI.RestartAppIfNecessary((AppId_t) Defines.STEAM_APPID)) { // First try using the BnS AppId
                    if(SteamAPI.RestartAppIfNecessary((AppId_t) Defines.STEAM_APPID_FALLBACK)) { // If BnS AppId didnt work, just use the fallback one (SpaceWar)
                        Log.Err(Defines.STEAM_API, "Could not link to Steam, is it running?");
                        return;
                    } else {
                        Log.Info(Defines.STEAM_API, "AMP connected to steam using the SpaceWar AppId.");
                    }
                } else {
                    Log.Info(Defines.STEAM_API, "AMP connected to steam using the Blade & Sorcery AppId.");
                }
                isInitialized = true;

                mySteamName = SteamFriends.GetPersonaName();
                mySteamId = SteamUser.GetSteamID();
            } catch(System.DllNotFoundException) {
                Log.Err(Defines.STEAM_API, "Could not load Steam API. If you are using the Steam Version this shouldn't happen, if its the Oculus Version, please try reinstalling.");
                return;
            }

            RegisterCallbacks();
        }

        protected Callback<GameLobbyJoinRequested_t> callbackGameLobbyJoinRequested;

        public void RegisterCallbacks() {
            callbackGameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
        }

        // Gets executed when a invite is accepted
        private void OnGameRichPresenceJoinRequested(GameLobbyJoinRequested_t param) {
            steamNet = new SteamNetHandler();
            steamNet.JoinLobby(param.m_steamIDLobby);
        }

        public void CreateLobby(uint maxClients) {
            if(steamNet == null) steamNet = new SteamNetHandler();
            steamNet.CreateLobby(maxClients);
        }

        internal void RunCallbacks() {
            SteamAPI.RunCallbacks();
        }
    }
}
