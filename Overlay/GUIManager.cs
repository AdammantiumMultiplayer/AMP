using AMP.Data;
using AMP.Logging;
using AMP.Network;
using AMP.Network.Handler;
using AMP.SteamNet;
using System.Collections;
using ThunderRoad;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace AMP.Overlay {
    internal class GUIManager : MonoBehaviour {
        public string join_ip         = ModManager.safeFile.inputCache.join_ip;
        public string join_port       = ModManager.safeFile.inputCache.join_port.ToString();
        public string join_password   = ModManager.safeFile.inputCache.join_password;

        public string host_port       = ModManager.safeFile.inputCache.host_port.ToString();
        public string host_password   = ModManager.safeFile.inputCache.host_password;
        public uint   host_maxPlayers = ModManager.safeFile.inputCache.host_max_players;

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

        private void PopulateWindow(int id) {
            if(ModManager.serverInstance != null) {
                title = $"[ Server { Defines.MOD_VERSION } | Port: { host_port } ]";

                GUILayout.Label($"Players: {ModManager.serverInstance.connectedClients} / {host_maxPlayers}");
                //GUILayout.Label("Creatures: " + Creature.all.Count + " (Active: " + Creature.allActive.Count + ")");
                //GUILayout.Label($"Items: {ModManager.serverInstance.spawnedItems}\n"
                //               +$"Creatures: {ModManager.serverInstance.spawnedCreatures}"
                //               );

                #if NETWORK_STATS
                GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                #endif
            } else if(ModManager.clientInstance != null) {
                title = $"[ Client { Defines.MOD_VERSION } @ { join_ip } ]";

                if(ModManager.clientInstance.nw.isConnected) {
                    #if NETWORK_STATS
                    GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                    #endif
                } else {
                    GUILayout.Label("Connecting...");
                }

                if(GUI.Button(new Rect(10, 105, 180, 20), "Disconnect")) {
                    Log.Debug("User requested disconnect...");
                    ModManager.StopClient();
                }
            } else {
                title = "<color=#fffb00>" + Defines.MOD_NAME + "</color>";

                if(Level.current == null || !Level.current.loaded || Level.current.data.id == "CharacterSelection") {
                    GUI.Label(new Rect(10, 60, 180, 50), "Wait for the level to finish loading...");
                } else {
                    switch(menu) {
                        case 0: // Overview
                            if(GUILayout.Button("Host Steam →")) {
                                menu = 1;
                            }
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
            for(int i = 0; i < servers.GetLength(0); i++) {
                if(servers[i, 0] == null || servers[i, 1].Length == 0) continue;
                if(GUILayout.Button(servers[i, 0], GUILayout.Width(180))) {
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
            if(Keyboard.current[Key.L].wasPressedThisFrame) {
                windowRect = new Rect(Screen.width - 210, Screen.height - 170, 200, 155);
            }

            if(Keyboard.current[Key.K].wasPressedThisFrame) {
                HostSteam(10);
            }
        }


        public static void JoinServer(string ip, string port, string password = "") {
            if(int.Parse(port) <= 0) return;
            NetworkHandler networkHandler = new SocketHandler(ip, int.Parse(port));

            ModManager.JoinServer(networkHandler, password);


            ModManager.safeFile.inputCache.join_ip       = ip;
            ModManager.safeFile.inputCache.join_port     = ushort.Parse(port);
            ModManager.safeFile.inputCache.join_password = password;
            ModManager.safeFile.Save();
        }

        public static void HostServer(uint maxPlayers, int port, string password = "") {
            if(ModManager.HostServer(maxPlayers, port, password)) {
                ModManager.JoinServer(new SocketHandler("127.0.0.1", port), password);

                ModManager.safeFile.inputCache.host_max_players = maxPlayers;
                ModManager.safeFile.inputCache.host_port        = (ushort) port;
                ModManager.safeFile.inputCache.host_password    = password;
                ModManager.safeFile.Save();
            }
        }

        public static void HostSteam(uint maxPlayers) {
            if(ModManager.HostSteamServer(maxPlayers)) {
                ModManager.JoinServer(SteamIntegration.Instance.steamNet);
            }
        }
    }
}
