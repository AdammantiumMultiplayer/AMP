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
    // Host pages: hosting through the master server and local hosting
    public partial class IngameModUI {

        RectTransform hostPanel;
        RectTransform hostButtonBar;
        //TextMeshProUGUI hostCode;

        // Master server hosting
        RectTransform lobbyDropdownList;
        TextMeshProUGUI lobbyDropdownLabel;
        int selectedLobbyServer = -1;
        TextMeshProUGUI masterHostMaxPlayers;
        uint masterHostMaxPlayerCount = 4;
        bool masterHostPvp = true;

        RectTransform localHostPanel;
        TextMeshProUGUI localHostMaxPlayers;
        TextMeshProUGUI localHostPort;
        TextMeshProUGUI localHostPassword;
        Image localHostPortField;
        Image localHostPasswordField;
        uint localHostMaxPlayerCount = 4;
        LocalHostSelection currentLocalHostSelection = LocalHostSelection.Port;
        enum LocalHostSelection {
            Port = 0,
            Password = 1
        }

        private void CreateHostPanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            TextMeshProUGUI btnText;
            VerticalLayoutGroup vlg;
            Image img;

            hostButtonBar = CreateSubTabBar("HostButtonbar");

            // Second tab group, the hosting method
            hostTabGroup = hostButtonBar.gameObject.AddComponent<ToggleGroup>();
            hostTabGroup.allowSwitchOff = true;
            CreateSubTab(hostButtonBar, hostTabGroup, "HostMasterServer", "Master-Server", Page.IpHosting);
            #if STEAM
            Toggle steamTab = CreateSubTab(hostButtonBar, hostTabGroup, "HostSteam", (SteamIntegration.IsInitialized ? "Steam" : "<line-height=35%>Steam\n<b><color=\"red\"><size=60%>Not available"), Page.SteamHosting);
            steamTab.interactable = SteamIntegration.IsInitialized;
            #endif
            CreateSubTab(hostButtonBar, hostTabGroup, "HostLocal", "Local Server", Page.LocalHosting);

            gobj = CreateObject("HostPanel");
            gobj.transform.SetParent(transform);
            hostPanel = gobj.AddComponent<RectTransform>();


            // Server: dropdown with the lobby servers of the master server
            CreateRowLabel(hostPanel, "Server:", 160);

            gobj = CreateObject("ServerDropdown");
            gobj.transform.SetParent(hostPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 80);
            rect.localPosition = new Vector3(120, 160, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;
            btn.onClick.AddListener(() => {
                SetLobbyDropdownOpen(!lobbyDropdownList.gameObject.activeSelf);
            });

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            rect = text.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20, 0);
            rect.offsetMax = new Vector2(-60, 0); // Space for the arrow
            lobbyDropdownLabel = text.AddComponent<TextMeshProUGUI>();
            lobbyDropdownLabel.text = "Loading...";
            lobbyDropdownLabel.enableAutoSizing = true;
            lobbyDropdownLabel.fontSizeMax = 36;
            lobbyDropdownLabel.color = tabTextColor;
            lobbyDropdownLabel.alignment = TextAlignmentOptions.Center;

            text = CreateObject("Arrow");
            text.transform.SetParent(btn.transform, false);
            rect = text.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(50, 50);
            rect.localPosition = new Vector3(215, 0, 0);
            rect.localEulerAngles = new Vector3(0, 0, 90); // "<" turned to point down
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "<";
            btnText.fontSize = 40;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = tabTextColor;
            btnText.alignment = TextAlignmentOptions.Center;

            // Max players, changed with - / +
            CreateRowLabel(hostPanel, "Max Players:", 60);
            masterHostMaxPlayerCount = (uint) Mathf.Clamp(ModManager.safeFile.inputCache.host_max_players, 2, Defines.MAX_PLAYERS);

            gobj = CreateObject("MaxPlayers");
            gobj.transform.SetParent(hostPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 80);
            rect.localPosition = new Vector3(120, 60, 0);
            img = gobj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);

            text = CreateObject("Text");
            text.transform.SetParent(rect, false);
            rect = text.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            masterHostMaxPlayers = text.AddComponent<TextMeshProUGUI>();
            masterHostMaxPlayers.text = masterHostMaxPlayerCount.ToString();
            masterHostMaxPlayers.fontSize = 50;
            masterHostMaxPlayers.color = Color.black;
            masterHostMaxPlayers.alignment = TextAlignmentOptions.Center;

            CreateKey(hostPanel, "Less", "-", new Vector2(70, 70), new Vector3(-90, 60, -1), keyTextColor, () => {
                if(masterHostMaxPlayerCount > 2) masterHostMaxPlayerCount--;
                masterHostMaxPlayers.text = masterHostMaxPlayerCount.ToString();
            });
            CreateKey(hostPanel, "More", "+", new Vector2(70, 70), new Vector3(330, 60, -1), keyTextColor, () => {
                if(masterHostMaxPlayerCount < Defines.MAX_PLAYERS) masterHostMaxPlayerCount++;
                masterHostMaxPlayers.text = masterHostMaxPlayerCount.ToString();
            });

            // PvP, on by default
            CreateRowLabel(hostPanel, "PvP:", -40);

            CreateEnabledToggle(hostPanel, "PvP", new Vector3(120, -40, 0), masterHostPvp, (value) => masterHostPvp = value);

            // Options of the dropdown, created last so they are drawn above the rows below
            gobj = CreateObject("ServerDropdownList");
            gobj.transform.SetParent(hostPanel);
            lobbyDropdownList = gobj.AddComponent<RectTransform>();
            lobbyDropdownList.pivot = new Vector2(0.5f, 1f);
            lobbyDropdownList.sizeDelta = new Vector2(500, 0);
            lobbyDropdownList.localPosition = new Vector3(120, 160 - 45, -2); // Right below the dropdown
            img = gobj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.09f, 0.03f, 0.95f);
            vlg = gobj.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.spacing = 5;
            gobj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            gobj.SetActive(false);

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
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            rect = text.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 150);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Create";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = Color.black;
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
        }

        private void CreateLocalHostPanel() {
            GameObject gobj;

            // Same as the host menu of the GUIManager: max players, port and password, then host and join it
            gobj = CreateObject("LocalHostPanel");
            gobj.transform.SetParent(transform);
            localHostPanel = gobj.AddComponent<RectTransform>();

            localHostMaxPlayerCount = (uint) Mathf.Clamp(ModManager.safeFile.inputCache.host_max_players, 2, Defines.MAX_PLAYERS);
            localHostMaxPlayers = CreateInputField(localHostPanel, "MaxPlayers", "Max Players:", -420, out _);
            localHostMaxPlayers.text = localHostMaxPlayerCount.ToString();

            localHostPort = CreateInputField(localHostPanel, "Port", "Port:", -10, out localHostPortField);
            localHostPort.text = ModManager.safeFile.inputCache.host_port.ToString();
            localHostPortField.GetComponent<Button>().onClick.AddListener(() => SelectLocalHostField(LocalHostSelection.Port));

            localHostPassword = CreateInputField(localHostPanel, "Password", "Password:", 400, out localHostPasswordField);
            localHostPassword.text = ModManager.safeFile.inputCache.host_password;
            localHostPasswordField.GetComponent<Button>().onClick.AddListener(() => SelectLocalHostField(LocalHostSelection.Password));
            SelectLocalHostField(LocalHostSelection.Port);

            // Max players are changed with - / + on the sides of the field
            CreateKey(localHostPanel, "Less", "-", new Vector2(70, 70), new Vector3(-580, 140, -1), keyTextColor, () => {
                if(localHostMaxPlayerCount > 2) localHostMaxPlayerCount--;
                localHostMaxPlayers.text = localHostMaxPlayerCount.ToString();
            });
            CreateKey(localHostPanel, "More", "+", new Vector2(70, 70), new Vector3(-260, 140, -1), keyTextColor, () => {
                if(localHostMaxPlayerCount < Defines.MAX_PLAYERS) localHostMaxPlayerCount++;
                localHostMaxPlayers.text = localHostMaxPlayerCount.ToString();
            });

            CreateKeyboard(localHostPanel, (key) => {
                switch(currentLocalHostSelection) {
                    case LocalHostSelection.Port:
                        if(localHostPort.text.Length >= 5 || !char.IsDigit(key)) return;
                        localHostPort.text += key;
                        break;
                    case LocalHostSelection.Password:
                        if(localHostPassword.text.Length >= 10) return; // Same limit as the GUIManager
                        localHostPassword.text += key;
                        break;
                }
            });

            CreateKey(localHostPanel, "Backspace", "<", new Vector2(150, 150), new Vector3(400, -55, 0), keyCancelColor, () => {
                TextMeshProUGUI field = currentLocalHostSelection == LocalHostSelection.Port ? localHostPort : localHostPassword;
                if(field.text.Length > 0) field.text = field.text.Remove(field.text.Length - 1);
            });

            CreateKey(localHostPanel, "Confirm", ">", new Vector2(150, 150), new Vector3(400, -240, 0), keyConfirmColor, () => {
                StartCoroutine(HostLocalServer());
            });
        }
    
        /*
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
        }*/

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

            public GameObject GetPrefab(bool selected, UnityEngine.Events.UnityAction onClick) {
                GameObject gobj = new GameObject(name);
                gobj.AddComponent<RectTransform>();
                gobj.AddComponent<LayoutElement>().preferredHeight = 80;
                Button button = gobj.AddComponent<Button>();
                button.targetGraphic = CreateTabBackground(gobj);
                // The tint can only darken the texture, so the selected one gets the golden tint
                button.colors = selected ? tabSelectedColor : tabColor;
                button.onClick.AddListener(onClick);

                GameObject text = new GameObject("Text");
                text.transform.SetParent(button.transform);
                RectTransform rect = text.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 30);
                rect.localPosition = new Vector2(0, 10);
                TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = name;
                btnText.fontSize = 40;
                btnText.color = tabTextColor;
                btnText.fontStyle = FontStyles.Bold;
                btnText.alignment = TextAlignmentOptions.Center;
                
                text = new GameObject("Text");
                text.transform.SetParent(button.transform);
                rect = text.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 30);
                rect.localPosition = new Vector2(0, -20);
                btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = GetUtilization();
                btnText.fontSize = 25;
                btnText.color = tabTextColor;
                btnText.fontStyle = FontStyles.Bold;
                btnText.alignment = TextAlignmentOptions.Center;

                return gobj;
            }
        }

        private void SelectLocalHostField(LocalHostSelection selection) {
            currentLocalHostSelection = selection;
            SetFieldSelected(localHostPortField,     selection == LocalHostSelection.Port);
            SetFieldSelected(localHostPasswordField, selection == LocalHostSelection.Password);
        }

        private IEnumerator HostLocalServer() {
            ShowPage(Page.Connecting);

            ushort port;
            if(!ushort.TryParse(localHostPort.text, out port) || port <= 0 || port > 65535) {
                connectingMessage.text = "Please enter a valid port (1 - 65535).";
                connectingMessage.color = Color.red;

                yield return new WaitForSeconds(5);

                ShowPage(Page.LocalHosting);
                yield break;
            }

            connectingMessage.text = "Starting server...";
            serverJoinCodeMessage.text = "";
            serverJoinCodeLabel.gameObject.SetActive(false);
            serverJoinCodeMessage.gameObject.SetActive(false);

            yield return new WaitForSeconds(1);
#if AMP
            // Hosts and joins it, the connection handler updates the screen afterwards
            GUIManager.HostServer(localHostMaxPlayerCount, port, localHostPassword.text);
#endif
            UpdateConnectionScreen();
        }

        List<LobbyServerInfo> lobby_servers = new List<LobbyServerInfo>();
        private float lastMasterDataLoadTime = 0;
        private IEnumerator GetMasterServerData() {
            if (lastMasterDataLoadTime > 0 && lastMasterDataLoadTime > Time.time - 120) yield break;
            
            SetLobbyDropdownOpen(false);
            foreach (Transform t in lobbyDropdownList) {
                Destroy(t.gameObject);
            }
            lobbyDropdownLabel.text = "Loading...";

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
            if(lobby_servers == null) lobby_servers = new List<LobbyServerInfo>();

            // Restore the last used server, if it still exists, otherwise keep the current one or take the first
            int lastUsed = lobby_servers.FindIndex(server => server.name == ModManager.safeFile.inputCache.host_master_server);
            if(lastUsed >= 0) {
                selectedLobbyServer = lastUsed;
            } else if(selectedLobbyServer < 0 || selectedLobbyServer >= lobby_servers.Count) {
                selectedLobbyServer = lobby_servers.Count > 0 ? 0 : -1;
            }

            UpdateLobbyDropdown();
            yield break;
        }

        private void UpdateLobbyDropdown() {
            foreach(Transform t in lobbyDropdownList) {
                Destroy(t.gameObject);
            }

            for(int i = 0; i < lobby_servers.Count; i++) {
                int index = i;
                GameObject obj = lobby_servers[i].GetPrefab(index == selectedLobbyServer, () => {
                    selectedLobbyServer = index;
                    ModManager.safeFile.inputCache.host_master_server = lobby_servers[index].name;
                    ModManager.safeFile.Save();

                    SetLobbyDropdownOpen(false);
                    UpdateLobbyDropdown();
                });
                obj.transform.SetParent(lobbyDropdownList, false);
            }

            if(selectedLobbyServer >= 0 && selectedLobbyServer < lobby_servers.Count) {
                LobbyServerInfo server = lobby_servers[selectedLobbyServer];
                lobbyDropdownLabel.text = $"{server.name} <size=70%>({server.GetUtilization()})";
            } else {
                lobbyDropdownLabel.text = "No servers available";
            }
            FixSize(lobbyDropdownList);
        }

        private void SetLobbyDropdownOpen(bool open) {
            if(lobbyDropdownList == null) return;
            if(open && lobby_servers.Count == 0) open = false;
            lobbyDropdownList.gameObject.SetActive(open);
            if(open) lobbyDropdownList.SetAsLastSibling(); // Drawn above everything else of the page
        }
        
        private IEnumerator HostServer() {
            string code = "";

            if(selectedLobbyServer < 0 || selectedLobbyServer >= lobby_servers.Count) {
                ShowPage(Page.Connecting);
                connectingMessage.text = "No master server available, please try again later.";
                connectingMessage.color = Color.red;

                yield return new WaitForSeconds(5);

                ShowPage(Page.IpHosting);
                yield break;
            }

            LobbyServerInfo lobby_server = lobby_servers[selectedLobbyServer];

            ModManager.safeFile.inputCache.host_master_server = lobby_server.name;
            ModManager.safeFile.inputCache.host_max_players   = masterHostMaxPlayerCount;
            ModManager.safeFile.Save();

            ShowPage(Page.Connecting);
            
            Log.Debug("Requesting Lobby on " + lobby_server.name);

            connectingMessage.text = $"Requesting lobby on {lobby_server.name}...";

            string map = "";
            string mode = "";
            
            bool levelInfoSuccess = LevelInfo.ReadLevelInfo(out map, out mode, out _);
            
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{ lobby_server.address }/run_server?map={map}&mode={mode}&version={Defines.FULL_MOD_VERSION.Replace(" ", "")}&max_players={masterHostMaxPlayerCount}&pvp={(masterHostPvp ? "true" : "false")}")) {
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
