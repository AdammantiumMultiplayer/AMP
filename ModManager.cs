using AMP.Data;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Handler;
using AMP.Network.Server;
using AMP.Overlay;
using AMP.Threading;
using AMP.Useless;
using AMP.Web;
using Steamworks;
using System;
using System.IO;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class ModManager : MonoBehaviour {
        public static ModManager instance;

        public static Server serverInstance;

        internal static Client clientInstance;
        internal static ClientSync clientSync;

        internal static GUIManager guiManager;
        internal static DiscordGUIManager discordGuiManager;
        internal static SteamGUIManager steamGuiManager;

        internal static bool discordNetworking = true;

        void Awake() {
            if (instance != null) {
                Destroy(gameObject);
                return;
            } else {
                instance = this;
                DontDestroyOnLoad(gameObject);

                Initialize();
            }
        }

        internal void Initialize() {
            Log.loggerType = Log.LoggerType.UNITY;

            //discordGuiManager = gameObject.AddComponent<DiscordGUIManager>();
            steamGuiManager = gameObject.AddComponent<SteamGUIManager>();
            guiManager = gameObject.AddComponent<GUIManager>();
            guiManager.enabled = false;

            GameConfig.Load(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "config.ini"));
            ServerConfig.Load(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "server.ini"));

            if(GameConfig.useBrowserIntegration) {
                WebSocketInteractor.Start();
            }

            gameObject.AddComponent<EventHandler>();

            EventManager.onLevelLoad += (levelData, eventTime) => {
                if(eventTime == EventTime.OnEnd) {
                    SecretLoader.DoLevelStuff();
                }
            };

            Log.Info($"<color=#FF8C00>[AMP] { Defines.MOD_NAME } has been initialized.</color>");
        }

        void Update() {
            Dispatcher.UpdateTick();
        }

        #if TEST_BUTTONS
        void OnGUI() {
            if(GUI.Button(new Rect(0, 0, 100, 30), "Dump scenes")) {
                LevelLayoutExporter.Export();
            }
        }
        #endif

        void OnApplicationQuit() {
            Exit();
        }

        void OnDestroy() {
            Exit();
        }

        private void Exit() {
            WebSocketInteractor.Stop();
            if(clientInstance != null) {
                StopClient();
            }
            if(serverInstance != null && serverInstance.isRunning) {
                StopHost();
            }
        }


        internal static void JoinServer(NetworkHandler networkHandler) {
            StopClient();

            clientInstance = new Client(networkHandler);
            clientInstance.nw.Connect();

            if(!clientInstance.nw.isConnected) {
                clientInstance = null;
            } else {
                if(instance.gameObject.GetComponent<ClientSync>() == null) {
                    clientSync = instance.gameObject.AddComponent<ClientSync>();
                }
                EventHandler.RegisterGlobalEvents();
                LevelFunc.EnableRespawning();
            }
        }

        internal static bool HostServer(uint maxPlayers, int port) {
            StopHost();

            serverInstance = new Server(maxPlayers, port);
            serverInstance.Start();

            if(serverInstance.isRunning) {
                return true;
            } else {
                serverInstance.Stop();
                serverInstance = null;
                throw new Exception("[Server] Server start failed. Check if an other program is running on that port.");
            }
        }

        public static bool HostDedicatedServer(uint maxPlayers, int port) {
            new Dispatcher();

            if(HostServer(maxPlayers, port)) {
                return true;
            }
            return false;
        }

        internal static void StopClient() {
            EventHandler.UnRegisterGlobalEvents();
            LevelFunc.DisableRespawning();

            clientInstance?.Disconnect();
            clientSync?.Stop();
            if(clientSync != null) Destroy(clientSync);

            clientInstance = null;
            clientSync = null;
        }

        public static void StopHost() {
            if(serverInstance == null) return;
            serverInstance.Stop();
            serverInstance = null;
        }

    }
}
