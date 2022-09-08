using UnityEngine;
using AMP.Network.Client;
using AMP.Threading;
using System;
using System.Reflection;
using AMP.Network.Server;
using ThunderRoad;
using UnityEngine.InputSystem;
using AMP.Logging;
using System.IO;
using AMP.Data;
using AMP.Network.Handler;
using static ThunderRoad.GameData;
using AMP.Useless;
using AMP.Export;
using System.Threading;

namespace AMP {
    public class ModManager : MonoBehaviour {
        public static ModManager instance;

        public static Server serverInstance;

        public static Client clientInstance;
        public static ClientSync clientSync;

        public static string MOD_DEV_STATE = "Alpha";
        public static string MOD_VERSION = "";
        public static string MOD_NAME = "";

        public static GUIManager guiManager;
        public static DiscordGUIManager discordGuiManager;

        public static bool discordNetworking = true;

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

        public static void ReadVersion() {
            try {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString().TrimEnd(new char[] { '.', '0' });
                if(version.Split('.').Length == 1) {
                    version += ".0";
                }
                if(version.Split('.').Length == 2) {
                    version += ".0";
                }

                MOD_VERSION = MOD_DEV_STATE + " " + version;
            } catch(Exception) { // With other languages the first one seems to screw up
                MOD_VERSION = MOD_DEV_STATE + " [VERSION ERROR] ";
            }
            MOD_NAME = "AMP " + MOD_VERSION;
        }


        void Initialize() {
            ReadVersion();

            discordGuiManager = gameObject.AddComponent<DiscordGUIManager>();
            //guiManager = gameObject.AddComponent<GUIManager>();

            GameConfig.Load(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "config.ini"));
            ServerConfig.Load(Path.Combine(Application.streamingAssetsPath, "Mods", "MultiplayerMod", "server.ini"));

            gameObject.AddComponent<EventHandler>();

            EventManager.onLevelLoad += (levelData, eventTime) => {
                if(eventTime == EventTime.OnEnd) {
                    SecretLoader.DoLevelStuff();
                }
            };

            Log.Info($"<color=#FF8C00>[AMP] {MOD_NAME} has been initialized.</color>");
        }

        float time = 0f;
        void FixedUpdate() {
            time += Time.fixedDeltaTime;
            if(time > 1) {
                time = 0;
                if(serverInstance != null) serverInstance.UpdatePacketCount();
            }
        }

        private const float movementSpeed = 1f;
        private bool reset = false;
        void Update() {
            Dispatcher.UpdateTick();

            #if KEYBOARD_MOVEMENT
            Vector3 direction = Vector3.zero;
            if(Keyboard.current[Key.Numpad8].isPressed) {
                direction.z = movementSpeed;
            }
            if(Keyboard.current[Key.Numpad4].isPressed) {
                direction.x = -movementSpeed;
            }
            if(Keyboard.current[Key.Numpad5].isPressed) {
                direction.z = -movementSpeed;
            }
            if(Keyboard.current[Key.Numpad6].isPressed) {
                direction.x = movementSpeed;
            }
            if(direction.sqrMagnitude > 0.1f) {
                if(Keyboard.current[Key.RightShift].isPressed) {
                    direction *= 2;
                }
                Player.local.locomotion.Move(Player.local.transform.TransformDirection(direction));
                reset = true;
            } else if(reset) {
                Player.local.locomotion.MoveStop();
                reset = false;
            }

            if(Keyboard.current[Key.Numpad0].isPressed) {
                Player.local.locomotion.Jump(true);
            }


            if(Keyboard.current[Key.Numpad7].isPressed) {
                Player.local.transform.Rotate(0, -50 * Time.deltaTime, 0);
            } else if(Keyboard.current[Key.Numpad9].isPressed) {
                Player.local.transform.Rotate(0, 50 * Time.deltaTime, 0);
            }
            #endif
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
            if(clientInstance != null) {
                StopClient();
            }
            if(serverInstance != null && serverInstance.isRunning) {
                StopHost();
            }
        }


        public static void JoinServer(NetworkHandler networkHandler) {
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
            }
        }

        public static bool HostServer(uint maxPlayers, int port) {
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
            Dispatcher dispatcher = new Dispatcher();
            Dispatcher.current = dispatcher;
            
            discordNetworking = false;

            if(HostServer(maxPlayers, port)) {
                return true;
            }
            return false;
        }

        public static void StopClient() {
            if(clientInstance == null) return;
            clientInstance.Disconnect();
            if(clientSync != null) {
                clientSync.Stop();
                Destroy(clientSync);
                clientSync = null;
            }
            clientInstance = null;

            EventHandler.UnRegisterGlobalEvents();
        }

        public static void StopHost() {
            if(serverInstance == null) return;
            serverInstance.Stop();
            serverInstance = null;
        }

    }
}
