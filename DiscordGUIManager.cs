using UnityEngine;
using System.Net.NetworkInformation;
using ThunderRoad;
using System.Collections;
using System;

namespace AMP {
    public class DiscordGUIManager : MonoBehaviour {

        public string lobbyId = "";
        public string secret = "";
        public int maxPlayers = 4;
        public int menu = 0;

        public long ping;

        private Rect windowRect = new Rect(Screen.width - 210, Screen.height - 140, 200, 130);

        string title = "<color=#fffb00>" + ModManager.MOD_NAME + "</color>";

        DiscordNetworking.DiscordNetworking discordNetworking;
        private void PopulateWindow(int id) {
            if(discordNetworking != null && discordNetworking.isConnected && discordNetworking.mode == DiscordNetworking.DiscordNetworking.Mode.SERVER) {
                title = $"[ Server { ModManager.MOD_VERSION } ]";
            
                GUILayout.Label("LobbyId / Secret:");
                GUILayout.TextField(discordNetworking.currentLobby.Id.ToString());
                GUILayout.TextField(discordNetworking.currentLobby.Secret.ToString());
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
                    GUI.Label(new Rect(15, 50, 30, 20), "LobbyId:");
                    GUI.Label(new Rect(15, 80, 30, 20), "Secret:");

                    lobbyId = GUI.TextField(new Rect(50, 50, 140, 20), lobbyId);
                    secret = GUI.TextField(new Rect(50, 80, 140, 20), secret);

                    if(GUI.Button(new Rect(10, 100, 180, 20), "Join Server")) {
                        JoinLobby(lobbyId, secret);
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

        private void JoinLobby(string lobbyId, string secret) {
            discordNetworking = new DiscordNetworking.DiscordNetworking();
            discordNetworking.JoinLobby(long.Parse(lobbyId), secret, () => {
                ModManager.JoinServer(discordNetworking);
            });
        }

        private void CreateLobby(uint maxPlayers) {
            discordNetworking = new DiscordNetworking.DiscordNetworking();
            discordNetworking.CreateLobby(maxPlayers, () => {
                ModManager.JoinServer(discordNetworking);
            });
        }

        void Update() {
            if(discordNetworking != null) discordNetworking.RunCallbacks();
        }

        private void OnGUI() {
            windowRect = GUI.Window(1000, windowRect, PopulateWindow, title);
        }
    }
}
