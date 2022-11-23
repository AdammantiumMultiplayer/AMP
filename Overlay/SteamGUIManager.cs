using AMP.Data;
using AMP.DiscordNetworking;
using AMP.Logging;
using AMP.Network;
using AMP.Network.Data;
using AMP.Network.Packets;
using AMP.SupportFunctions;
using Discord;
using System;
using System.IO;
using ThunderRoad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AMP.Overlay {
    internal class SteamGUIManager : MonoBehaviour {

        public string lobbyId = "";
        public int maxPlayers = 4;
        public int menu = 0;

        internal Rect windowRect = new Rect(Screen.width - 210, Screen.height - 165, 200, 155);

        string title = "<color=#fffb00>" + Defines.MOD_NAME + "</color>";

        internal static Steam.SteamNetworking steamNetworking;
        private void PopulateWindow(int id) {
            if(steamNetworking != null && steamNetworking.isConnected && steamNetworking.mode == Steam.SteamNetworking.Mode.SERVER) {
                title = $"[ Server { Defines.MOD_VERSION } ]";

                GUILayout.Label("Secret:");
                GUILayout.TextField(steamNetworking.currentLobby.ToString());
                if(GUILayout.Button("Copy")) {
                    Clipboard.SendToClipboard(steamNetworking.currentLobby.ToString());
                }

                #if NETWORK_STATS
                GUILayout.Label($"Stats: ↓ { NetworkStats.receiveKbs }KB/s | ↑ { NetworkStats.sentKbs }KB/s");
                #endif

                if(GUILayout.Button("Stop")) {
                    ModManager.StopHost();
                }
            } else if(steamNetworking != null && steamNetworking.isConnected && steamNetworking.mode == Steam.SteamNetworking.Mode.CLIENT) {
                title = $"[ Client { Defines.MOD_VERSION } ]";

                GUILayout.Label("Secret:");
                GUILayout.TextField(steamNetworking.currentLobby.ToString());
                if(GUILayout.Button("Copy")) {
                    Clipboard.SendToClipboard(steamNetworking.currentLobby.ToString());
                }

                #if NETWORK_STATS
                GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                #endif

                if(GUILayout.Button("Disconnect")) {
                    ModManager.StopClient();
                }
            } else {
                if(GUI.Button(new Rect(10, 25, 180, 20), "Use Servers")) {
                    ModManager.steamGuiManager.enabled = false;
                    ModManager.guiManager.enabled = true;
                    ModManager.guiManager.windowRect = ModManager.steamGuiManager.windowRect;
                }

                if(Level.current == null || !Level.current.loaded || Level.current.data.id == "CharacterSelection") {
                    GUI.Label(new Rect(10, 60, 180, 50), "Wait for the level to finish loading...");
                } else {
                    if(GUI.Button(new Rect(10, 50, 85, 20), menu == 0 ? "[ Join ]" : "Join")) {
                        menu = 0;
                    }
                    if(GUI.Button(new Rect(105, 50, 85, 20), menu == 1 ? "[ Host ]" : "Host")) {
                        menu = 1;
                    }
            
                    if(menu == 0) {
                        GUI.Label(new Rect(15, 75, 30, 20), "Lobby:");
                        lobbyId = GUI.TextField(new Rect(50, 75, 140, 20), lobbyId);

                        if(GUI.Button(new Rect(10, 125, 180, 20), "Join Server")) {
                            JoinLobby(lobbyId);
                        }
                    } else {
                        GUI.Label(new Rect(15, 75, 30, 20), "Max:");
            
                        maxPlayers = (int) GUI.HorizontalSlider(new Rect(53, 80, 110, 20), maxPlayers, 2, Defines.MAX_PLAYERS);
                        GUI.Label(new Rect(175, 75, 30, 20), maxPlayers.ToString());
            
                        if(GUI.Button(new Rect(10, 125, 180, 20), "Start Server")) {
                            CreateLobby(maxPlayers);
                        }
                    }
                }
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public static void JoinLobby(string lobbyId) {
            steamNetworking.JoinLobby(ulong.Parse(lobbyId));
        }

        public static void CreateLobby(int maxPlayers) {
            steamNetworking.CreateLobby(maxPlayers);
        }

        void Start() {
            Steam.SteamIntegration.TryToInitSteam();

            steamNetworking = new Steam.SteamNetworking();
        }

        #if NETWORK_STATS
        float time = 0;
        #endif
        void Update() {
            if(steamNetworking != null) steamNetworking.RunCallbacks();

            if(Keyboard.current[Key.L].wasPressedThisFrame) {
                windowRect = new Rect(Screen.width - 210, Screen.height - 165, 200, 155);
            }

            #if NETWORK_STATS
            time += Time.deltaTime;
            if(time > 1) {
                NetworkStats.UpdatePacketCount();
                time = 0;
            }
            #endif
        }

        void LateUpdate() {
            if(steamNetworking != null) steamNetworking.RunLateCallbacks();
        }

        private void OnGUI() {
            windowRect = GUI.Window(1000, windowRect, PopulateWindow, title);
        }
    }
}
