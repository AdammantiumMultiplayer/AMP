using AMP.Data;
using AMP.Logging;
using AMP.Network;
using AMP.Network.Data.Sync;
using AMP.SupportFunctions;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Client.Implementation;
#if STEAM
using Netamite.Steam.Client;
using Netamite.Steam.Integration;
using Netamite.Steam.Server;
#endif
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
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

        private bool visible = true;

        internal Rect windowRect = new Rect(Screen.width - 220, Screen.height - 170, 210, 155);

        string title = "<color=#fffb00>" + Defines.MOD_NAME + "</color>";

        IEnumerator Start() {
            using(UnityWebRequest webRequest = UnityWebRequest.Get($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/list.php")) {
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
                        servers = JsonConvert.DeserializeObject<List<ServerInfo>>(webRequest.downloadHandler.text);

                        break;
                }
            }
        }

        public class ServerInfo {
#pragma warning disable CS0649
            public int id;
            public string servername;
            public string address;
            public short port;
            public string description;
            public string servericon;
            public byte official;
            public int players_max;
            public int players_connected;
            public string map;
            public string modus;
            public string version;
            public byte pvp;
            public byte static_map;
#pragma warning restore CS0649
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
                #if FULL_DEBUG
                //GUILayout.Label($"Active Creatures: {Creature.allActive.Count} / {Creature.all.Count}");
                if(ModManager.clientSync != null && ModManager.clientSync.syncData != null)
                    GUILayout.Label($"Active Items: {ModManager.clientSync.syncData.items.Where(item => item.Value.clientsideItem?.holder == null && item.Value.networkItem?.lastTime == 0 && !item.Value.networkItem.IsSending()).Count()} / {ModManager.clientSync.syncData.items.Count}");
                #endif

                if(GUI.Button(new Rect(10, 125, 180, 20), "Stop Server")) {
                    Log.Debug("User requested server stopping...");
                    ModManager.StopClient();
                    ModManager.StopHost();
                }
            } else if(ModManager.clientInstance != null) {
#if STEAM
                if(ModManager.clientInstance.netclient is SteamClient)
                    title = $"[ Client { Defines.FULL_MOD_VERSION} @ Steam ]";
                else
#endif
                    title = $"[ Client {Defines.FULL_MOD_VERSION} @ {join_ip} ]";

                if(ModManager.clientInstance.netclient.IsConnected) {
                    #if NETWORK_STATS
                    GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                    GUILayout.Label($"Ping: {ModManager.clientInstance.netclient.Ping}ms");
                    #endif
                    #if FULL_DEBUG
                    GUILayout.Label($"Active Items: {ModManager.clientSync.syncData.items.Count(item => item.Value.clientsideItem?.holder == null && item.Value.networkItem?.lastTime == 0 && !item.Value.networkItem.IsSending())} / {ModManager.clientSync.syncData.items.Count}");
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
#if STEAM
                            GUI.enabled = SteamIntegration.IsInitialized;
                            if(GUILayout.Button("Host Steam →")) {
                                menu = 1;
                            }
#endif
                            GUI.enabled = true;
#if STEAM
                            GUILayout.Label(SteamIntegration.IsInitialized ? " " : "Requires Steam Version");
#endif
                            GUILayout.Label(" ");
                            if(GUILayout.Button("Join Server →")) {
                                menu = 2;
                            }
                            if(GUILayout.Button("Host Server →")) {
                                menu = 3;
                            }
                            break;
#if STEAM
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
#endif
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


        private List<ServerInfo> servers;
        private Vector2 serverScroll;
        private void DrawServerBox() {
            if(ModManager.serverInstance != null) return;
            if(ModManager.clientInstance != null) return;
            if(servers == null || servers.Count == 0) return;
            if(LevelInfo.IsLoading()) return;

            GUI.Box(new Rect(windowRect.x - 210, windowRect.y, 200, 155), "Serverlist");
            serverScroll = GUI.BeginScrollView(new Rect(windowRect.x - 210, windowRect.y + 25, 200, 130), serverScroll, new Rect(0, 0, 180, servers.Count * 25), false, false);
            GUILayout.BeginVertical();

            int width = servers.Count <= 5 ? 200 : 180;

            foreach(ServerInfo info in servers) {
                if(GUILayout.Button(info.servername, GUILayout.Width(width))) {
                    join_ip = info.address;
                    JoinServer(info.address, info.port.ToString());
                }
            }

            GUILayout.EndVertical();
            GUI.EndScrollView();
        }


        private void DrawPlayerlist() {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;

            GUI.Box(new Rect(windowRect.x - 210, windowRect.y, 200, 155), "Playerlist");
            serverScroll = GUI.BeginScrollView(new Rect(windowRect.x - 210, windowRect.y + 25, 200, 130), serverScroll, new Rect(0, 0, 180, ModManager.clientSync.syncData.players.Count * 25), false, false);
            GUILayout.BeginVertical();

            int width = ModManager.clientSync.syncData.players.Count <= 5 ? 200 : 180;

            foreach(PlayerNetworkData pnd in ModManager.clientSync.syncData.players.Values) {
                if(GUILayout.Button(pnd.name, GUILayout.Width(width))) {
                    Player.local.Teleport(pnd.position, Quaternion.identity);
                }
            }

            GUILayout.EndVertical();
            GUI.EndScrollView();
        }

        private void OnGUI() {
            if(!visible) return;
            windowRect = GUI.Window(0, windowRect, PopulateWindow, title);
            DrawServerBox();
            #if TEST_BUTTONS
            DrawPlayerlist();
            #endif
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
            if(UnityEngine.InputSystem.Keyboard.current[Key.H].wasPressedThisFrame) {
                visible = !visible;
            }
        }


        public static void JoinServer(string address, string port, string password = "", bool save_cache = true) {
            if(int.Parse(port) <= 0) return;
            NetamiteClient client = new IPClient(address, int.Parse(port));
            client.ConnectToken = password;
            client.ClientName = UserData.GetUserName();

            ModManager.JoinServer(client, password);

            if(save_cache) {
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
                        JoinServer("127.0.0.1", port + "", password, false);
                    });
                } else {
                    ModManager.StopHost();
                }
            });
        }
#if STEAM
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
#endif
        private IEnumerator checkScreenSizeChanged() {
            Vector2 lastScreenSize = new Vector2(Screen.width, Screen.height);
            while(enabled) {
                try {
                    if(lastScreenSize.x != Screen.width && lastScreenSize.y != Screen.height) {
                        windowRect = new Rect(Screen.width - 210, Screen.height - 170, 200, 155);
                        lastScreenSize = new Vector2(Screen.width, Screen.height);
                    }
                }catch(Exception) { }

                yield return new WaitForSeconds(5);
            }
        }
    }
}
