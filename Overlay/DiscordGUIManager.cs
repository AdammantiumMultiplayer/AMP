using AMP.Data;
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
    internal class DiscordGUIManager : MonoBehaviour {

        public string secret = "";
        public int maxPlayers = 4;
        public int menu = 0;

        internal Rect windowRect = new Rect(Screen.width - 210, Screen.height - 165, 200, 155);

        string title = "<color=#fffb00>" + Defines.MOD_NAME + "</color>";

        internal static DiscordNetworking.DiscordNetworking discordNetworking;
        private void PopulateWindow(int id) {
            if(discordNetworking != null && discordNetworking.isConnected && discordNetworking.mode == DiscordNetworking.DiscordNetworking.Mode.SERVER) {
                title = $"[ Server { Defines.MOD_VERSION } ]";
                
                GUILayout.TextField(discordNetworking.activitySecret);
                if(GUILayout.Button("Copy")) {
                    Clipboard.SendToClipboard(discordNetworking.activitySecret);
                }
                #if NETWORK_STATS
                GUILayout.Label($"Stats: ↓ { NetworkStats.receiveKbs }KB/s | ↑ { NetworkStats.sentKbs }KB/s");
                #endif

                if(GUILayout.Button("Stop")) {
                    ModManager.StopHost();
                }
            } else if(discordNetworking != null && discordNetworking.isConnected && discordNetworking.mode == DiscordNetworking.DiscordNetworking.Mode.CLIENT) {
                title = $"[ Client { Defines.MOD_VERSION } ]";

                GUILayout.Label("Secret:");
                GUILayout.TextField(discordNetworking.activitySecret);
                if(GUILayout.Button("Copy")) {
                    Clipboard.SendToClipboard(discordNetworking.activitySecret);
                }
                #if NETWORK_STATS
                GUILayout.Label($"Stats: ↓ {NetworkStats.receiveKbs}KB/s | ↑ {NetworkStats.sentKbs}KB/s");
                #endif

                if(GUILayout.Button("Disconnect")) {
                    ModManager.StopClient();
                }
            } else {
                if(GUI.Button(new Rect(10, 25, 180, 20), "Use Servers")) {
                    ModManager.discordGuiManager.enabled = false;
                    ModManager.guiManager.enabled = true;
                    ModManager.guiManager.windowRect = ModManager.discordGuiManager.windowRect;
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
                        GUI.Label(new Rect(15, 75, 30, 20), "Secret:");

                        secret = GUI.TextField(new Rect(50, 75, 140, 20), secret);

                        if(GUI.Button(new Rect(10, 125, 180, 20), "Join Server")) {
                            JoinLobby(secret);
                        }
                    } else {
                        GUI.Label(new Rect(15, 75, 30, 20), "Max:");
            
                        maxPlayers = (int) GUI.HorizontalSlider(new Rect(53, 80, 110, 20), maxPlayers, 2, Defines.MAX_PLAYERS);
                        GUI.Label(new Rect(175, 75, 30, 20), maxPlayers.ToString());
            
                        if(GUI.Button(new Rect(10, 125, 180, 20), "Start Server")) {
                            CreateLobby((uint) maxPlayers);
                        }
                    }
                }
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public static void JoinLobby(string secret) {
            discordNetworking.JoinLobby(secret.Trim(), () => {
                ModManager.JoinServer(discordNetworking);
            });
        }

        public static void CreateLobby(uint maxPlayers) {
            discordNetworking.CreateLobby(maxPlayers, () => {
                ModManager.HostServer(maxPlayers, 0);
                ModManager.JoinServer(discordNetworking);

                foreach(Delegate d in discordNetworking.onPacketReceived.GetInvocationList()) {
                    discordNetworking.onPacketReceived -= (Action<NetPacket>) d;
                }

                if(discordNetworking.onPacketReceivedFromUser != null) {
                    foreach(Delegate d in discordNetworking.onPacketReceivedFromUser.GetInvocationList()) {
                        discordNetworking.onPacketReceivedFromUser -= (Action<User, NetPacket>) d;
                    }
                }

                discordNetworking.onPacketReceivedFromUser += (user, p) => {
                    if(!ModManager.serverInstance.clients.ContainsKey(user.Id)) {
                        return;
                    }

                    ClientData clientData = ModManager.serverInstance.clients[user.Id];
                    ModManager.serverInstance.OnPacket(clientData, p);
                };
            });
        }

        byte sdk_error = 0;
        string discordSdkFile = Path.Combine(Application.dataPath, "..", "discord_game_sdk.dll");

        void Start() {
            CheckForDiscordSDK();

            try {
                discordNetworking = new DiscordNetworking.DiscordNetworking();
                sdk_error = 0;
            } catch(Exception) {
                if(!File.Exists(discordSdkFile)) {
                    sdk_error = 1;
                } else {
                    sdk_error = 2;
                }
            }

            discordNetworking.UpdateActivity();
        }

        private void CheckForDiscordSDK() {
            if(!File.Exists(discordSdkFile)) {
                Log.Warn("AMP", "Couldn't find discord_game_sdk.dll, extracting it now.");
                using(var file = new FileStream(discordSdkFile, FileMode.Create, FileAccess.Write)) {
                    file.Write(Properties.Resources.discord_game_sdk, 0, Properties.Resources.discord_game_sdk.Length);
                }
            }
        }

        #if NETWORK_STATS
        float time = 0;
        #endif
        void Update() {
            if(discordNetworking != null) discordNetworking.RunCallbacks();

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
            if(discordNetworking != null) discordNetworking.RunLateCallbacks();
        }

        private void OnGUI() {
            if(sdk_error == 1) GUI.Label(new Rect(0, 0, 1000, 20), "Couldn't find discord_game_sdk.dll in game folder! Maybe unpacking failed, try installing manually.");
            if(sdk_error == 2) GUI.Label(new Rect(0, 0, 1000, 20), "Discord Game SDK returned error, is discord installed and running? Maybe try reinstalling discord.");

            windowRect = GUI.Window(1000, windowRect, PopulateWindow, title);
        }
    }
}
