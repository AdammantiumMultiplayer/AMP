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

//////////////////////////////////////////////////////////////
/// If you can read this, close the file as fast as you can
/// This is horrible, but the price I had to pay because i refuse
/// to learn the SDK.
/// On the one hand, thats stupid and im stupid and i know it
/// buuuuuut on the other hand, no dependencies and no need to wait
/// for the SDK to update. Also, no warpfrog rules apply for the mod (evil face)
/// 
/// But anyways, you have been warned
//////////////////////////////////////////////////////////////


namespace AMP.UI {
    public partial class IngameModUI : MonoBehaviour {

        RectTransform buttonBar;


        Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0f);

        internal static IngameModUI currentUI = null;

        private enum Page {
            Serverlist = 0,
            #if STEAM
            SteamHosting = 1,
            #endif
            IpHosting = 2,
            CodeJoining = 3,
            IpJoining = 4,
            Disconnect = 5,
            Connecting = 6,
            Moderation = 7,
            LocalHosting = 8,
            #if STEAM
            SteamJoining = 9,
            #endif
            ModCompatibility = 20,
            
            WARNING = 10
        }


        private void Awake() {
            if(currentUI != null) {
                Destroy(currentUI.gameObject);
            }
            currentUI = this;
        }
        
        void Start() {
            if(ModManager.safeFile.hostingSettings.masterServerUrl == null || ModManager.safeFile.hostingSettings.masterServerUrl.Length == 0) {
                ModManager.safeFile.hostingSettings.masterServerUrl = "amp.adamite.de";
                Log.Warn(Defines.AMP, "No masterserver has been set, falling back to 'amp.adamite.de'.");
            }
            
            RectTransform canvasRect = this.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1280, 720);
            canvasRect.localScale = Vector3.one;
            canvasRect.anchoredPosition3D = Vector3.zero;
            canvasRect.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            gameObject.AddComponent<GraphicRaycaster>();

            GameObject background = CreateObject("Background");
            background.transform.SetParent(transform);
            background.AddComponent<Image>().color = backgroundColor;
            RectTransform rect = background.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(0, 0);

            BoxCollider col = background.AddComponent<BoxCollider>();
            col.size = new Vector3(1280, 720, 1);
            col.tag = "PointerActive";

            CreateTabBar();
            CreateServerlistPanel();
            #if STEAM
            CreateSteamHostPanel();
            #endif
            CreateJoinPanel();
            CreateJoinCodePanel();
            CreateJoinIpPanel();
            CreateHostPanel();
            CreateLocalHostPanel();
            CreateDisconnectPanel();
            CreateConnectingPanel();
            CreateWarningPanel();
            #if STEAM
            CreateSteamFriendsPanel();
            #endif
            CreateModerationPanel();
            CreateModCompatibiltyPanel();

            #if AMP
            ThunderRoad.PointerInputModule.SetUICameraToAllCanvas();
            SetupConnectionHandler();
            UpdateConnectionScreen();
            #endif
        }

        private void CreateTabBar() {
            GameObject gobj = CreateObject("Buttonbar");
            gobj.transform.SetParent(transform);
            buttonBar = gobj.AddComponent<RectTransform>();
            HorizontalLayoutGroup hlg = gobj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.padding = new RectOffset(5, 5, 0, 0);
            buttonBar.anchorMin = new Vector2(0, 1);
            buttonBar.anchorMax = new Vector2(0.99f, 0.99f);
            buttonBar.sizeDelta = new Vector2(0, tabHeight);
            buttonBar.localPosition = new Vector3(0, 325, 0);

            // Tabs, in a toggle group so the tab of the open page stays selected (see UpdateTabSelection)
            tabGroup = buttonBar.gameObject.AddComponent<ToggleGroup>();
            tabGroup.allowSwitchOff = true; // Pages without a tab (connecting, disconnect, ...) have none selected

            CreateTab(buttonBar, tabGroup, "Serverlist", "SERVERLIST", 36, Page.Serverlist);
            CreateTab(buttonBar, tabGroup, "Join", "JOIN", 36
                     , Page.CodeJoining
                     , Page.IpJoining
                     #if STEAM
                     , Page.SteamJoining
                     #endif
                     );
            CreateTab(buttonBar, tabGroup, "Host", "HOST", 36
                     , Page.IpHosting
                     #if STEAM
                     , Page.SteamHosting
                     #endif
                     , Page.LocalHosting
                     );
            CreateTab(buttonBar, tabGroup, "Mods", "MODS", 36
                    , Page.ModCompatibility
                    );
        }


        private void FixSize() {
            FixSize(transform);
        }

        private void FixSize(Transform parent) {
            gameObject.layer = 5;
            foreach(Transform child in parent) {
                if(child.localScale.x >= 200) {
                    child.localScale = Vector3.one;
                    child.localEulerAngles = Vector3.zero;
                }
                child.gameObject.layer = 5;
                FixSize(child);
            }
        }
        
        internal void RefreshPage() {
            ShowPage(currentPage);
        }

        private Page currentPage = Page.Serverlist;
        private void ShowPage(Page page) {
            if(page == Page.WARNING && showedWarning) {
                page = Page.Serverlist;
            }
            
            currentPage = page;
            UpdateTabSelection(page);
            
            buttonBar.gameObject.SetActive(true);
            
            serverlist.gameObject.SetActive(false);
            serverInfo.gameObject.SetActive(false);
            #if STEAM
            steamHost.gameObject.SetActive(false);
            steamInvites.gameObject.SetActive(false);
            #endif
            hostPanel.gameObject.SetActive(false);
            hostButtonBar.gameObject.SetActive(false);
            localHostPanel.gameObject.SetActive(false);
            joinPanel.gameObject.SetActive(false);
            joinButtonBar.gameObject.SetActive(false);
            joinCodePanel.gameObject.SetActive(false);
            joinIpPanel.gameObject.SetActive(false);
            disconnectPanel.gameObject.SetActive(false);
            connectingPanel.gameObject.SetActive(false);
            moderationPanel.gameObject.SetActive(false);
            moderationPanelPlayerList.gameObject.SetActive(false);
            warningPanel.gameObject.SetActive(false);
           	modCompatibilityPanel.gameObject.SetActive(false);

            //SetHostPinVisible(false);

            #if STEAM
            friendsPanel.gameObject.SetActive(false);
            if(page != Page.Disconnect) CloseInvitePopup();
            #endif
            switch (page) {
                case Page.Serverlist: {
                        serverlist.gameObject.SetActive(true);
                        serverInfo.gameObject.SetActive(true);
                        if(serverJoinButton != null) serverJoinButton.interactable = true; // Back from a connection attempt
                        StartCoroutine(LoadServerlist());
                        break;
                    }
                #if STEAM
                case Page.SteamHosting: {
                        hostButtonBar.gameObject.SetActive(true);
                        steamHost.gameObject.SetActive(true);
                        break;
                    }
                case Page.SteamJoining: {
                        joinButtonBar.gameObject.SetActive(true);
                        steamInvites.gameObject.SetActive(true);

                        StartCoroutine(LoadInvites());
                        break;
                    }
                #endif
                case Page.CodeJoining: {
                        joinPanel.gameObject.SetActive(true);
                        joinButtonBar.gameObject.SetActive(true);
                        joinCodePanel.gameObject.SetActive(true);
                        break;
                    }
                case Page.IpJoining: {
                        joinPanel.gameObject.SetActive(true);
                        joinButtonBar.gameObject.SetActive(true);
                        joinIpPanel.gameObject.SetActive(true);
                        break;
                    }
                case Page.IpHosting: {
                        SetLobbyDropdownOpen(false);
                        StartCoroutine(GetMasterServerData());
                        
                        hostButtonBar.gameObject.SetActive(true);
                        hostPanel.gameObject.SetActive(true);
                        break;
                    }
                case Page.LocalHosting: {
                        hostButtonBar.gameObject.SetActive(true);
                        localHostPanel.gameObject.SetActive(true);
                        break;
                    }
                case Page.Disconnect: {
                        buttonBar.gameObject.SetActive(false);
                        disconnectPanel.gameObject.SetActive(true);

                        #if STEAM
                        #if AMP
                        UpdateSteamFriendList();
                        #endif
                        #endif
                        break;
                    }
                case Page.Connecting: {
                    buttonBar.gameObject.SetActive(false);
                    connectingPanel.gameObject.SetActive(true);
                    connectingMessage.text = "";
                    connectingMessage.color = Color.black;
                    break;
                }
                case Page.WARNING: {
                    buttonBar.gameObject.SetActive(false);
                    warningPanel.gameObject.SetActive(true);
                    break;
                }
                case Page.Moderation: {
                        buttonBar.gameObject.SetActive(false);
                        moderationPanel.gameObject.SetActive(true);
                        moderationPanelPlayerList.gameObject.SetActive(true);

                        UpdatePlayerList();
                        break;
                }
                case Page.ModCompatibility: {
                	modCompatibilityPanel.gameObject.SetActive(true);
                 	BuildModList();
                 	break;
                }
                default: {
                    break;
                }
            }
            FixSize();
        }

        private float time = 0f;
        void Update() {
            if(currentPage == Page.Disconnect) {
                #if AMP
                if(ModManager.serverInstance == null && ModManager.clientInstance == null) {
                    UpdateConnectionScreen();
                } else if(time > 1f) {
                    UpdateConnectionScreen();
                    time = 0f;
                }
                time += Time.deltaTime;
                #endif
            }
        }
    }
}
