using AMP.Data;
using AMP.Discord;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Server;
using AMP.Overlay;
using AMP.Threading;
using AMP.Useless;
using AMP.Web;
using Netamite.Client.Definition;
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
                    WebSocketInteractor.InvokeMapInfo();
                }
            };

            Netamite.Logging.Log.loggerType = Netamite.Logging.Log.LoggerType.EVENT_ONLY;

            SteamIntegration.OnError += (e) => Log.Err(e);

            if(safeFile.modSettings.useSpaceWarMode) {
                CheckForSteamDll();
                SteamIntegration.Initialize(Defines.STEAM_APPID_SPACEWAR, false);
            } else {
                SteamIntegration.Initialize(Defines.STEAM_APPID, false);
            }
            SteamIntegration.OnOverlayJoin += OnSteamOverlayJoin;

            Log.Info($"<color=#FF8C00>[AMP] { Defines.MOD_NAME } has been initialized.</color>");
        }

        private string steamSdkPath = Path.Combine(Application.dataPath, "Plugins", "x86_64", "steam_api64.dll");
        private void CheckForSteamDll() {
            if(!File.Exists(steamSdkPath)) {
                Log.Warn("Couldn't find steam_api64.dll, extracting it now.");
                using(var file = new FileStream(steamSdkPath, FileMode.Create, FileAccess.Write)) {
                    file.Write(Properties.Resources.steam_api64, 0, Properties.Resources.steam_api64.Length);
                }
            }
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

            netClient.OnConnect += () => {
                clientInstance.StartSync();
                EventHandler.RegisterGlobalEvents();
                LevelFunc.EnableRespawning();

                WebSocketInteractor.SendServerDetails();
            };

            netClient.OnConnectionError += (error) => {
                Log.Err(Defines.CLIENT, error);
                StopClient();
            };

            netClient.OnDisconnect += (reason) => {
                Log.Err(Defines.CLIENT, reason);
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

        public static void HostDedicatedServer(uint maxPlayers, int port, string password, Action<string> callback) {
            HostServer(maxPlayers, port, password, callback);
        }

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

            WebSocketInteractor.ClearServerDetails();
        }

        public static void StopHost() {
            if(serverInstance == null) return;
            serverInstance.Stop();
            serverInstance = null;
        }

        /*
        Quaternion from;
        Quaternion to;
        float velocity;
        void Start() {
            from = Quaternion.Euler(0, 0, 0);
            to = Quaternion.Euler(135, 40, 50);
        }

        void Update() {
            from = from.SmoothDamp(to, ref velocity, 1f);
            Log.Debug(from.eulerAngles);
        }*/

    }
}
