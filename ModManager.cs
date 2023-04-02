using AMP.Data;
using AMP.Discord;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Server;
using AMP.Overlay;
using AMP.SupportFunctions;
using AMP.Threading;
using AMP.Useless;
using AMP.Web;
using Netamite.Client.Definition;
using Netamite.Client.Implementation;
using Netamite.Server.Implementation;
using Netamite.Steam.Client;
using Netamite.Steam.Integration;
using Netamite.Steam.Server;
using System;
using System.IO;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class ModManager : ThunderBehaviour {
        public static ModManager instance;

        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update | ManagedLoops.FixedUpdate;

        public static Server serverInstance;

        internal static Client clientInstance;
        internal static ClientSync clientSync;

        internal static GUIManager guiManager;

        internal static bool discordNetworking = true;

        public static SafeFile safeFile;

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

            Netamite.Logging.Log.onLogMessage += (type, message) => {
                Log.Msg((Log.Type) type, message);
            };

            safeFile = SafeFile.Load(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "config.json"));

            guiManager = gameObject.AddComponent<GUIManager>();

            if(safeFile.modSettings.useBrowserIntegration) {
                WebSocketInteractor.Start();
            }

            gameObject.AddComponent<EventHandler>();

            EventManager.onLevelLoad += (levelData, eventTime) => {
                if(eventTime == EventTime.OnEnd) {
                    SecretLoader.DoLevelStuff();
                    DiscordIntegration.Instance.UpdateActivity();
                }
            };

            SteamIntegration.Initialize(Defines.STEAM_APPID, false);
            SteamIntegration.OnOverlayJoin += OnSteamOverlayJoin;

            Log.Info($"<color=#FF8C00>[AMP] { Defines.MOD_NAME } has been initialized.</color>");
        }

        protected override void ManagedUpdate() {
            Dispatcher.UpdateTick();
        }

        protected override void ManagedFixedUpdate() {
            DiscordIntegration.Instance.RunCallbacks();
            SteamIntegration.RunCallbacks();
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
            if(serverInstance != null && serverInstance.netamiteServer.IsRunning) {
                StopHost();
            }
        }


        internal static void JoinServer(NetamiteClient netClient, string password = "") {
            StopClient();

            if(instance.gameObject.GetComponent<ClientSync>() != null) {
                Destroy(instance.gameObject.GetComponent<ClientSync>());
            }
            clientSync = instance.gameObject.AddComponent<ClientSync>();

            clientInstance = new Client(netClient);

            netClient.PingDelay = 15000;

            netClient.OnConnect += () => {
                clientInstance.StartSync();
                EventHandler.RegisterGlobalEvents();
                LevelFunc.EnableRespawning();
            };

            netClient.OnConnectionError += (error) => {
                StopClient();
            };

            netClient.OnDisconnect += (reason) => {
                StopClient();
            };

            clientInstance.Connect(password);
        }

        internal static void HostServer(uint maxPlayers, int port, string password, Action<string> callback) {
            StopClient();
            StopHost();

            IPServer server = new IPServer("0.0.0.0", (short) port, (int) maxPlayers);
            server.ConnectToken = password;

            serverInstance = new Server();

            server.OnStart += (ms) => {
                callback(null);
            };
            server.OnStartupError += (error) => {
                callback(error);
            };

            serverInstance.Start(server);
        }

        internal static void HostSteamServer(uint maxPlayers, Action<string> callback) {
            StopClient();
            StopHost();

            SteamServer steamServer = new SteamServer((int) maxPlayers);

            serverInstance = new Server();

            steamServer.OnStart += (ms) => {
                callback(null);
            };
            steamServer.OnStartupError += (error) => {
                callback(error);
            };

            serverInstance.Start(steamServer);
        }

        private void OnSteamOverlayJoin(SteamClient client) {
            JoinServer(client);
        }

        public static void JoinSteam(ulong lobbyId) {
            SteamClient client = new SteamClient(lobbyId);
            JoinServer(client);
        }

        //public static bool HostDedicatedServer(uint maxPlayers, int port, string password = "") {
        //    if(HostServer(maxPlayers, port, password)) {
        //        return true;
        //    }
        //    return false;
        //}

        internal static void StopClient() {
            if(clientInstance == null) return;

            EventHandler.UnRegisterGlobalEvents();
            LevelFunc.DisableRespawning();

            clientInstance?.Disconnect();
            clientSync?.Stop();
            if(clientSync != null) Destroy(clientSync);

            clientInstance = null;
            clientSync = null;

            DiscordIntegration.Instance.UpdateActivity();
        }

        public static void StopHost() {
            if(serverInstance == null) return;
            serverInstance.Stop();
            serverInstance = null;
        }

    }
}
