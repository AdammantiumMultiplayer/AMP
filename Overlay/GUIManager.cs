﻿using AMP.Data;
using AMP.Logging;
using AMP.Network;
using AMP.Network.Handler;
using AMP.Steam;
using Steamworks;
using System.Collections;
using ThunderRoad;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace AMP.Overlay {
    internal class GUIManager : MonoBehaviour {
        public string ip = "127.0.0.1";
        public uint maxPlayers = 4;
        public string port = "26950";
        public string password = "";

        public string host_port = "26950";
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
                        string[] splits = webRequest.downloadHandler.text.Split('\n');

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

                GUILayout.Label($"Players: {ModManager.serverInstance.connectedClients} / {maxPlayers}");
                //GUILayout.Label("Creatures: " + Creature.all.Count + " (Active: " + Creature.allActive.Count + ")");
                //GUILayout.Label($"Items: {ModManager.serverInstance.spawnedItems}\n"
                //               +$"Creatures: {ModManager.serverInstance.spawnedCreatures}"
                //               );

                #if NETWORK_STATS
                GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                #endif
            } else if(ModManager.clientInstance != null) {
                title = $"[ Client { Defines.MOD_VERSION } @ { ip } ]";

                if(ModManager.clientInstance.nw.isConnected) {
                    #if NETWORK_STATS
                    GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                    #endif
                } else {
                    GUILayout.Label("Connecting...");
                }

                if(GUI.Button(new Rect(10, 105, 180, 20), "Disconnect")) {
                    ModManager.StopClient();
                }
            } else {
                title = "<color=#fffb00>" + Defines.MOD_NAME + "</color>";

                //if(GUI.Button(new Rect(10, 25, 180, 20), "Use Steam")) {
                //    ModManager.steamGuiManager.enabled = true;
                //    ModManager.guiManager.enabled = false;
                //    ModManager.steamGuiManager.windowRect = ModManager.guiManager.windowRect;
                //}

                if(Level.current == null || !Level.current.loaded || Level.current.data.id == "CharacterSelection") {
                    GUI.Label(new Rect(10, 60, 180, 50), "Wait for the level to finish loading...");
                } else {
                    if(GUI.Button(new Rect(10, 25, 85, 20), menu == 0 ? "[ Join ]" : "Join")) {
                        menu = 0;
                    }
                    if(GUI.Button(new Rect(105, 25, 85, 20), menu == 1 ? "[ Host ]" : "Host")) {
                        menu = 1;
                    }

                    if(menu == 0) {
                        GUI.Label(new Rect(15, 50, 30, 20), "IP:");
                        GUI.Label(new Rect(15, 75, 30, 20), "Port:");
                        GUI.Label(new Rect(15, 100, 30, 20), "Password:");

                        ip = GUI.TextField(new Rect(50, 50, 140, 20), ip);
                        port = GUI.TextField(new Rect(50, 75, 140, 20), port);
                        password = GUI.PasswordField(new Rect(50, 100, 140, 20), password, '#');

                        if(GUI.Button(new Rect(10, 125, 180, 20), "Join Server")) {
                            JoinServer(ip, port, password);
                        }
                    } else {
                        GUI.Label(new Rect(15, 75, 30, 20), "Max:");
                        GUI.Label(new Rect(15, 100, 30, 20), "Port:");

                        maxPlayers = (uint) GUI.HorizontalSlider(new Rect(53, 80, 110, 20), maxPlayers, 2, ServerConfig.maxPlayers);
                        GUI.Label(new Rect(175, 75, 30, 20), maxPlayers.ToString());
                        host_port = GUI.TextField(new Rect(50, 100, 140, 20), host_port);

                        if(GUI.Button(new Rect(10, 125, 180, 20), "Start Server")) {
                            HostServer(maxPlayers, int.Parse(host_port));
                        }
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
                if(GUILayout.Button(servers[i, 0], GUILayout.Width(180))) {
                    ip = servers[i, 1];
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
            SteamAPI.RunCallbacks();
        }


        public static void JoinServer(string ip, string port, string password = "") {
            if(int.Parse(port) <= 0) return;
            NetworkHandler networkHandler = new SocketHandler(ip, int.Parse(port));

            ModManager.JoinServer(networkHandler, password);
        }

        public static void HostServer(uint maxPlayers, int port) {
            if(ModManager.HostServer(maxPlayers, port)) {
                ModManager.JoinServer(new SocketHandler("127.0.0.1", port));
            }
        }
    }
}
