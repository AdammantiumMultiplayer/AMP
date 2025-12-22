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
    public class IngameModUI : MonoBehaviour {

        Canvas canvas;
        
        ScrollRect serverlist;
        RectTransform serverInfo;
        RectTransform buttonBar;
        #if STEAM
        RectTransform steamHost;
        RectTransform steamInvites;
        ScrollRect friendsPanel;
        RectTransform friendInvitePanel;
        private Image inviteFriendImage;
        private TextMeshProUGUI inviteFriendName;
        #endif
        RectTransform disconnectPanel;
        TextMeshProUGUI serverJoinCodeLabel;
        TextMeshProUGUI serverJoinCodeMessage;
        TextMeshProUGUI serverInfoMessage;
        TextMeshProUGUI serverDebugMessage;
        RectTransform disconnectButton;
        RectTransform moderationButton;

        RectTransform moderationPanel;
        ScrollRect moderationPanelPlayerList;

        RectTransform hostPanel;
        TextMeshProUGUI hostCode;
        static ToggleGroup hostServerToggleGroup;


        RectTransform joinPanel;
        TextMeshProUGUI joinCode;
        
        RectTransform connectingPanel;
        TextMeshProUGUI connectingMessage;
        
        
        RectTransform warningPanel;
        TextMeshProUGUI warningMessage;
        bool showedWarning = false;



        Color backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0f);

        private static ServerInfo currentInfo = null;
        #if STEAM        
        private static FriendInfo currentFriend = null;
        private static float lastInviteClick = Time.time;
        #endif
        private static Button currentButton = null;
        internal static IngameModUI currentUI = null;

        private enum Page {
            Serverlist = 0,
            #if STEAM
            SteamHosting = 1,
            #endif
            IpHosting = 2,
            Joining = 3,
            Disconnect = 4,
            Connecting = 5,
            Moderation = 6,
            
            WARNING = 10
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

            gobj = CreateObject("Join");
            gobj.transform.SetParent(buttonBar.transform);
            rect = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.Joining);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Join";
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
            btnText.text = "Host";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;

            #if STEAM
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
            #endif

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
            
            #if STEAM
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
                serverJoinCodeMessage.text = "";
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
            #endif
            
            #region Join
            gobj = CreateObject("JoinPanel");
            gobj.transform.SetParent(transform);
            joinPanel = gobj.AddComponent<RectTransform>();



            gobj = CreateObject("JoinCodeInfoLabel");
            gobj.transform.SetParent(joinPanel);
            TextMeshProUGUI textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "Join Code:";
            textMesh.fontSize = 50;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Center;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 200);
            rect.localPosition = new Vector3(0, 220, 0);

            gobj = CreateObject("ButtonPanel");
            gobj.transform.SetParent(joinPanel.transform);
            RectTransform buttonPanel = gobj.AddComponent<RectTransform>();
            buttonPanel.sizeDelta = new Vector2(775, 310);
            buttonPanel.localPosition = new Vector3(-75, -150, 0);
            GridLayoutGroup gridLayout = gobj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(150, 150);
            gridLayout.spacing = new Vector2(5, 5);
            for (int i = 0; i <= 9; i++) {
                int mynum = i;
                gobj = CreateObject("Button" + mynum);
                gobj.transform.SetParent(buttonPanel);
                btn = gobj.AddComponent<Button>();
                btn.targetGraphic = gobj.AddComponent<Image>();
                btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
                btn.colors = buttonColor;

                text = CreateObject("Text");
                text.transform.SetParent(btn.transform);
                btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = mynum.ToString();
                btnText.color = Color.black;
                btnText.fontSize = 80;
                btnText.alignment = TextAlignmentOptions.Center;

                btn.onClick.AddListener(() => {
                    if(joinCode.text != null && joinCode.text.Length > 6) return;
                    joinCode.text += mynum.ToString();
                });
            }

            gobj = CreateObject("Abort");
            gobj.transform.SetParent(joinPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 150);
            rect.localPosition = new Vector3(400, -70, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "X";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.red;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                joinCode.text = "";
            });

            
            
            gobj = CreateObject("Confirm");
            gobj.transform.SetParent(joinPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 150);
            rect.localPosition = new Vector3(400, -225, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = ">";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.green;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                if(joinCode.text.Length == 0) return;
                StartCoroutine(JoinOnValidCode());
            });

            
            gobj = CreateObject("CurrentCode");
            gobj.transform.SetParent(joinPanel.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 100);
            rect.localPosition = new Vector3(0, 100, 0);
            Image img = gobj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);

            gobj = CreateObject("CurrentCodeText");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 150);
            rect.localPosition = new Vector3(0, 0, 0);
            joinCode = gobj.AddComponent<TextMeshProUGUI>();
            joinCode.color = Color.black;
            joinCode.alignment = TextAlignmentOptions.Center;
            joinCode.fontSize = 90;


            #endregion
            
            #region Host
            gobj = CreateObject("HostPanel");
            gobj.transform.SetParent(transform);
            hostPanel = gobj.AddComponent<RectTransform>();


            text = CreateObject("Text");
            text.transform.SetParent(hostPanel, false);
            rect = text.AddComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 250, 0);
            rect.sizeDelta = new Vector2(810, 50);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Select a server:";
            btnText.fontSize = 50;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;

            gobj = CreateObject("Server Selection");
            gobj.transform.SetParent(hostPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 0, 0);
            rect.sizeDelta = new Vector2(810, 400);
            hostServerToggleGroup = gobj.AddComponent<ToggleGroup>();
            gridLayout = gobj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(400, 80);
            gridLayout.spacing = new Vector2(10, 10);

            /*
            gobj = CreateObject("ButtonPanel");
            gobj.transform.SetParent(hostPanel.transform);
            buttonPanel = gobj.AddComponent<RectTransform>();
            buttonPanel.sizeDelta = new Vector2(775, 310);
            buttonPanel.localPosition = new Vector3(-75, -250, 0);
            gridLayout = gobj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(150, 150);
            gridLayout.spacing = new Vector2(5, 5);
            for (int i = 0; i <= 9; i++) {
                int mynum = i;
                gobj = CreateObject("Button" + mynum);
                gobj.transform.SetParent(buttonPanel);
                btn = gobj.AddComponent<Button>();
                btn.targetGraphic = gobj.AddComponent<Image>();
                btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
                btn.colors = buttonColor;

                text = CreateObject("Text");
                text.transform.SetParent(btn.transform);
                btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = mynum.ToString();
                btnText.color = Color.black;
                btnText.fontSize = 80;
                btnText.alignment = TextAlignmentOptions.Center;

                btn.onClick.AddListener(() => {
                    if(hostCode.text != null && hostCode.text.Length > 6) return;
                    hostCode.text += mynum.ToString();
                });
            }

            gobj = CreateObject("Abort");
            gobj.transform.SetParent(hostPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 150);
            rect.localPosition = new Vector3(400, -170, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "X";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.red;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                hostCode.text = "";
            });



            */
            gobj = CreateObject("Confirm");
            gobj.transform.SetParent(hostPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 150);
            rect.localPosition = new Vector3(0, -325, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            rect = text.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 150);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Create >";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.green;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                StartCoroutine(HostServer());
            });

            /*
            gobj = CreateObject("CodeLabel");
            gobj.transform.SetParent(hostPanel);
            textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "Password";
            textMesh.fontSize = 50;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Right;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 120);
            rect.localPosition = new Vector3(-370, 0, 0);
            
            gobj = CreateObject("CurrentCode");
            gobj.transform.SetParent(hostPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 100);
            rect.localPosition = new Vector3(0, 0, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.6f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                SetHostPinVisible(true);
            });

            gobj = CreateObject("CurrentCodeText");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 150);
            rect.localPosition = new Vector3(0, 0, 0);
            hostCode = gobj.AddComponent<TextMeshProUGUI>();
            hostCode.color = Color.black;
            hostCode.alignment = TextAlignmentOptions.Center;
            hostCode.fontSize = 90;*/

            #endregion
            
            #region Disconnect
            gobj = CreateObject("DisconnectPanel");
            gobj.transform.SetParent(transform);
            disconnectPanel = gobj.AddComponent<RectTransform>();
            
            
            gobj = CreateObject("DisconnectButton");
            gobj.transform.SetParent(disconnectPanel);
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
            btnText.fontWeight = FontWeight.Bold;
            disconnectButton.sizeDelta = new Vector2(500, 150);
            disconnectButton.localPosition = new Vector3(0, -260, 0);

            
            
            gobj = CreateObject("ModerationButton");
            gobj.transform.SetParent(disconnectPanel);
            moderationButton = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.Moderation);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Players";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            moderationButton.sizeDelta = new Vector2(200, 150);
            moderationButton.localPosition = new Vector3(360, -260, 0);
            
            
            


            gobj = CreateObject("JoinCodeInfoLabel");
            gobj.transform.SetParent(disconnectPanel);
            serverJoinCodeLabel = gobj.AddComponent<TextMeshProUGUI>();
            serverJoinCodeLabel.text = "Join Code:";
            serverJoinCodeLabel.fontSize = 50;
            serverJoinCodeLabel.color = Color.black;
            serverJoinCodeLabel.alignment = TextAlignmentOptions.Center;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000, 120);
            rect.localPosition = new Vector3(0, 220, 0);
            
            
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

            #region Connecting

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
            #endregion

            #region Connecting
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
            rect.sizeDelta = new Vector2(500, 200);
            rect.localPosition = new Vector3(0, -260, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                Log.Info(Defines.AMP, "User confirmed the warning!");
                showedWarning = true;
                ShowPage(Page.Serverlist);
            });
            text = CreateObject("Text");
            rect = text.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 200);
            text.transform.SetParent(btn.transform);
            rect.localPosition = new Vector3(0, 0, 0);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "I understand";
            btnText.color = Color.black;
            btnText.fontSize = 80;
            btnText.fontWeight = FontWeight.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            #endregion


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

#if STEAM
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
                if(lastInviteClick != 0 && lastInviteClick > Time.time - 5) return;

                lastInviteClick = Time.time;
                IngameModUI.currentUI.Invite(currentFriend);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            text.transform.localPosition = Vector3.zero;
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Invite";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;
            #endif
            #endregion


            #region Moderation
            gobj = CreateObject("ModerationPanel");
            gobj.transform.SetParent(transform);
            moderationPanel = gobj.AddComponent<RectTransform>();


            gobj = CreateObject("BackButton");
            gobj.transform.SetParent(moderationPanel);
            rect = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = gobj.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
            btn.colors = buttonColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.Disconnect);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Back";
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            rect.sizeDelta = new Vector2(200, 50);
            rect.localPosition = new Vector3(-500, 300, 0);


            moderationPanelPlayerList = CreateScrollRect("Players", false);
            moderationPanelPlayerList.transform.SetParent(transform);
            rect = moderationPanelPlayerList.gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-80, -80);
            rect.localPosition = new Vector3(0, -35, 0);
            







            #endregion

#if AMP
            ThunderRoad.PointerInputModule.SetUICameraToAllCanvas();
            SetupConnectionHandler();
            UpdateConnectionScreen();
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
            if(page == Page.WARNING && showedWarning) {
                page = Page.Serverlist;
            }
            
            currentPage = page;
            
            buttonBar.gameObject.SetActive(true);
            
            serverlist.gameObject.SetActive(false);
            serverInfo.gameObject.SetActive(false);
            #if STEAM
            steamHost.gameObject.SetActive(false);
            steamInvites.gameObject.SetActive(false);
            #endif
            hostPanel.gameObject.SetActive(false);
            joinPanel.gameObject.SetActive(false);
            disconnectPanel.gameObject.SetActive(false);
            connectingPanel.gameObject.SetActive(false);
            moderationPanel.gameObject.SetActive(false);
            moderationPanelPlayerList.gameObject.SetActive(false);
            warningPanel.gameObject.SetActive(false);

            SetHostPinVisible(false);

            #if STEAM
            friendsPanel.gameObject.SetActive(false);
            friendInvitePanel.gameObject.SetActive(false);
            #endif
            switch (page) {
                case Page.Serverlist: {
                        serverlist.gameObject.SetActive(true);
                        serverInfo.gameObject.SetActive(true);
                        StartCoroutine(LoadServerlist());
                        break;
                    }
                #if STEAM
                case Page.SteamHosting: {

                        steamHost.gameObject.SetActive(true);
                        steamInvites.gameObject.SetActive(true);

                        StartCoroutine(LoadInvites());

                        break;
                    }
                #endif
                case Page.Joining: {
                        joinPanel.gameObject.SetActive(true);
                        break;
                    }
                case Page.IpHosting: {
                        StartCoroutine(GetMasterServerData());
                        
                        hostPanel.gameObject.SetActive(true);
                        break;
                    }
                case Page.Disconnect: {
                        buttonBar.gameObject.SetActive(false);
                        disconnectPanel.gameObject.SetActive(true);

                        #if STEAM
                        friendInvitePanel.gameObject.SetActive(false);
                        #if AMP
                        if (ModManager.serverInstance != null && ModManager.serverInstance.netamiteServer is SteamServer) {
                            friendInvitePanel.gameObject.SetActive(true);

                            foreach (Transform t in friendsPanel.content) Destroy(t.gameObject);

                            friendsPanel.gameObject.SetActive(true);
                            
                            foreach(FriendInfo friendInfo in currentlyPlaying) {
                                GameObject obj = friendInfo.GetPrefab();
                                obj.transform.SetParent(friendsPanel.content, false);
                            }
                        }
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
                UpdateFriendsPlaying();
                #endif
                
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

        private Keyboard hostPinKeyboard;
        private void SetHostPinVisible(bool visible) {

            if(visible && hostPinKeyboard == null) {
                Log.Debug(Defines.AMP, "Spawning Host Pin Keyboard");
                
                Catalog.GetData<KeyboardData>("Numpad").SpawnAsync(keyboard => {
                    hostPinKeyboard = keyboard;

                    hostPinKeyboard.OnKey.AddListener(delegate (Keyboard.KeyEvent keyEvent) {
                        // Ignore key release events
                        if (!keyEvent.pressed) {
                            return;
                        }

                        // Do not update the seed, with the keyboard input text, when the cancel key is pressed,
                        // because this key is clearing the keyboard text and would also clear the typed seed
                        if (keyEvent.actionType == KeyActionTypes.Cancel) {
                            return;
                        }

                        hostCode.text = hostPinKeyboard.vrkb.Text;
                        Log.Debug(Defines.AMP, hostPinKeyboard.vrkb.Text);
                    });

                    hostPinKeyboard.OnConfirm.AddListener(result => {
                        SetHostPinVisible(false);
                    });

                    hostPinKeyboard.OnCancel.AddListener(result => {
                        hostCode.text = "";
                        SetHostPinVisible(false);
                    });

                    hostPinKeyboard.Show();
                },
                500,
                "Lobby Pin",
                hostPanel.position + new Vector3(0.05f, 0.001f, 0.08f),
                Quaternion.Euler(hostPanel.eulerAngles + new Vector3(-90, 0, 0)),
                hostPanel);
            }

            if(hostPinKeyboard != null) {
                if(visible) hostPinKeyboard.Show();
                else hostPinKeyboard.Hide();
            }
        }

#if STEAM
        internal IEnumerator LoadInvites() {

            foreach(Transform t in steamInvites) {
                Destroy(t.gameObject);
            }

            GameObject gobj = CreateObject("SteamInviteTitle");
            gobj.transform.SetParent(steamInvites);
            TextMeshProUGUI text = gobj.AddComponent<TextMeshProUGUI>();
            text.text = "Open Invites:";
            text.color = Color.black;

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
#endif
#if STEAM
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
                        tmp.color = Color.black;
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

                rect.sizeDelta = new Vector2(100, 100);

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
                txtRect.sizeDelta = new Vector2(350, 80);
                txtRect.localPosition = new Vector3(-10, 0, 0);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.enableAutoSizing = true;
                tmp.color = Color.black;
                tmp.text = name;
                tmp.fontSize = 30;

                return gobj;
            }
        }
#endif
        private class JoinCodeInfo {
            public string address;
            public short port;
        }

        private class LobbyServerInfo {
#pragma warning disable CS0649
            public string name;
            public string address;
            public string status;
            public int current_lobbies;
            public int max_lobbies;
            public byte utilization;
#pragma warning restore CS0649

            public Sprite GetIcon() {
                // Maybe some day
                return null;
            }

            public String GetUtilization() {
                if(max_lobbies == 0)
                    return "<color=#ff0000>Offline</color>";

                if (utilization > 95)
                    return "<color=#ff0000>Very High</color>";
                if(utilization > 75)
                    return "<color=#fb542b>High</color>";
                if (utilization > 55)
                    return "<color=#ff9f00>Medium</color>";
                
                if (utilization > 30)
                    return "<color=#0045bd>Low</color>";
                return "<color=#0045bd>Very Low</color>";
            }

            public GameObject GetPrefab() {
                GameObject gobj = new GameObject(name);
                Toggle toggle = gobj.AddComponent<Toggle>();
                toggle.targetGraphic = gobj.AddComponent<Image>();
                toggle.targetGraphic.color = new Color(1, 1, 1, 0.6f);
                toggle.colors = buttonColor;
                toggle.group = hostServerToggleGroup;

                GameObject text = new GameObject("Text");
                text.transform.SetParent(toggle.transform);
                RectTransform rect = text.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 30);
                rect.localPosition = new Vector2(0, 10);
                TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = name;
                btnText.fontSize = 40;
                btnText.color = Color.black;
                btnText.fontStyle = FontStyles.Bold;
                btnText.alignment = TextAlignmentOptions.Center;
                
                text = new GameObject("Text");
                text.transform.SetParent(toggle.transform);
                rect = text.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 30);
                rect.localPosition = new Vector2(0, -20);
                btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = GetUtilization();
                btnText.fontSize = 25;
                btnText.color = Color.black;
                btnText.fontStyle = FontStyles.Bold;
                btnText.alignment = TextAlignmentOptions.Center;

                toggle.onValueChanged.AddListener((value) => {
                    if (value) {
                        toggle.targetGraphic.color = buttonColor.selectedColor;
                    } else {
                        toggle.targetGraphic.color = buttonColor.normalColor;
                    }
                });
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
                    name = "<color=#0045bd>" + name + "</color>";
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
                tmp.color = Color.black;
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
                tmp.color = Color.black;
                tmp.text = players_connected + " / " + players_max;

                return gobj;
            }
        }

        List<ServerInfo> servers = new List<ServerInfo>();
        private float lastServerListLoadTime = 0;
        IEnumerator LoadServerlist() {
            if (lastServerListLoadTime > 0 && lastServerListLoadTime > Time.time - 120) yield break;

            Debug.Log($"Getting server list https://{ModManager.safeFile.hostingSettings.masterServerUrl}/list.php");
            servers.Clear();
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/list.php")) {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result) {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Log.Err(Defines.AMP, $"Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Log.Err(Defines.AMP, $"HTTP Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        servers = JsonConvert.DeserializeObject<List<ServerInfo>>(webRequest.downloadHandler.text);
                        lastServerListLoadTime = Time.time;
                        break;
                }
            }
            UpdateServerListDisplay();
        }

        private void UpdateServerListDisplay() {
            foreach(Transform t in serverlist.content) {
                Destroy(t.gameObject);
            }
            
            if(servers.Count == 0) {
                GameObject gobj = CreateObject("Button");
                gobj.transform.SetParent(serverlist.content);
                Image image = gobj.AddComponent<Image>();
                image.color = new Color(1, 1, 1, 0.6f);

                GameObject text = CreateObject("Text");
                text.transform.SetParent(image.transform);
                RectTransform txtRect = text.AddComponent<RectTransform>();
                txtRect.sizeDelta = new Vector2(500, 40);
                TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = "No public servers available.\nYou can still host your own lobby!";
                btnText.fontSize = 20;
                btnText.color = Color.black;
                btnText.alignment = TextAlignmentOptions.Center;
            } else {
                foreach (ServerInfo sv in servers) {
                    GameObject obj = sv.GetPrefab();
                    obj.transform.SetParent(serverlist.content, false);
                }
            }
            
            FixSize();
        }
        
        private void UpdatePlayerList() {
            foreach (Transform t in moderationPanelPlayerList.content) {
                Destroy(t.gameObject);
            }

            GameObject gobj = CreateObject("Player");
            gobj.AddComponent<RectTransform>();
            
            GameObject text = CreateObject("Text");
            text.transform.SetParent(gobj.transform);
            RectTransform txtRect = text.AddComponent<RectTransform>();
            txtRect.sizeDelta = new Vector2(500, 40);
            TextMeshProUGUI tmp = text.AddComponent<TextMeshProUGUI>();
            tmp.text = ModManager.clientInstance.netclient.ClientName + " <color=#0352fc>[You]</color>";
            tmp.fontSize = 50;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;

            gobj.transform.SetParent(moderationPanelPlayerList.content, false);
            
            
            foreach (PlayerNetworkData pnd in ModManager.clientSync.syncData.players.Values) {
                gobj = CreateObject("Player");
                gobj.AddComponent<RectTransform>();
                HorizontalLayoutGroup hlg = gobj.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10;
                hlg.childForceExpandHeight = true;
                hlg.childForceExpandWidth = true;


                text = CreateObject("Text");
                text.transform.SetParent(gobj.transform);
                txtRect = text.AddComponent<RectTransform>();
                txtRect.sizeDelta = new Vector2(200, 40);
                tmp = text.AddComponent<TextMeshProUGUI>();
                tmp.text = pnd.name;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMax = 50;
                tmp.color = Color.black;
                tmp.alignment = TextAlignmentOptions.Center;

                if (ModManager.clientSync.syncData.permissionLevel >= Datatypes.PermissionLevel.NORMAL) {
                    if (ModManager.clientSync.voiceClient != null) {
                        Texture2D muteIcon = new Texture2D(2, 2);
                        muteIcon.LoadImage(AMPResources.mute);
                        Button muteBtn = CreateButton("Mute", ModManager.clientSync.voiceClient.GetClientMute(pnd.clientId) ? Color.red : Color.green, new Vector2(400, 50), muteIcon);
                        muteBtn.onClick.AddListener(() => {
                            ModManager.clientSync.voiceClient.SetClientMute(pnd.clientId, !ModManager.clientSync.voiceClient.GetClientMute(pnd.clientId));
                            UpdatePlayerList();
                        });
                        muteBtn.transform.SetParent(gobj.transform, false);
                        ((RectTransform) muteBtn.transform).sizeDelta = new Vector2(400, 50);
                    }


                    Texture2D hideNameplateIcon = new Texture2D(2, 2);
                    hideNameplateIcon.LoadImage(AMPResources.tag);
                    Button hideNameplateBtn = CreateButton("Nameplate", HealthbarObject.GetNameTagVisibility(pnd.clientId) ? Color.green : Color.red, new Vector2(400, 50), hideNameplateIcon);
                    hideNameplateBtn.onClick.AddListener(() => {
                        HealthbarObject.SetNameTagVisible(pnd.clientId, !HealthbarObject.GetNameTagVisibility(pnd.clientId));
                        HealthbarObject.SetHealthBarVisible(pnd.clientId, HealthbarObject.GetNameTagVisibility(pnd.clientId));
                        UpdatePlayerList();
                    });
                    hideNameplateBtn.transform.SetParent(gobj.transform, false);
                    ((RectTransform)hideNameplateBtn.transform).sizeDelta = new Vector2(400, 50);
                    
                    if (ModManager.clientSync.syncData.permissionLevel < Datatypes.PermissionLevel.LOBBY_ADMIN) {
                        Texture2D voteKickIcon = new Texture2D(2, 2);
                        voteKickIcon.LoadImage(AMPResources.kick);
                        Button voteKickBtn = CreateButton("Vote Kick", Color.black, new Vector2(400, 50), voteKickIcon);
                        voteKickBtn.onClick.AddListener(() => {
                            Log.Debug($"Requesting vote kick of {pnd.name}");
                            new ModerationVoteKickPacket(pnd.clientId).SendToServer();
                        });
                        voteKickBtn.transform.SetParent(gobj.transform, false);
                        ((RectTransform) voteKickBtn.transform).sizeDelta = new Vector2(400, 50);
                    }
                }
                
                if (ModManager.clientSync.syncData.permissionLevel >= Datatypes.PermissionLevel.LOBBY_ADMIN) {
                    Texture2D voteKickIcon = new Texture2D(2, 2);
                    voteKickIcon.LoadImage(AMPResources.kick);
                    Button kickBtn = CreateButton("Kick", Color.black, new Vector2(400, 50), voteKickIcon);
                    kickBtn.transform.SetParent(gobj.transform, false);
                    kickBtn.onClick.AddListener(() => {
                        Log.Debug($"Requesting kick of {pnd.name}");
                        new ModerationKickPacket(pnd.clientId).SendToServer();
                    });
                    ((RectTransform) kickBtn.transform).sizeDelta = new Vector2(400, 50);
                    
                    Texture2D banIcon = new Texture2D(2, 2);
                    banIcon.LoadImage(AMPResources.ban);
                    Button banBtn = CreateButton("Ban", Color.black, new Vector2(400, 50), banIcon);
                    banBtn.transform.SetParent(gobj.transform, false);
                    kickBtn.onClick.AddListener(() => {
                        Log.Debug($"Requesting Ban of {pnd.name}");
                        new ModerationBanPacket(pnd.clientId).SendToServer();
                    });
                    ((RectTransform) banBtn.transform).sizeDelta = new Vector2(400, 50);
                }
                
                if (ModManager.clientSync.syncData.permissionLevel >= Datatypes.PermissionLevel.MODERATOR) {
                    Texture2D banIcon = new Texture2D(2, 2);
                    banIcon.LoadImage(AMPResources.ban);
                    Button banBtn = CreateButton("Glob Ban", Color.black, new Vector2(400, 50), banIcon);
                    banBtn.onClick.AddListener(() => {
                        Log.Debug($"Requesting Global Ban of {pnd.name}");
                    });
                    banBtn.transform.SetParent(gobj.transform, false);
                    ((RectTransform) banBtn.transform).sizeDelta = new Vector2(400, 50);
                }
                
                gobj.transform.SetParent(moderationPanelPlayerList.content, false);
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

            current_PvP.color = currentInfo.pvp == 0 ? new Color(0.67f, 0.12f, 0) : new Color(0, 0.57f, 0);
            current_MapChanging.color = currentInfo.static_map == 0 ? new Color(0.67f, 0.12f, 0) : new Color(0, 0.57f, 0);

            current_Description.ForceMeshUpdate();
            RectTransform rt = current_Description.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, current_Description.textBounds.size.y + 10);
        }
#if STEAM
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
#endif
#if STEAM
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
#endif

        private void BuildServerInfo() {
            GameObject obj;
            RectTransform rt;

            obj = CreateObject("CurrentName");
            current_Name = obj.AddComponent<TextMeshProUGUI>();
            current_Name.transform.SetParent(serverInfo);
            current_Name.enableAutoSizing = true;
            current_Name.fontStyle = FontStyles.Bold;
            current_Name.alignment = TextAlignmentOptions.Center;
            current_Name.color = Color.black;
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
            current_Description.color = Color.black;
            current_Description.alignment = TextAlignmentOptions.TopJustified;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 150);

            obj = CreateObject("MapInfo");
            current_MapInfo = obj.AddComponent<TextMeshProUGUI>();
            current_MapInfo.transform.SetParent(serverInfo);
            current_MapInfo.enableAutoSizing = true;
            current_MapInfo.fontSizeMax = 36;
            current_MapInfo.color = Color.black;
            current_MapInfo.alignment = TextAlignmentOptions.Center;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 40);

            obj = CreateObject("PlayerCount");
            current_PlayerCount = obj.AddComponent<TextMeshProUGUI>();
            current_PlayerCount.transform.SetParent(serverInfo);
            current_PlayerCount.enableAutoSizing = true;
            current_PlayerCount.fontSizeMax = 36;
            current_PlayerCount.color = Color.black;
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
            current_MapChanging.text = "Changeable Maps";
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 40);

            obj = CreateObject("Address");
            current_Address = obj.AddComponent<TextMeshProUGUI>();
            current_Address.transform.SetParent(serverInfo);
            current_Address.enableAutoSizing = true;
            current_Address.fontSizeMax = 36;
            current_Address.color = Color.black;
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
            else if ("CONNECTION_CLOSED".Equals(reason)) yield break;


            ShowPage(Page.Connecting);
            connectingMessage.text = $"Connection failed.\n\nReason: {reason}";
            connectingMessage.color = Color.red;

            yield return new WaitForSeconds(5);
            ShowPage(Page.Serverlist);
        }
        
        private void DoConnect() {
            serverJoinCodeLabel.gameObject.SetActive(false);
            serverJoinCodeMessage.gameObject.SetActive(false);
#if AMP
            GUIManager.JoinServer(currentInfo.address, currentInfo.port.ToString());
#endif
        }
#if STEAM
        private void DoConnect(ulong lobbyId) {
#if AMP 
            ShowPage(Page.Connecting);
            connectingMessage.text = "Connecting to Steam Lobby...";

            serverJoinCodeMessage.text = "";
            ModManager.JoinSteam(lobbyId);
            
            ModManager.instance.invites.RemoveAll((invite) => invite.lobbyId == lobbyId);
            StartCoroutine(LoadInvites());
#endif
        }

        private void Invite(FriendInfo friendInfo) {
#if AMP
            if(ModManager.serverInstance != null && ModManager.serverInstance.netamiteServer is SteamServer) {
                Log.Debug($"Invite to lobby {((SteamServer) ModManager.serverInstance.netamiteServer).currentLobby.LobbyId} sent to {friendInfo.steamId}.");
                SteamMatchmaking.InviteUserToLobby(((SteamServer)ModManager.serverInstance.netamiteServer).currentLobby.LobbyId, (CSteamID)friendInfo.steamId);
            }
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
#endif
        private GameObject CreateObject(string obj) {
            GameObject go = new GameObject(obj);
            go.transform.parent = transform;
            go.transform.localScale = Vector3.one;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localPosition = Vector3.zero;
            return go;
        }
        
        private Button CreateButton(string obj, Color color, Vector2 size, Texture2D icon = null) {
            GameObject go = new GameObject(obj);
            go.transform.parent = transform;
            go.transform.localScale = Vector3.one;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localPosition = Vector3.zero;

            RectTransform rt = go.AddComponent<RectTransform>();
            Button btn = go.AddComponent<Button>();
            rt.sizeDelta = size;
            
            btn.targetGraphic = go.AddComponent<Image>();
            btn.targetGraphic.color = new Color(1, 1, 1, 0.3f);
            btn.colors = buttonColor;
            
            
            GameObject txtObj = CreateObject("Text");
            RectTransform rtText = txtObj.AddComponent<RectTransform>();
            rtText.sizeDelta = size;
            rtText.localPosition = Vector2.zero;
            TextMeshProUGUI btnText = txtObj.AddComponent<TextMeshProUGUI>();
            btnText.color = color;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            btnText.fontSize = 40;
            btnText.text = obj;
            txtObj.transform.SetParent(btn.transform);
            
            if(icon != null) {
                float min = Math.Min(size.x, size.y);
                
                GameObject iconObj = CreateObject("Text");
                RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
                rtIcon.sizeDelta = Vector2.one * min * 1.4f;
                rtIcon.localPosition = new Vector2(0, 0.4f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.one / 2);
                iconImg.color = color;
                
                btnText.fontSize = 20;
                btnText.fontStyle = FontStyles.Bold;
                rtText.localPosition = new Vector2(0, size.y * -0.8f);

                iconObj.transform.SetParent(btn.transform);
            }
            
            
            return btn;
        }

        List<LobbyServerInfo> lobby_servers = new List<LobbyServerInfo>();
        private float lastMasterDataLoadTime = 0;
        private IEnumerator GetMasterServerData() {
            if (lastMasterDataLoadTime > 0 && lastMasterDataLoadTime > Time.time - 120) yield break;
            
            foreach (Transform t in hostServerToggleGroup.transform) {
                Destroy(t.gameObject);
            }
            
            GameObject gobj = CreateObject("Button");
            gobj.transform.SetParent(hostServerToggleGroup.transform);
            Image image = gobj.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.6f);

            GameObject text = CreateObject("Text");
            text.transform.SetParent(gobj.transform);
            TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Loading...";
            btnText.fontSize = 40;
            btnText.color = Color.black;
            btnText.alignment = TextAlignmentOptions.Center;

            yield return new WaitForSeconds(1);


            Debug.Log($"Getting lobby server list https://{ModManager.safeFile.hostingSettings.masterServerUrl}/host/hostservers.php");
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/host/hostservers.php")) {
                yield return webRequest.SendWebRequest();

                //Log.Debug(webRequest.downloadHandler.text);
                switch (webRequest.result) {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Log.Err(Defines.AMP, $"Error while getting lobby servers: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Log.Err(Defines.AMP, $"HTTP Error while getting lobby servers: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        lobby_servers = JsonConvert.DeserializeObject<List<LobbyServerInfo>>(webRequest.downloadHandler.text);
                        lastMasterDataLoadTime = Time.time;
                        
                        break;
                }
            }
            
            yield return RebuildHostingList();
        }

        private IEnumerator RebuildHostingList() {
            foreach(Transform t in hostServerToggleGroup.transform) {
                Destroy(t.gameObject);
            }
            
            foreach (LobbyServerInfo server in lobby_servers) {
                GameObject obj = server.GetPrefab();
                obj.transform.SetParent(hostServerToggleGroup.transform, false);
            }

            yield break;
        }

        private IEnumerator JoinOnValidCode() {
            ShowPage(Page.Connecting);
            
            string code = joinCode.text.Trim();

            JoinCodeInfo info = null;
            connectingMessage.text = "Getting Server Info...";
            yield return GetAddressForCode(code, (joincodeinfo) => { info = joincodeinfo; });
            
            yield return new WaitForSeconds(5); // Give the server some time to start up
            
            if (info != null && info.address != null && info.address.Length > 0) {
                serverJoinCodeMessage.text = code;
                serverJoinCodeLabel.gameObject.SetActive(true);
                serverJoinCodeMessage.gameObject.SetActive(true);

                connectingMessage.text = "Connecting to server...";
                
                yield return new WaitForSeconds(1); // Give the server some time to start up

                GUIManager.JoinServer(info.address, info.port.ToString());
            } else {
                connectingMessage.text = "Getting Server Info failed! Is the code correct?";
                connectingMessage.color = Color.red;
                
                yield return new WaitForSeconds(5);
                
                ShowPage(Page.Joining);
            }
        }
        
        private IEnumerator GetAddressForCode(string code, System.Action<JoinCodeInfo> callback) {
            Log.Debug("Requesting Info for join code " + code);
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/ping/join_code.php?code={code}&version={Defines.FULL_MOD_VERSION.Replace(" ", "")}")) {
                yield return webRequest.SendWebRequest();

                Log.Debug(webRequest.downloadHandler.text);
                switch (webRequest.result) {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        //Log.Err(Defines.AMP, $"Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        //Log.Err(Defines.AMP, $"HTTP Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        JoinCodeInfo jci = JsonConvert.DeserializeObject<JoinCodeInfo>(webRequest.downloadHandler.text);
                        Log.Debug("Join Code resolved to " + jci.address + ":" + jci.port);
                        
                        callback(jci);
                        yield break;
                }
            }
            callback(new JoinCodeInfo());
        }
        
        private IEnumerator HostServer() {
            string code = "";
            int server_index = 0;
            
            if(hostServerToggleGroup.ActiveToggles().Count() > 0) {
                server_index = hostServerToggleGroup.ActiveToggles().First().transform.GetSiblingIndex();
            }
            
            LobbyServerInfo lobby_server = lobby_servers[server_index];

            ShowPage(Page.Connecting);
            
            Log.Debug("Requesting Lobby on " + lobby_server.name);

            connectingMessage.text = $"Requesting lobby on {lobby_server.name}...";

            string map = "";
            string mode = "";
            
            bool levelInfoSuccess = LevelInfo.ReadLevelInfo(out map, out mode, out _);
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{ lobby_server.address }/run_server?map={map}&mode={mode}&version={Defines.FULL_MOD_VERSION.Replace(" ", "")}")) {
                yield return webRequest.SendWebRequest();
                
                switch (webRequest.result) {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        //Log.Err(Defines.AMP, $"Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        //Log.Err(Defines.AMP, $"HTTP Error while getting server list: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        code = webRequest.downloadHandler.text;
                        if (code == "false") {
                            code = "";
                        } else if (code.Length > 10) {
                            connectingMessage.text = $"{code}";
                            connectingMessage.color = Color.red;
                            code = "";

                            yield return new WaitForSeconds(5);

                            ShowPage(Page.IpHosting);
                            yield break;
                        }
                        
                        Log.Debug("Join Code received " + code);
                        break;
                }
            }
            
            if(code.Length > 0) {
                yield return new WaitForSeconds(3); // Give the server some time to start up

                connectingMessage.text = "Getting Server Info...";
                
                JoinCodeInfo info = null;
                yield return GetAddressForCode(code, (joincodeinfo) => { info = joincodeinfo; });
                
                yield return new WaitForSeconds(3); // Give the server some time to start up

                if (info != null && info.address != null && info.address.Length > 0) {
                    serverJoinCodeMessage.text = code;
                    serverJoinCodeLabel.gameObject.SetActive(true);
                    serverJoinCodeMessage.gameObject.SetActive(true);

                    connectingMessage.text = "Connecting to server...";
                    
                    GUIManager.JoinServer(info.address, info.port.ToString());

                    yield break;
                }
            }
            connectingMessage.text = "Lobby creation failed, please try again later.";
            connectingMessage.color = Color.red;
            
            yield return new WaitForSeconds(5);
            
            ShowPage(Page.IpHosting);
        }
    }
}