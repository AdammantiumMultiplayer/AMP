﻿#if AMP
using AMP.Overlay;
using ThunderRoad;
using Steamworks;
using Netamite.Steam.Server;
using AMP.Data;
using AMP.Extension;
using SteamClient = Netamite.Steam.Client.SteamClient;
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
using Netamite.Steam.Integration;
using AMP.Logging;

namespace AMP.UI {
    public class IngameModUI : MonoBehaviour {

        Canvas canvas;

        ScrollRect serverlist;
        RectTransform serverInfo;
        RectTransform buttonBar;
        RectTransform steamHost;
        RectTransform steamInvites;
        TextMeshProUGUI serverInfoMessage;
        RectTransform disconnectButton;
        ScrollRect friendsPanel;
        RectTransform friendInvitePanel;
        RectTransform hostPanel;

        private Image inviteFriendImage;
        private TextMeshProUGUI inviteFriendName;

        Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.85f);

        private static ServerInfo currentInfo = null;
        private static FriendInfo currentFriend = null;
        private static Button currentButton = null;
        internal static IngameModUI currentUI = null;

        private enum Page {
            Serverlist = 0,
            SteamHosting,
            IpHosting,
            Disconnect
        }

        static ColorBlock buttonColor = new ColorBlock() {
            normalColor = new Color(1, 1, 1, 0.8f),
            highlightedColor = new Color(0.1f, 0.1f, 0.1f, 0.5f),
            pressedColor = new Color(0.21f, 0.62f, 0.2f, 0.85f),
            selectedColor = new Color(0.21f, 0.62f, 0.2f, 0.85f),
            colorMultiplier = 1,
            fadeDuration = 0.25f
        };

        private void Awake() {
            if(currentUI != null) {
                Destroy(currentUI.gameObject);
            }
            currentUI = this;
        }

        void Start() {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1280, 720);
            canvasRect.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);

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

            #region Buttonbar
            GameObject gobj = CreateObject("Buttonbar");
            gobj.transform.SetParent(transform);
            buttonBar = gobj.AddComponent<RectTransform>();
            HorizontalLayoutGroup hlg = gobj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.padding = new RectOffset(5, 5, 0, 0);
            buttonBar.anchorMin = new Vector2(0, 1);
            buttonBar.anchorMax = Vector2.one;
            buttonBar.sizeDelta = new Vector2(0, 60);
            buttonBar.localPosition = new Vector3(0, 325, 0);

            // Buttons
            gobj = CreateObject("Serverlist");
            gobj.transform.SetParent(buttonBar.transform);
            rect = gobj.AddComponent<RectTransform>();
            Button btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.Serverlist);
            });
            GameObject text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Serverlist";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;


            gobj = CreateObject("Steam");
            gobj.transform.SetParent(buttonBar.transform);
            rect = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
                        btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.SteamHosting);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            
            btnText.text = (SteamIntegration.IsInitialized ? "Steam" : "Steam\nNot initialized");
            btn.enabled = SteamIntegration.IsInitialized;

            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;


            gobj = CreateObject("Host");
            gobj.transform.SetParent(buttonBar.transform);
            rect = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.IpHosting);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Server";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;
            #endregion

            #region Serverlist
            /*gobj = CreateObject("Serverlist");
            gobj.transform.SetParent(transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(-650, -80);
            rect.localPosition = new Vector3(-320f, -35, 0);
            serverlist = gobj.AddComponent<ScrollRect>();
            serverlist.horizontal = false;
            
            GameObject viewport = CreateObject("ViewPort");
            viewport.transform.SetParent(serverlist.transform);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>();
            rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(0, 0);
            rect.localPosition = new Vector3(0, 0, 0);

            serverlist.viewport = rect;


            gobj = CreateObject("Content");
            gobj.transform.SetParent(viewport.transform);
            VerticalLayoutGroup vlg = gobj.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            //vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            rect = gobj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(0, 0);
            rect.localPosition = new Vector3(0, 0, 0);
            rect.pivot = new Vector2(0.5f, 1f);

            ContentSizeFitter csf = gobj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            serverlist.content = rect;
            */
            serverlist = CreateScrollRect("Serverlist", false);
            serverlist.transform.SetParent(transform);
            rect = serverlist.gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-650, -80);
            rect.localPosition = new Vector3(-320f, -35, 0);


            gobj = CreateObject("Serverinfo");
            gobj.transform.SetParent(transform);
            serverInfo = gobj.AddComponent<RectTransform>();
            serverInfo.anchorMin = Vector2.zero;
            serverInfo.anchorMax = Vector2.one;
            serverInfo.sizeDelta = new Vector2(-640, -50);
            serverInfo.localPosition = new Vector3(320f, -50, 0);

            VerticalLayoutGroup vlg = gobj.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childControlWidth = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            vlg.childAlignment = TextAnchor.UpperCenter;
            #endregion

            #region Steam
            gobj = CreateObject("SteamHost");
            gobj.transform.SetParent(transform);
            steamHost = gobj.AddComponent<RectTransform>();
            vlg = gobj.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            steamHost.anchorMin = Vector2.zero;
            steamHost.anchorMax = Vector2.one;
            steamHost.sizeDelta = new Vector2(-640, -50);
            steamHost.localPosition = new Vector3(-320f, -50, 0);

            gobj = CreateObject("HostSteamButton");
            gobj.transform.SetParent(steamHost.transform);
            rect = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
#if AMP
                GUIManager.HostSteam(10);
#endif
                UpdateConnectionScreen();
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Host Steam";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;



            gobj = CreateObject("SteamInvites");
            gobj.transform.SetParent(transform);
            steamInvites = gobj.AddComponent<RectTransform>();
            vlg = gobj.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.spacing = 5;
            steamInvites.anchorMin = Vector2.zero;
            steamInvites.anchorMax = Vector2.one;
            steamInvites.sizeDelta = new Vector2(-640, -50);
            steamInvites.localPosition = new Vector3(320f, -50, 0);
            #endregion

            #region Host
            gobj = CreateObject("HostPanel");
            gobj.transform.SetParent(transform);
            hostPanel = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {

            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Coming in future versions";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;
            hostPanel.sizeDelta = new Vector2(500, 150);
            hostPanel.localPosition = new Vector3(0, 0, 0);
            #endregion

            #region Disconnect
            gobj = CreateObject("DisconnectButton");
            gobj.transform.SetParent(transform);
            disconnectButton = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
            btn.colors = buttonColor;
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
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;
            disconnectButton.sizeDelta = new Vector2(500, 150);
            disconnectButton.localPosition = new Vector3(0, -260, 0);

            gobj = CreateObject("ConnectionInfo");
            serverInfoMessage = gobj.AddComponent<TextMeshProUGUI>();
            serverInfoMessage.text = "";
            serverInfoMessage.color = Color.white;
            serverInfoMessage.alignment = TextAlignmentOptions.Center;
            serverInfoMessage.enableAutoSizing = true;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 50);
            rect.localPosition = new Vector3(0, -150, 0);



            /*gobj = CreateObject("SteamFriends");
            gobj.transform.SetParent(transform);
            friendsPanel = gobj.AddComponent<RectTransform>();
            GridLayoutGroup glg = gobj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(400, 75);
            glg.padding = new RectOffset(10, 10, 10, 10);
            glg.spacing = new Vector2(5, 5);
            glg.childAlignment = TextAnchor.UpperLeft;
            friendsPanel.anchorMin = Vector2.zero;
            friendsPanel.anchorMax = Vector2.one;
            friendsPanel.sizeDelta = new Vector2(-10, -350);
            friendsPanel.localPosition = new Vector3(0, 100, 0);*/


            friendsPanel = CreateScrollRect("InviteFriends", true);
            friendsPanel.transform.SetParent(transform);
            rect = friendsPanel.gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(-400, -350);
            rect.localPosition = new Vector3(-185, 100, 0);


            gobj = CreateObject("InviteFriend");
            gobj.transform.SetParent(transform);
            friendInvitePanel = gobj.gameObject.AddComponent<RectTransform>();
            friendInvitePanel.anchorMin = Vector2.zero;
            friendInvitePanel.anchorMax = Vector2.one;
            friendInvitePanel.sizeDelta = new Vector2(-920, -350);
            friendInvitePanel.localPosition = new Vector3(450, 100, 0);

            gobj = CreateObject("ProfileImage");
            gobj.transform.SetParent(friendInvitePanel);
            rect = gobj.gameObject.AddComponent<RectTransform>();
            inviteFriendImage = gobj.gameObject.AddComponent<Image>();
            rect.anchorMin = Vector2.one / 2;
            rect.anchorMax = Vector2.one / 2;
            rect.sizeDelta = new Vector2(200, 200);
            rect.localPosition = new Vector3(0, 80, 0);
            rect.localScale = new Vector3(1, -1, 1);

            gobj = CreateObject("ProfileName");
            gobj.transform.SetParent(friendInvitePanel);
            rect = gobj.gameObject.AddComponent<RectTransform>();
            inviteFriendName = gobj.gameObject.AddComponent<TextMeshProUGUI>();
            inviteFriendName.alignment = TextAlignmentOptions.Center;
            inviteFriendName.text = "";
            rect.anchorMin = Vector2.one / 2;
            rect.anchorMax = Vector2.one / 2;
            rect.sizeDelta = new Vector2(300, 50);
            rect.localPosition = new Vector3(0, -50, 0);

            gobj = CreateObject("InviteProfileButton");
            gobj.transform.SetParent(friendInvitePanel);
            rect = gobj.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.one / 2;
            rect.anchorMax = Vector2.one / 2;
            rect.sizeDelta = new Vector2(300, 60);
            rect.localPosition = new Vector3(0, -130, 0);

            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                if(currentFriend == null) return;
                IngameModUI.currentUI.Invite(currentFriend);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            text.transform.localPosition = Vector3.zero;
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Invite";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;

            #endregion

            UpdateConnectionScreen();

#if AMP
            ThunderRoad.PointerInputModule.SetUICameraToAllCanvas();
#endif
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

        private Page currentPage = Page.Serverlist;
        private void ShowPage(Page page) {
            currentPage = page;

            buttonBar.gameObject.SetActive(true);

            serverlist.gameObject.SetActive(false);
            serverInfo.gameObject.SetActive(false);

            steamHost.gameObject.SetActive(false);
            steamInvites.gameObject.SetActive(false);

            hostPanel.gameObject.SetActive(false);

            disconnectButton.gameObject.SetActive(false);
            serverInfoMessage.gameObject.SetActive(false);
            friendsPanel.gameObject.SetActive(false);
            friendInvitePanel.gameObject.SetActive(false);

            switch(page) {
                case Page.Serverlist: {
                        serverlist.gameObject.SetActive(true);
                        serverInfo.gameObject.SetActive(true);
                        StartCoroutine(LoadServerlist());
                        break;
                    }
                case Page.SteamHosting: {
                        steamHost.gameObject.SetActive(true);
                        steamInvites.gameObject.SetActive(true);

                        StartCoroutine(LoadInvites());
                        break;
                    }
                case Page.IpHosting: {
                        hostPanel.gameObject.SetActive(true);
                        break;
                    }
                case Page.Disconnect: {
                        buttonBar.gameObject.SetActive(false);
                        disconnectButton.gameObject.SetActive(true);
                        serverInfoMessage.gameObject.SetActive(true);
                        friendInvitePanel.gameObject.SetActive(true);
#if AMP
                        if(ModManager.serverInstance != null && ModManager.serverInstance.netamiteServer is SteamServer) {
                            foreach(Transform t in friendsPanel.content) Destroy(t.gameObject);

                            friendsPanel.gameObject.SetActive(true);

                            foreach(FriendInfo friendInfo in currentlyPlaying) {
                                GameObject obj = friendInfo.GetPrefab();
                                obj.transform.SetParent(friendsPanel.content, false);
                            }
                        }
#endif
                        break;
                    }
                default: {
                        break;
                    }
            }
            FixSize();
        }

        private ScrollRect CreateScrollRect(string name, bool gridLayout) {
            GameObject gScrollRect = CreateObject(name);
            gScrollRect.transform.SetParent(transform);
            RectTransform rScrollRect = gScrollRect.AddComponent<RectTransform>();
            rScrollRect.anchorMin = Vector2.zero;
            rScrollRect.anchorMax = Vector2.one;
            rScrollRect.sizeDelta = new Vector2(-650, -80);
            rScrollRect.localPosition = new Vector3(-320f, -35, 0);
            ScrollRect scrollRect = gScrollRect.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject gViewport = CreateObject("ViewPort");
            gViewport.transform.SetParent(gScrollRect.transform);
            gViewport.AddComponent<Mask>().showMaskGraphic = false;
            gViewport.AddComponent<Image>();
            RectTransform rViewport = gViewport.GetComponent<RectTransform>();
            rViewport.anchorMin = Vector2.zero;
            rViewport.anchorMax = Vector2.one;
            rViewport.sizeDelta = new Vector2(0, 0);
            rViewport.localPosition = new Vector3(0, 0, 0);

            scrollRect.viewport = rViewport;


            GameObject gContent = CreateObject("Content");
            gContent.transform.SetParent(gViewport.transform);

            if(gridLayout) {
                GridLayoutGroup glg = gContent.AddComponent<GridLayoutGroup>();
                glg.cellSize = new Vector2(400, 75);
                glg.padding = new RectOffset(10, 10, 10, 10);
                glg.spacing = new Vector2(5, 5);
                glg.childAlignment = TextAnchor.UpperCenter;
            } else {
                VerticalLayoutGroup vlg = gContent.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = false;
                //vlg.padding = new RectOffset(10, 10, 10, 10);
                vlg.spacing = 2;
            }

            RectTransform rContent = gContent.GetComponent<RectTransform>();
            rContent.anchorMin = new Vector2(0, 1);
            rContent.anchorMax = Vector2.one;
            rContent.sizeDelta = new Vector2(0, 0);
            rContent.localPosition = new Vector3(0, 0, 0);
            rContent.pivot = new Vector2(0.5f, 1f);

            ContentSizeFitter csf = gContent.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = rContent;


            GameObject gScrollBarV = CreateObject("ScrollbarV");
            Scrollbar scrollBarV = gScrollBarV.AddComponent<Scrollbar>();
            scrollBarV.direction = Scrollbar.Direction.BottomToTop;
            gScrollBarV.transform.SetParent(gScrollRect.transform);
            RectTransform rScrollBarV = gScrollBarV.GetComponent<RectTransform>();
            rScrollBarV.anchorMin = new Vector2(1, 0);
            rScrollBarV.anchorMax = Vector2.one;
            rScrollBarV.pivot = new Vector2(1, 0.5f);
            rScrollBarV.anchoredPosition = new Vector2(10, 0);
            rScrollBarV.sizeDelta = new Vector2(8, 0);

            GameObject gScrollBarVSlidingArea = CreateObject("ScrollbarVSlidingArea");
            gScrollBarVSlidingArea.transform.SetParent(gScrollBarV.transform);
            RectTransform rScrollBarVSlidingArea = gScrollBarVSlidingArea.AddComponent<RectTransform>();
            rScrollBarVSlidingArea.anchorMin = Vector2.zero;
            rScrollBarVSlidingArea.anchorMax = Vector2.one;
            rScrollBarVSlidingArea.sizeDelta = Vector2.zero;
            rScrollBarVSlidingArea.anchoredPosition = Vector2.zero;


            GameObject gScrollBarVHandle = CreateObject("ScrollbarVHandle");
            gScrollBarVHandle.transform.SetParent(gScrollBarVSlidingArea.transform);
            RectTransform rScrollBarVHandle = gScrollBarVHandle.AddComponent<RectTransform>();
            rScrollBarVHandle.sizeDelta = new Vector2(0, 0);
            rScrollBarVHandle.anchorMin = Vector2.zero;
            rScrollBarVHandle.anchorMax = Vector2.one;
            rScrollBarVHandle.anchoredPosition = Vector2.zero;


            scrollBarV.targetGraphic = gScrollBarVHandle.AddComponent<Image>();
            scrollBarV.targetGraphic.color = Color.gray;

            scrollBarV.handleRect = gScrollBarVHandle.GetComponent<RectTransform>();

            scrollRect.verticalScrollbar = scrollBarV;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            return scrollRect;
        }

        private void UpdateConnectionScreen() {
#if AMP
            if(ModManager.clientInstance != null || ModManager.serverInstance != null) {

                string info = "";
                if(ModManager.serverInstance != null) {
                    if(ModManager.serverInstance.netamiteServer is SteamServer)
                        info = $"[ Hosting Steam {Defines.FULL_MOD_VERSION} ]";
                    else
                        info = $"[ Hosting Server {Defines.FULL_MOD_VERSION} ]";
                } else if(ModManager.clientInstance != null) {
                    if(ModManager.clientInstance.netclient is SteamClient)
                        info = $"[ Client {Defines.FULL_MOD_VERSION} @ Steam ]";
                    else
                        info = $"[ Client {Defines.FULL_MOD_VERSION} @ {ModManager.guiManager.join_ip} ]";
                }
                serverInfoMessage.text = info;

                UpdateFriendsPlaying();

                ShowPage(Page.Disconnect);
                return;
            }
#endif
            ShowPage(Page.Serverlist);
            return;
        }

        internal IEnumerator LoadInvites() {

            foreach(Transform t in steamInvites) {
                Destroy(t.gameObject);
            }

            GameObject gobj = CreateObject("SteamInviteTitle");
            gobj.transform.SetParent(steamInvites);
            gobj.AddComponent<TextMeshProUGUI>().text = "Open Invites:";

#if AMP
            foreach(SteamInvite invite in ModManager.instance.invites) {
                GameObject obj = invite.GetPrefab();
                obj.transform.SetParent(steamInvites, false);
                Log.Debug(invite.name);
            }
#endif

            FixSize();

            yield break;
        }

        private class FriendInfo {
            public ulong steamId;
            public string steamName;
            public Status status = Status.Offline;

            public enum Status {
                Offline,
                Online,
                OtherGame,
                SameGame
            }

            public FriendInfo(ulong steamId, string steamName, Status status) {
                this.steamId = steamId;
                this.steamName = steamName;
                this.status = status;
            }


            public GameObject GetPrefab() {
                GameObject gobj = new GameObject(steamId.ToString());
                RectTransform rect = gobj.AddComponent<RectTransform>();
                Button btn = gobj.AddComponent<Button>();
                btn.targetGraphic = gobj.AddComponent<Image>();
                btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
                btn.colors = buttonColor;
                btn.onClick.AddListener(() => {
                    currentFriend = this;
                    UpdateFriendInfo();
                });

                rect.sizeDelta = new Vector2(100, 60);

                GameObject obj;
                /*
                obj = new GameObject("Icon");
                Image image = obj.AddComponent<Image>();
                image.sprite = GetIcon();
                image.transform.SetParent(rect);
                RectTransform imgRect = obj.GetComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0, 0.5f);
                imgRect.anchorMax = new Vector2(0, 0.5f);
                imgRect.sizeDelta = new Vector2(50f, 50f);
                imgRect.localPosition = new Vector3(-20f, 0, 0);
                */

                obj = new GameObject("Name");
                TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.transform.SetParent(rect);
                RectTransform txtRect = obj.GetComponent<RectTransform>();
                txtRect.sizeDelta = new Vector2(350, 40);
                txtRect.localPosition = new Vector3(-10, 0, 0);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.enableAutoSizing = true;
                tmp.text = steamName;

                switch(status) {
                    case Status.Offline:
                        break;
                    case Status.Online:
                    case Status.OtherGame:
                        tmp.color = Color.blue;
                        break;
                    case Status.SameGame:
                        tmp.color = Color.green;
                        break;
                    default: break;
                }

                return gobj;
            }
        }

        internal class SteamInvite {
            public string name = "";
            public ulong steamId;
            public ulong lobbyId;

            public SteamInvite(string name, ulong steamId, ulong lobbyId) {
                this.name = name;
                this.steamId = steamId;
                this.lobbyId = lobbyId;
            }

            public GameObject GetPrefab() {
                GameObject gobj = new GameObject(steamId.ToString());
                RectTransform rect = gobj.AddComponent<RectTransform>();
                Button btn = gobj.AddComponent<Button>();
                btn.targetGraphic = gobj.AddComponent<Image>();
                btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
                btn.colors = buttonColor;
                btn.onClick.AddListener(() => {
                    currentUI.DoConnect(lobbyId);
                });

                rect.sizeDelta = new Vector2(100, 60);

                GameObject obj;
                /*
                obj = new GameObject("Icon");
                Image image = obj.AddComponent<Image>();
                image.sprite = GetIcon();
                image.transform.SetParent(rect);
                RectTransform imgRect = obj.GetComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0, 0.5f);
                imgRect.anchorMax = new Vector2(0, 0.5f);
                imgRect.sizeDelta = new Vector2(50f, 50f);
                imgRect.localPosition = new Vector3(-20f, 0, 0);
                */

                obj = new GameObject("Name");
                TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.transform.SetParent(rect);
                RectTransform txtRect = obj.GetComponent<RectTransform>();
                txtRect.sizeDelta = new Vector2(350, 40);
                txtRect.localPosition = new Vector3(-10, 0, 0);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.enableAutoSizing = true;
                tmp.text = name;

                return gobj;
            }
        }

        private class ServerInfo {
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

            public Sprite GetIcon() {
                byte[] imageBytes = Convert.FromBase64String(servericon);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imageBytes);
                Sprite sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
                return sprite;
            }

            public string GetName() {
                string name = servername;
                if(official == 1) {
                    name = "<color=#0ab5ec>" + name + "</color>";
                }
                return name;
            }

            public GameObject GetPrefab() {
                GameObject gobj = new GameObject(id.ToString());
                RectTransform rect = gobj.AddComponent<RectTransform>();
                Button btn = gobj.AddComponent<Button>();
                btn.targetGraphic = gobj.AddComponent<Image>();
                btn.targetGraphic.color = new Color(0, 0, 0, 0);
                btn.colors = buttonColor;
                btn.onClick.AddListener(() => {
                    if(currentButton != null) {
                        currentButton.targetGraphic.color = new Color(0, 0, 0, 0);
                    }

                    currentInfo = this;
                    currentButton = btn;
                    currentButton.targetGraphic.color = buttonColor.selectedColor;
                    currentUI.UpdateServerInfo();
                });

                rect.sizeDelta = new Vector2(100, 60);


                GameObject obj = new GameObject("Icon");
                Image image = obj.AddComponent<Image>();
                image.sprite = GetIcon();
                image.transform.SetParent(rect);
                RectTransform imgRect = obj.GetComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0, 0.5f);
                imgRect.anchorMax = new Vector2(0, 0.5f);
                imgRect.sizeDelta = new Vector2(50f, 50f);
                imgRect.localPosition = new Vector3(-20f, 0, 0);


                obj = new GameObject("Name");
                TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.transform.SetParent(rect);
                RectTransform txtRect = obj.GetComponent<RectTransform>();
                txtRect.sizeDelta = new Vector2(480, 40);
                txtRect.localPosition = new Vector3(-10, 0, 0);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.enableAutoSizing = true;
                tmp.text = GetName();

                obj = new GameObject("PlayerCount");
                tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.transform.SetParent(rect);
                txtRect = obj.GetComponent<RectTransform>();
                txtRect.anchorMin = new Vector2(1, 0.5f);
                txtRect.anchorMax = new Vector2(1, 0.5f);
                txtRect.sizeDelta = new Vector2(70, 40);
                txtRect.localPosition = new Vector3(10, 0, 0);
                tmp.alignment = TextAlignmentOptions.Midline;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMax = 20;
                tmp.text = players_connected + " / " + players_max;

                return gobj;
            }
        }

        List<ServerInfo> servers;
        IEnumerator LoadServerlist() {
            using(UnityWebRequest webRequest = UnityWebRequest.Get($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/list.php")) {
                yield return webRequest.SendWebRequest();

                switch(webRequest.result) {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        //Log.Err(Defines.AMP, $"Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        //Log.Err(Defines.AMP, $"HTTP Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        servers = JsonConvert.DeserializeObject<List<ServerInfo>>(webRequest.downloadHandler.text);

                        UpdateServerListDisplay();
                        break;
                }
            }
        }

        private void UpdateServerListDisplay() {
            foreach(Transform t in serverlist.content) {
                Destroy(t.gameObject);
            }

            foreach(ServerInfo sv in servers) {
                GameObject obj = sv.GetPrefab();
                obj.transform.SetParent(serverlist.content, false);
            }
            FixSize();
        }

        private Image current_Icon;
        private TextMeshProUGUI current_Name;
        private TextMeshProUGUI current_Description;
        private TextMeshProUGUI current_MapInfo;
        private TextMeshProUGUI current_PlayerCount;
        private TextMeshProUGUI current_Address;
        private TextMeshProUGUI current_PvP;
        private TextMeshProUGUI current_MapChanging;
        private void UpdateServerInfo() {
            if(current_Icon == null) {
                BuildServerInfo();
            }
            current_Icon.sprite = currentInfo.GetIcon();
            current_Name.text = currentInfo.GetName();
            current_Description.text = currentInfo.description;
            current_PlayerCount.text = "Players: " + currentInfo.players_connected + " / " + currentInfo.players_max;
            current_MapInfo.text = currentInfo.modus + " @ " + currentInfo.map;
            current_Address.text = currentInfo.address + ":" + currentInfo.port;

            current_PvP.color = currentInfo.pvp == 0 ? Color.red : Color.green;
            current_MapChanging.color = currentInfo.static_map == 0 ? Color.red : Color.green;

            current_Description.ForceMeshUpdate();
            RectTransform rt = current_Description.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, current_Description.textBounds.size.y + 10);
        }

        private static void UpdateFriendInfo() {
            if(currentUI == null) return;

            int ret = SteamFriends.GetLargeFriendAvatar((CSteamID) currentFriend.steamId);
            Texture2D img = GetSteamImageAsTexture2D(ret);

            if(img != null) {
                currentUI.inviteFriendImage.sprite = Sprite.Create(img, new Rect(0, 0, img.width, img.height), Vector2.zero);
                currentUI.inviteFriendImage.gameObject.SetActive(true);
            } else {
                currentUI.inviteFriendImage.gameObject.SetActive(false);
            }
            currentUI.inviteFriendName.text = currentFriend.steamName;
        }

        private static Texture2D GetSteamImageAsTexture2D(int iImage) {
            Texture2D ret = null;
            uint ImageWidth;
            uint ImageHeight;
            bool bIsValid = SteamUtils.GetImageSize(iImage, out ImageWidth, out ImageHeight);

            if(bIsValid) {
                byte[] Image = new byte[ImageWidth * ImageHeight * 4];

                bIsValid = SteamUtils.GetImageRGBA(iImage, Image, (int)(ImageWidth * ImageHeight * 4));
                if(bIsValid) {
                    ret = new Texture2D((int)ImageWidth, (int)ImageHeight, TextureFormat.RGBA32, false, true);
                    ret.LoadRawTextureData(Image);
                    ret.Apply();
                }
            }

            return ret;
        }


        private void BuildServerInfo() {
            GameObject obj;
            RectTransform rt;

            obj = CreateObject("CurrentName");
            current_Name = obj.AddComponent<TextMeshProUGUI>();
            current_Name.transform.SetParent(serverInfo);
            current_Name.enableAutoSizing = true;
            current_Name.fontStyle = FontStyles.Bold;
            current_Name.alignment = TextAlignmentOptions.Center;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 50);

            obj = CreateObject("CurrentIcon");
            current_Icon = obj.AddComponent<Image>();
            current_Icon.transform.SetParent(serverInfo);
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);


            obj = CreateObject("CurrentDesc");
            current_Description = obj.AddComponent<TextMeshProUGUI>();
            current_Description.transform.SetParent(serverInfo);
            current_Description.enableAutoSizing = true;
            current_Description.fontSizeMax = 36;
            current_Description.alignment = TextAlignmentOptions.TopJustified;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 150);

            obj = CreateObject("MapInfo");
            current_MapInfo = obj.AddComponent<TextMeshProUGUI>();
            current_MapInfo.transform.SetParent(serverInfo);
            current_MapInfo.enableAutoSizing = true;
            current_MapInfo.fontSizeMax = 36;
            current_MapInfo.alignment = TextAlignmentOptions.Center;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 40);

            obj = CreateObject("PlayerCount");
            current_PlayerCount = obj.AddComponent<TextMeshProUGUI>();
            current_PlayerCount.transform.SetParent(serverInfo);
            current_PlayerCount.enableAutoSizing = true;
            current_PlayerCount.fontSizeMax = 36;
            current_PlayerCount.alignment = TextAlignmentOptions.Center;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 40);


            obj = CreateObject("PvP");
            current_PvP = obj.AddComponent<TextMeshProUGUI>();
            current_PvP.transform.SetParent(serverInfo);
            current_PvP.enableAutoSizing = true;
            current_PvP.fontSizeMax = 36;
            current_PvP.alignment = TextAlignmentOptions.Center;
            current_PvP.text = "PvP";
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 40);

            obj = CreateObject("MapChanging");
            current_MapChanging = obj.AddComponent<TextMeshProUGUI>();
            current_MapChanging.transform.SetParent(serverInfo);
            current_MapChanging.enableAutoSizing = true;
            current_MapChanging.fontSizeMax = 36;
            current_MapChanging.alignment = TextAlignmentOptions.Center;
            current_MapChanging.text = "Change Maps";
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 40);

            obj = CreateObject("Address");
            current_Address = obj.AddComponent<TextMeshProUGUI>();
            current_Address.transform.SetParent(serverInfo);
            current_Address.enableAutoSizing = true;
            current_Address.fontSizeMax = 36;
            current_Address.alignment = TextAlignmentOptions.Center;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 80);


            obj = CreateObject("JoinButton");
            Button btn = obj.AddComponent<Button>();
            obj.transform.SetParent(serverInfo);
            btn.targetGraphic = obj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.5f);
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 80);

            GameObject text = CreateObject("Text");
            text.transform.SetParent(obj.transform);
            TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Connect";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                DoConnect();
            });

            FixSize();
        }

        private float time = 0f;
        void FixedUpdate() {
            if(currentPage == Page.Disconnect) {
#if AMP
                if(ModManager.serverInstance == null && ModManager.clientInstance == null) {
                    UpdateConnectionScreen();
                } else if(time > 15f) {
                    UpdateConnectionScreen();
                    time = 0f;
                }
                time += Time.fixedDeltaTime;
#endif          
            }
#if AMP
            if(Player.currentCreature.transform.position.Distance(transform.position) > 3) {
                CloseMenu();
            }
#endif
        }

        internal void CloseMenu() {
            Destroy(gameObject);
#if AMP
            ModManager.ingameUI = null;
#endif
        }

        private void DoConnect() {
#if AMP
            GUIManager.JoinServer(currentInfo.address, currentInfo.port.ToString());
#endif
            UpdateConnectionScreen();
        }

        private void DoConnect(ulong lobbyId) {
#if AMP
            ModManager.JoinSteam(lobbyId);
            ModManager.instance.invites.RemoveAll((invite) => invite.lobbyId == lobbyId);
            StartCoroutine(LoadInvites());
#endif
        }

        private void Invite(FriendInfo friendInfo) {
#if AMP
            SteamFriends.InviteUserToGame((CSteamID)friendInfo.steamId, "");
#endif
        }

        private List<FriendInfo> currentlyPlaying = new List<FriendInfo>();
        private void UpdateFriendsPlaying() {
#if AMP
            currentlyPlaying.Clear();

            int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll);
            for(int i = 0; i < count; i++) {
                CSteamID friend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagAll);
                FriendGameInfo_t gameInfo;
                SteamFriends.GetFriendGamePlayed(friend, out gameInfo);

                FriendInfo.Status status = FriendInfo.Status.Offline;
                EPersonaState state = SteamFriends.GetFriendPersonaState(friend);
                string name = SteamFriends.GetFriendPersonaName(friend);

                if((int)state >= (int)EPersonaState.k_EPersonaStateOnline) status = FriendInfo.Status.Online;

                if(gameInfo.m_gameID.m_GameID == ModManager.instance.currentAppId) {
                    status = FriendInfo.Status.SameGame;
                } else if(gameInfo.m_gameID.m_GameID > 0) {
                    status = FriendInfo.Status.OtherGame;
                }
                currentlyPlaying.Add(new FriendInfo(friend.m_SteamID, name, status));
            }
            currentlyPlaying = currentlyPlaying.OrderByDescending(f => f.status).ThenBy(f => f.steamName).ToList();
#endif
        }

        private GameObject CreateObject(string obj) {
            GameObject go = new GameObject(obj);
            go.transform.parent = transform;
            go.transform.localScale = Vector3.one;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localPosition = Vector3.zero;
            return go;
        }
    }
}