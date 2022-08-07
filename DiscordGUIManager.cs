using UnityEngine;
using System.Net.NetworkInformation;
using ThunderRoad;
using System.Collections;
using System;
using AMP.Network.Data;

namespace AMP {
    public class DiscordGUIManager : MonoBehaviour {

        public string secret = "";
        public int maxPlayers = 4;
        public int menu = 0;

        public long ping;

        private Rect windowRect = new Rect(Screen.width - 210, Screen.height - 140, 200, 130);

        string title = "<color=#fffb00>" + ModManager.MOD_NAME + "</color>";

        public static DiscordNetworking.DiscordNetworking discordNetworking = new DiscordNetworking.DiscordNetworking();
        private void PopulateWindow(int id) {
            if(discordNetworking != null && discordNetworking.isConnected && discordNetworking.mode == DiscordNetworking.DiscordNetworking.Mode.SERVER) {
                title = $"[ Server { ModManager.MOD_VERSION } ]";
            
                GUILayout.Label("Secret:");
                GUILayout.TextField(discordNetworking.activitySecret);
            } else if(discordNetworking != null && discordNetworking.isConnected && discordNetworking.mode == DiscordNetworking.DiscordNetworking.Mode.CLIENT) {
                title = $"[ Client { ModManager.MOD_VERSION } ]";
            
            } else {
                if(GUI.Button(new Rect(10, 25, 85, 20), menu == 0 ? "[ Join ]" : "Join")) {
                    menu = 0;
                }
                if(GUI.Button(new Rect(105, 25, 85, 20), menu == 1 ? "[ Host ]" : "Host")) {
                    menu = 1;
                }
            
                if(menu == 0) {
                    GUI.Label(new Rect(15, 50, 30, 20), "Secret:");

                    secret = GUI.TextField(new Rect(50, 50, 140, 20), secret);

                    if(GUI.Button(new Rect(10, 100, 180, 20), "Join Server")) {
                        JoinLobby(secret);
                    }
                } else {
                    GUI.Label(new Rect(15, 50, 30, 20), "Max:");
            
                    maxPlayers = (int) GUI.HorizontalSlider(new Rect(53, 55, 110, 20), maxPlayers, 2, 10);
                    GUI.Label(new Rect(175, 50, 30, 20), maxPlayers.ToString());
            
                    if(GUI.Button(new Rect(10, 100, 180, 20), "Start Server")) {
                        CreateLobby((uint) maxPlayers);
                    }
                }
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        public static void JoinLobby(string secret) {
            discordNetworking.JoinLobby(secret, () => {
                ModManager.JoinServer(discordNetworking);
            });
        }

        public static void CreateLobby(uint maxPlayers) {
            discordNetworking.CreateLobby(maxPlayers, () => {
                ModManager.HostServer(maxPlayers, 0);
                ModManager.JoinServer(discordNetworking);

                foreach(Delegate d in discordNetworking.onPacketReceived.GetInvocationList()) {
                    discordNetworking.onPacketReceived -= (Action<Packet>) d;
                }
                discordNetworking.onPacketReceivedFromUser += (user, p) => {
                    if(!ModManager.serverInstance.clients.ContainsKey(user.Id)) {
                        ClientData cd = new ClientData(user.Id);
                        cd.name = user.Username;

                        ModManager.serverInstance.clients.Add(user.Id, cd);
                    }

                    ClientData clientData = ModManager.serverInstance.clients[user.Id];
                    ModManager.serverInstance.OnPacket(clientData, p);
                };
            });
        }

        void Start() {
            discordNetworking.UpdateActivity();
        }

        void Update() {
            if(discordNetworking != null) discordNetworking.RunCallbacks();
        }

        void LateUpdate() {
            if(discordNetworking != null) discordNetworking.RunLateCallbacks();
        }

        private void OnGUI() {
            windowRect = GUI.Window(1000, windowRect, PopulateWindow, title);
        }
    }
}
