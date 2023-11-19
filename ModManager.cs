using AMP.Data;
using AMP.Discord;
#if TEST_BUTTONS
using AMP.Export;
#endif
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Data;
using AMP.Network.Server;
using AMP.Overlay;
using AMP.Threading;
using AMP.UI;
using AMP.Useless;
using AMP.Web;
using Netamite.Client.Definition;
using Netamite.Server.Implementation;
using Netamite.Steam.Integration;
using Netamite.Steam.Server;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using ThunderRoad;
using UnityEngine;
using SteamClient = Netamite.Steam.Client.SteamClient;

namespace AMP {
    public class ModManager : ThunderBehaviour {
        public static ModManager instance;

        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update | ManagedLoops.FixedUpdate;

        public static Server serverInstance;

        internal static Client clientInstance;
        internal static ClientSync clientSync;

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

        internal List<IngameModUI.SteamInvite> invites = new List<IngameModUI.SteamInvite>();

        internal uint currentAppId = 0;
        internal void Initialize() {
            Log.loggerType = Log.LoggerType.UNITY;

            Netamite.Logging.Log.onLogMessage += (type, message) => {
                Log.Msg((Log.Type) type, message);
            };

            safeFile = SafeFile.Load(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "config.json"));

            if(safeFile.modSettings.useBrowserIntegration) {
                WebSocketInteractor.Start();
            }

            gameObject.AddComponent<EventHandler>();
            gameObject.AddComponent<NetworkComponentManager>();
            guiManager = gameObject.AddComponent<GUIManager>();

            EventManager.onLevelLoad += (levelData, eventTime) => {
                if(eventTime == EventTime.OnEnd) {
                    SecretLoader.DoLevelStuff();
                    DiscordIntegration.Instance.UpdateActivity();
                    WebSocketInteractor.InvokeMapInfo();
                }
            };

            SetupNetamite();

            SteamIntegration.OnError += (e) => Log.Err(e);
            SteamIntegration.OnInitialized += () => {
                Log.Info("AMP", "Steam connection initialized.");
            };

            CheckForSteamDll();
            if(safeFile.modSettings.useSpaceWarMode) {
                Environment.SetEnvironmentVariable("SteamAppId", Defines.STEAM_APPID_SPACEWAR.ToString());
                Environment.SetEnvironmentVariable("SteamGameId", Defines.STEAM_APPID_SPACEWAR.ToString());

                CheckForSteamId(Defines.STEAM_APPID_SPACEWAR);
                SteamIntegration.Initialize(Defines.STEAM_APPID_SPACEWAR, false);
                currentAppId = Defines.STEAM_APPID_SPACEWAR;
            } else {
                CheckForSteamId(Defines.STEAM_APPID);
                SteamIntegration.Initialize(Defines.STEAM_APPID, false);
                currentAppId = Defines.STEAM_APPID;
            }
            SteamIntegration.OnOverlayJoin += OnSteamOverlayJoin;
            SteamIntegration.OnInviteReceived += OnSteamInviteReceived;

            Log.Info($"<color=#FF8C00>[AMP] { Defines.MOD_NAME } has been initialized.</color>");
        }

        private void OnSteamInviteReceived(ulong appId, ulong userId, ulong lobbyId) {
            Log.Warn(Defines.AMP, "Invite through steam: " + appId + " " + userId + " " + lobbyId);
            //JoinSteam(lobbyId);
            while(invites.Count > 5) {
                invites.RemoveAt(0);
            }

            string name = SteamFriends.GetFriendPersonaName((CSteamID) userId);
            invites.Add(new IngameModUI.SteamInvite(name, userId, lobbyId));

            if(IngameModUI.currentUI != null) StartCoroutine(IngameModUI.currentUI.LoadInvites());
        }

        public static void SetupNetamite() {
            Netamite.Netamite.SetClientinfoType(typeof(ClientData));
            Netamite.Logging.Log.loggerType = Netamite.Logging.Log.LoggerType.EVENT_ONLY;
            Netamite.Netamite.Initialize();
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

        private string steamAppIdPath = Path.Combine(Application.dataPath, "..", "steam_appid.txt");
        private void CheckForSteamId(uint id) {
            if(!File.Exists(steamAppIdPath)) {
                Log.Warn("Couldn't find steam_appid.txt, adding it now.");
                File.WriteAllText(steamAppIdPath, id.ToString());
            }/* else {
                if(!File.ReadAllText(steamAppIdPath).Trim().Contains(id.ToString())) {
                    Log.Warn("Overwriting steam_appid.txt...");
                    File.WriteAllText(steamAppIdPath, id.ToString());
                }
            }*/
        }

        protected override void ManagedUpdate() {
            Dispatcher.UpdateTick();
        }

        protected override void ManagedFixedUpdate() {
            DiscordIntegration.Instance.RunCallbacks();
            SteamIntegration.RunCallbacks();
        }

        internal static GUIManager guiManager;
        internal void UpdateOnScreenMenu() {
            if(ModLoader._ShowOldMenu && guiManager != null) {
                guiManager.enabled = true;
            } else if(!ModLoader._ShowOldMenu && guiManager != null) {
                guiManager.enabled = false;
            }
        }

        internal static IngameModUI ingameUI;
        internal void UpdateIngameMenu() {
            if(ModLoader._ShowMenu && ingameUI == null) {
                GameObject obj = new GameObject("IngameUI");
                ingameUI = obj.AddComponent<IngameModUI>();

                obj.transform.position = Player.local.head.transform.position + new Vector3(Player.local.head.transform.forward.x, 0, Player.local.head.transform.forward.z) * 1;
                obj.transform.eulerAngles = new Vector3(0, Player.local.head.transform.eulerAngles.y, 0);
            } else if(!ModLoader._ShowMenu && ingameUI != null) {
                ingameUI.CloseMenu();
            }
        }

        #if TEST_BUTTONS
        void OnGUI() {
            if(GUI.Button(new Rect(0, 0, 100, 30), "Dump scenes")) {
                LevelLayoutExporter.Export();
            }
            if(GUI.Button(new Rect(0, 35, 100, 30), "Test Text")) {
                DisplayMessage.MessageData messageData = new DisplayMessage.MessageData("Test", "", "", "", 1);
                messageData.anchorType = MessageAnchorType.Head;
                DisplayMessage.instance.ShowMessage(messageData);
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
                StopClientImmediatly();
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
                Dispatcher.Enqueue(() => { 
                    clientInstance.StartSync();
                    EventHandler.RegisterGlobalEvents();
                    LevelFunc.EnableRespawning();
                });
                WebSocketInteractor.SendServerDetails();
            };

            netClient.OnConnectionError += (error) => {
                Log.Info(Defines.CLIENT, error);
                StopClient();
            };

            netClient.OnDisconnect += (reason) => {
                Log.Info(Defines.CLIENT, "Disconnected: " + reason);
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

            Dispatcher.Enqueue(() => {
                StopClientImmediatly();
            });
        }

        internal static void StopClientImmediatly() {
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

        public static void Stop() {
            StopClient();
            StopHost();
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
