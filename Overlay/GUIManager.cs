using AMP.Data;
using AMP.Logging;
using AMP.Network;
using AMP.SupportFunctions;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Client.Implementation;
using Netamite.Steam.Client;
using Netamite.Steam.Integration;
using Netamite.Steam.Server;
using System.Collections;
using ThunderRoad;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace AMP.Overlay {
    internal class GUIManager : MonoBehaviour {
        public string join_ip                 = ModManager.safeFile.inputCache.join_address;
        public string join_port               = ModManager.safeFile.inputCache.join_port.ToString();
        public string join_password           = ModManager.safeFile.inputCache.join_password;

        public string host_port               = ModManager.safeFile.inputCache.host_port.ToString();
        public string host_password           = ModManager.safeFile.inputCache.host_password;
        public uint   host_maxPlayers         = ModManager.safeFile.inputCache.host_max_players;

        public bool   host_steam_friends_only = ModManager.safeFile.inputCache.host_steam_friends_only;

        public int menu = 0;

        internal Rect windowRect = new Rect(Screen.width - 210, Screen.height - 170, 200, 155);

        string title = "<color=#fffb00>" + Defines.MOD_NAME + "</color>";

        IEnumerator Start() {
            using(UnityWebRequest webRequest = UnityWebRequest.Get("https://bns.devforce.de/bns.txt")) {
                yield return webRequest.SendWebRequest();

                switch(webRequest.result) {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Log.Err(Defines.AMP, $"Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Log.Err(Defines.AMP, $"HTTP Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        string[] splits = webRequest.downloadHandler.text.Split(new string[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

                        servers = new string[splits.Length, 3];
                        for(int i = 0; i < splits.Length; i++) {
                            string[] parts = splits[i].Split(';');
                            if(parts.Length == 3) {
                                servers[i, 0] = parts[0];
                                servers[i, 1] = parts[1];
                                servers[i, 2] = parts[2];
                            }
                        }
                        break;
                }
            }
        }

        void Awake() {
            StartCoroutine(checkScreenSizeChanged());
        }

        private void PopulateWindow(int id) {
            if(ModManager.serverInstance != null) {
                if(host_port.Length > 1)
                    title = $"[ Server { Defines.FULL_MOD_VERSION} | Port: { host_port } ]";
                else 
                    title = $"[ Server {Defines.FULL_MOD_VERSION} ]";

                GUILayout.Label($"Players: {ModManager.serverInstance.connectedClients} / {host_maxPlayers}");
                //GUILayout.Label("Creatures: " + Creature.all.Count + " (Active: " + Creature.allActive.Count + ")");
                //GUILayout.Label($"Items: {ModManager.serverInstance.spawnedItems}\n"
                //               +$"Creatures: {ModManager.serverInstance.spawnedCreatures}"
                //               );

                #if NETWORK_STATS
                GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                GUILayout.Label($"Ping: {ModManager.clientInstance?.netclient?.Ping}ms");
                #endif

                if(GUI.Button(new Rect(10, 125, 180, 20), "Stop Server")) {
                    Log.Debug("User requested server stopping...");
                    ModManager.StopClient();
                    ModManager.StopHost();
                }
            } else if(ModManager.clientInstance != null) {
                if(ModManager.clientInstance.netclient is SteamClient)
                    title = $"[ Client { Defines.FULL_MOD_VERSION} @ Steam ]";
                else
                    title = $"[ Client {Defines.FULL_MOD_VERSION} @ {join_ip} ]";

                if(ModManager.clientInstance.netclient.IsConnected) {
                    #if NETWORK_STATS
                    GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                    GUILayout.Label($"Ping: {ModManager.clientInstance.netclient.Ping}ms");
                    #endif
                } else {
                    GUILayout.Label("Connecting...");
                }

                if(GUI.Button(new Rect(10, 125, 180, 20), "Disconnect")) {
                    Log.Debug("User requested disconnect...");
                    ModManager.StopClient();
                }
            } else {
                title = "<color=#fffb00>" + Defines.MOD_NAME + "</color>";

                if(LevelInfo.IsLoading()) {
                    GUI.Label(new Rect(10, 60, 180, 50), "Wait for the level to finish loading...");
                } else {
                    switch(menu) {
                        case 0: // Overview
                            GUI.enabled = SteamIntegration.IsInitialized;
                            if(GUILayout.Button("Host Steam →")) {
                                menu = 1;
                            }
                            GUI.enabled = true;
                            GUILayout.Label(SteamIntegration.IsInitialized ? " " : "Requires Steam Version");
                            GUILayout.Label(" ");
                            if(GUILayout.Button("Join Server →")) {
                                menu = 2;
                            }
                            if(GUILayout.Button("Host Server →")) {
                                menu = 3;
                            }
                            break;

                        case 1: // Steam Host
                            if(GUI.Button(new Rect(10, 25, 180, 20), "← Back")) {
                                menu = 0;
                            }

                            GUI.Label(new Rect(15, 50, 30, 20), "Max:");

                            host_maxPlayers = (uint)GUI.HorizontalSlider(new Rect(53, 55, 110, 20), host_maxPlayers, 2, Defines.MAX_PLAYERS);
                            GUI.Label(new Rect(175, 50, 30, 20), host_maxPlayers.ToString());

                            host_steam_friends_only = !GUI.Toggle(new Rect(15, 75, 200, 20), !host_steam_friends_only, "Public");
                            host_steam_friends_only = GUI.Toggle(new Rect(15, 100, 200, 20), host_steam_friends_only, "Friends only");

                            if(GUI.Button(new Rect(10, 125, 180, 20), "Host Steam")) {
                                HostSteam(host_maxPlayers);
                            }
                            break;

                        case 2: // Join Menu
                            if(GUI.Button(new Rect(10, 25, 180, 20), "← Back")) {
                                menu = 0;
                            }

                            GUI.Label(new Rect(15, 50, 30, 20), "IP:");
                            GUI.Label(new Rect(15, 75, 30, 20), "Port:");
                            GUI.Label(new Rect(15, 100, 30, 20), "Password:");

                            join_ip = GUI.TextField(new Rect(50, 50, 140, 20), join_ip);
                            join_port = GUI.TextField(new Rect(50, 75, 140, 20), join_port);
                            join_password = GUI.PasswordField(new Rect(50, 100, 140, 20), join_password, '#');

                            if(GUI.Button(new Rect(10, 125, 180, 20), "Join Server")) {
                                JoinServer(join_ip, join_port, join_password);
                            }
                            break;
                        
                        case 3: // Host Menu
                            if(GUI.Button(new Rect(10, 25, 180, 20), "← Back")) {
                                menu = 0;
                            }

                            GUI.Label(new Rect(15, 50, 30, 20), "Max:");
                            GUI.Label(new Rect(15, 75, 30, 20), "Port:");
                            GUI.Label(new Rect(15, 100, 30, 20), "Password:");

                            host_maxPlayers = (uint)GUI.HorizontalSlider(new Rect(53, 55, 110, 20), host_maxPlayers, 2, Defines.MAX_PLAYERS);
                            GUI.Label(new Rect(175, 50, 30, 20), host_maxPlayers.ToString());
                            host_port = GUI.TextField(new Rect(50, 75, 140, 20), host_port);
                            host_password = GUI.PasswordField(new Rect(50, 100, 140, 20), host_password, '#', 10);

                            if(GUI.Button(new Rect(10, 125, 180, 20), "Host Server")) {
                                HostServer(host_maxPlayers, int.Parse(host_port), host_password);
                            }
                            break;

                        default:
                            if(GUI.Button(new Rect(10, 25, 180, 20), "← Back")) {
                                menu = 0;
                            }
                            break;
                    }
                }
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }


        private string[,] servers;
        private Vector2 serverScroll;
        private void DrawServerBox() {
            if(ModManager.serverInstance != null) return;
            if(ModManager.clientInstance != null) return;
            if(servers == null || servers.GetLength(0) == 0) return;
            if(Level.current == null || !Level.current.loaded || Level.current.data.id == "CharacterSelection") return;

            GUI.Box(new Rect(windowRect.x - 210, windowRect.y, 200, 155), "Serverlist");
            serverScroll = GUI.BeginScrollView(new Rect(windowRect.x - 210, windowRect.y + 25, 200, 130), serverScroll, new Rect(0, 0, 180, servers.GetLength(0) * 25), false, false);
            GUILayout.BeginVertical();

            int width = servers.GetLength(0) <= 5 ? 200 : 180;

            for(int i = 0; i < servers.GetLength(0); i++) {
                if(servers[i, 0] == null || servers[i, 1].Length == 0) continue;
                if(GUILayout.Button(servers[i, 0], GUILayout.Width(width))) {
                    join_ip = servers[i, 1];
                    JoinServer(servers[i, 1], servers[i, 2]);
                }
            }
            GUILayout.EndVertical();
            GUI.EndScrollView();
        }
        private void OnGUI() {
            windowRect = GUI.Window(0, windowRect, PopulateWindow, title);
            DrawServerBox();
        }

        #if NETWORK_STATS
        float time = 0;
        void FixedUpdate() {
            time += Time.fixedDeltaTime;
            if(time > 1) {
                NetworkStats.UpdatePacketCount();
                time = 0;
            }
        }
        #endif

        void Update() {
            if(UnityEngine.InputSystem.Keyboard.current[Key.L].wasPressedThisFrame) {
                windowRect = new Rect(Screen.width - 210, Screen.height - 170, 200, 155);
            }
        }


        public static void JoinServer(string address, string port, string password = "") {
            if(int.Parse(port) <= 0) return;
            NetamiteClient client = new IPClient(address, int.Parse(port));
            client.ConnectToken = password;
            client.ClientName = UserData.GetUserName();

            ModManager.JoinServer(client, password);

            if(!address.Equals("127.0.0.1")) {
                ModManager.safeFile.inputCache.join_address  = address;
                ModManager.safeFile.inputCache.join_port     = ushort.Parse(port);
                ModManager.safeFile.inputCache.join_password = password;
                ModManager.safeFile.Save();
            }
        }

        public static void HostServer(uint maxPlayers, int port, string password = "") {
            ModManager.HostServer(maxPlayers, port, password, (error) => {
                if(error == null) {
                    ModManager.safeFile.inputCache.host_max_players = maxPlayers;
                    ModManager.safeFile.inputCache.host_port = (ushort)port;
                    ModManager.safeFile.inputCache.host_password = password;
                    ModManager.safeFile.Save();

                    Dispatcher.Enqueue(() => {
                        JoinServer("127.0.0.1", port + "", password);
                    });
                } else {
                    ModManager.StopHost();
                }
            });
        }

        public static void HostSteam(uint maxPlayers) {
            ModManager.HostSteamServer(maxPlayers, (error) => {
                if(error == null) {
                    ModManager.safeFile.inputCache.host_max_players = maxPlayers;
                    ModManager.safeFile.Save();

                    Dispatcher.Enqueue(() => {
                        ModManager.JoinSteam((ulong) ((SteamServer) ModManager.serverInstance.netamiteServer).currentLobby.LobbyId);
                    });
                } else {
                    ModManager.StopHost();
                }
            });
        }

        private IEnumerator checkScreenSizeChanged() {
            Vector2 lastScreenSize = new Vector2(Screen.width, Screen.height);
            while(enabled) {
                if(lastScreenSize.x != Screen.width && lastScreenSize.y != Screen.height) {
                    windowRect = new Rect(Screen.width - 210, Screen.height - 170, 200, 155);
                    lastScreenSize = new Vector2(Screen.width, Screen.height);
                }

                yield return new WaitForSeconds(5);
            }
        }
    }
}
