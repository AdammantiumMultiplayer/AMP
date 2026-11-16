#if AMP
using AMP.Overlay;
using ThunderRoad;
#if STEAM  
using Steamworks;
using Netamite.Steam.Server;
using Netamite.Steam.Integration;
using SteamClient = Netamite.Steam.Client.SteamClient;
#endif
using AMP.Data;
using AMP.Extension;
#endif
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using AMP.SupportFunctions;

using AMP.Logging;
using ThunderRoadVRKBSharedData;
using AMP.Network;
using AMP.Threading;
using AMP.Network.Data.Sync;
using AMP.Resources;
using AMP.GameInteraction;
using AMP.Network.Packets.Implementation;

namespace AMP.UI {
    // Connection: connecting, warning and disconnect pages, connection handling
    public partial class IngameModUI {

        RectTransform disconnectPanel;
        TextMeshProUGUI serverJoinCodeLabel;
        TextMeshProUGUI serverJoinCodeMessage;
        TextMeshProUGUI serverInfoMessage;
        TextMeshProUGUI serverDebugMessage;
        RectTransform disconnectButton;
        RectTransform moderationButton;
        RectTransform connectingPanel;
        TextMeshProUGUI connectingMessage;
        
        
        RectTransform warningPanel;
        TextMeshProUGUI warningMessage;
        bool showedWarning = false;

        private void CreateDisconnectPanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            TextMeshProUGUI btnText;

            gobj = CreateObject("DisconnectPanel");
            gobj.transform.SetParent(transform);
            disconnectPanel = gobj.AddComponent<RectTransform>();
            
            
            gobj = CreateObject("DisconnectButton");
            gobj.transform.SetParent(disconnectPanel);
            disconnectButton = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;
            btn.onClick.AddListener(() => {
                #if AMP
                ModManager.Stop();
                #endif
                UpdateConnectionScreen();
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Disconnect";
            btnText.color = tabTextColor;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            disconnectButton.sizeDelta = new Vector2(500, 150);
            disconnectButton.localPosition = new Vector3(0, -260, 0);

            
            
            gobj = CreateObject("ModerationButton");
            gobj.transform.SetParent(disconnectPanel);
            moderationButton = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.Moderation);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Players";
            btnText.color = tabTextColor;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            moderationButton.sizeDelta = new Vector2(300, 150);
            moderationButton.localPosition = new Vector3(460, -260, 0);


            gobj = CreateObject("FixButton");
            gobj.transform.SetParent(disconnectPanel);
            moderationButton = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;
            btn.onClick.AddListener(() => {
                ModManager.Fix();
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Fix";
            btnText.color = tabTextColor;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            moderationButton.sizeDelta = new Vector2(300, 150);
            moderationButton.localPosition = new Vector3(-460, -260, 0);


            gobj = CreateObject("JoinCodeInfoLabel");
            gobj.transform.SetParent(disconnectPanel);
            serverJoinCodeLabel = gobj.AddComponent<TextMeshProUGUI>();
            serverJoinCodeLabel.text = "Join Code:";
            serverJoinCodeLabel.fontSize = 50;
            serverJoinCodeLabel.color = Color.black;
            serverJoinCodeLabel.alignment = TextAlignmentOptions.Center;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 120);
            rect.localPosition = new Vector3(0, 200, 0);
            
            
            gobj = CreateObject("JoinCodeInfo");
            gobj.transform.SetParent(disconnectPanel);
            serverJoinCodeMessage = gobj.AddComponent<TextMeshProUGUI>();
            serverJoinCodeMessage.text = "";
            serverJoinCodeMessage.fontSize = 160;
            serverJoinCodeMessage.color = Color.black;
            serverJoinCodeMessage.alignment = TextAlignmentOptions.Center;
            serverJoinCodeMessage.enableAutoSizing = false;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 300);
            rect.localPosition = new Vector3(0, 130, 0);
            
            
            gobj = CreateObject("ConnectionInfo");
            gobj.transform.SetParent(disconnectPanel);
            serverInfoMessage = gobj.AddComponent<TextMeshProUGUI>();
            serverInfoMessage.text = "";
            serverInfoMessage.color = Color.black;
            serverInfoMessage.alignment = TextAlignmentOptions.Center;
            serverInfoMessage.enableAutoSizing = true;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 50);
            rect.localPosition = new Vector3(0, -150, 0);

            gobj = CreateObject("DebugInfo");
            gobj.transform.SetParent(disconnectPanel);
            serverDebugMessage = gobj.AddComponent<TextMeshProUGUI>();
            serverDebugMessage.text = "";
            serverDebugMessage.color = Color.black;
            serverDebugMessage.alignment = TextAlignmentOptions.Center;
            serverDebugMessage.fontSize = 20;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 50);
            rect.localPosition = new Vector3(0, -400, 0);
        }

        private void CreateConnectingPanel() {
            RectTransform rect;
            GameObject gobj;

            gobj = CreateObject("ConnectingPanel");
            gobj.transform.SetParent(transform);
            connectingPanel = gobj.AddComponent<RectTransform>();


            gobj = CreateObject("ConnectingMessage");
            gobj.transform.SetParent(connectingPanel);
            connectingMessage = gobj.AddComponent<TextMeshProUGUI>();
            connectingMessage.text = "";
            connectingMessage.fontSize = 180;
            connectingMessage.color = Color.black;
            connectingMessage.alignment = TextAlignmentOptions.Center;
            connectingMessage.enableAutoSizing = true;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 300);
            rect.localPosition = new Vector3(0, 130, 0);
        }

        private void CreateWarningPanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            TextMeshProUGUI btnText;

            gobj = CreateObject("WarningPanel");
            gobj.transform.SetParent(transform);
            warningPanel = gobj.AddComponent<RectTransform>();


            gobj = CreateObject("ConnectingMessage");
            gobj.transform.SetParent(warningPanel);
            warningMessage = gobj.AddComponent<TextMeshProUGUI>();
            warningMessage.text = "This mod is experimental! It will mess with your save file, so make sure you use a separate save when using multiplayer!";
            warningMessage.fontSize = 180;
            warningMessage.color = Color.red;
            warningMessage.alignment = TextAlignmentOptions.Center;
            warningMessage.enableAutoSizing = true;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 300);
            rect.localPosition = new Vector3(0, 130, 0);

            gobj = CreateObject("ConfirmWarningButton");
            gobj.transform.SetParent(warningPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(850, 200);
            rect.localPosition = new Vector3(0, -260, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;
            btn.onClick.AddListener(() => {
                Log.Info(Defines.AMP, "User confirmed the warning!");
                showedWarning = true;
                ShowPage(Page.Serverlist);
            });
            text = CreateObject("Text");
            rect = text.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(850, 200);
            text.transform.SetParent(btn.transform);
            rect.localPosition = new Vector3(0, 0, 0);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "I understand";
            btnText.color = tabTextColor;
            btnText.fontSize = 80;
            btnText.fontWeight = FontWeight.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
        }
        
        public void UpdateConnectionScreen() {
            #if AMP
            if(ModManager.clientInstance != null || ModManager.serverInstance != null) {

                string info = "";
                if(ModManager.serverInstance != null) {
                    #if STEAM
                    if(ModManager.serverInstance.netamiteServer is SteamServer)
                        info = $"[ Hosting Steam {Defines.FULL_MOD_VERSION} ]";
                    else
                    #endif
                        info = $"[ Hosting Server {Defines.FULL_MOD_VERSION} ]";
                } else if(ModManager.clientInstance != null) {
                    #if STEAM
                    if(ModManager.clientInstance.netclient is SteamClient)
                        info = $"[ Client {Defines.FULL_MOD_VERSION} @ Steam ]";
                    else
                    #endif
                        info = $"[ Client {Defines.FULL_MOD_VERSION} @ {ModManager.guiManager.join_ip}:{ModManager.guiManager.join_port} ]";
                }
                serverInfoMessage.text = info;

                #if DEBUG
                try {
                    string message = $"Players: {ModManager.clientSync.syncData.players.Count + 1} / {ModManager.clientSync.syncData.server_config.max_players} ";
                    
                    #if NETWORK_STATS
                    message += $"| Ping: {ModManager.clientInstance?.netclient?.Ping}ms ";
                    message += $"| Stats: D {NetworkStats.receiveKbs}KB/s | U {NetworkStats.sentKbs}KB/s ";
                    #endif

                    serverDebugMessage.text = message;
                } catch { }
                #endif
                
                serverJoinCodeLabel.gameObject.SetActive(serverJoinCodeMessage.text.Length > 0);

                #if STEAM
                if (SteamIntegration.IsInitialized) {
                    UpdateFriendsPlaying();
                }
                #endif
                
                if(currentPage != Page.Disconnect)
                    ShowPage(Page.Disconnect);
                return;
            }
            #endif
            #if AMP
            if(Level.current == null || !Level.current.loaded || Level.current.data == null || Level.current.data.id == null) {
                ShowPage(Page.Connecting);
                connectingMessage.color = Color.red;
                connectingMessage.text = "Please load into a level.";
                return;
            }else if(Level.current.data.id == "CharacterSelection" || Level.current.data.id == "MainMenu" || Level.current.data.id.StartsWith("Dungeon")) {
                ShowPage(Page.Connecting);
                connectingMessage.color = Color.red;
                connectingMessage.text = "You can't join or host from inside a dungeon, please load into a different level first.";
                return;
            }
            #endif

            ShowPage(Page.WARNING);
            return;
        }

        private void SetupConnectionHandler() {
            ModManager.onConnectionError += (reason) => {
                Dispatcher.Enqueue(() => {
                    currentUI.StartCoroutine(FailedConnectionCoroutine(reason));
                });
                ModManager.clientInstance.netclient.OnConnect += () => {
                    Dispatcher.Enqueue(UpdateConnectionScreen);
                };
            };

            ModManager.onConnectionSuccess += () => {
                Dispatcher.Enqueue(UpdateConnectionScreen);
            };
            
            ModManager.onConnectionClose += (reason) => {
                if (reason != null && reason.Length > 0) {
                    Dispatcher.Enqueue(() => {
                        currentUI.StartCoroutine(FailedConnectionCoroutine(reason));
                    });
                } else {
                    Dispatcher.Enqueue(UpdateConnectionScreen);
                }
            };
        }
        
        private IEnumerator FailedConnectionCoroutine(string reason) {
            if ("SERVER_FULL".Equals(reason)) reason = "Server is full";
            else if ("WRONG_PASSWORD".Equals(reason)) reason = "Wrong password";
            else if ("SERVER_CLOSED".Equals(reason)) reason = "Server closed";
            else if ("CONNECTION_CLOSED".Equals(reason)) {
                if(currentPage == Page.Connecting) UpdateConnectionScreen(); // Don't stay on the connecting screen
                yield break;
            }

            ShowPage(Page.Connecting);
            connectingMessage.text = $"Connection closed.\n\nReason: {reason}";
            connectingMessage.color = Color.red;

            yield return new WaitForSeconds(5);
            ShowPage(Page.Serverlist);
        }
        
        private void DoConnect() {
            if(currentInfo == null) return;

            serverJoinCodeLabel.gameObject.SetActive(false);
            serverJoinCodeMessage.gameObject.SetActive(false);

            // The connection handler (SetupConnectionHandler) switches to the server screen once connected, or shows the error
            ShowPage(Page.Connecting);
            connectingMessage.text = $"Connecting to {currentInfo.servername}...";
#if AMP
            try {
                if(!GUIManager.JoinServer(currentInfo.address, currentInfo.port.ToString())) {
                    StartCoroutine(FailedConnectionCoroutine("Already connecting to a server, please wait."));
                }
            } catch(Exception e) {
                Log.Err(Defines.AMP, $"Couldn't connect to {currentInfo.address}:{currentInfo.port}: {e}");
                StartCoroutine(FailedConnectionCoroutine(e.Message));
            }
#endif
        }
    }
}
