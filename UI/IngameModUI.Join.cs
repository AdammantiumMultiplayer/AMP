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
    // Join pages: join by code and direct join by ip
    public partial class IngameModUI {

        RectTransform joinButtonBar;
        RectTransform joinPanel;
        RectTransform joinCodePanel;
        TextMeshProUGUI joinCode;

        RectTransform joinIpPanel;
        TextMeshProUGUI joinIpAddress;
        TextMeshProUGUI joinIpPort;
        TextMeshProUGUI joinIpPassword;
        Image joinIpAddressField;
        Image joinIpPortField;
        Image joinIpPasswordField;
        bool isShifted = false;
        JoinIpSelection currentJoinIpSelection = JoinIpSelection.Address;
        enum JoinIpSelection
        {
            Address = 0,
            Port = 1,
            Password = 2
        }

        private void CreateJoinPanel() {
            GameObject gobj;

            gobj = CreateObject("JoinPanel");
            gobj.transform.SetParent(transform);
            joinPanel = gobj.AddComponent<RectTransform>();

            joinButtonBar = CreateSubTabBar("JoinButtonbar");


            // Second tab group, the join method
            joinTabGroup = joinButtonBar.gameObject.AddComponent<ToggleGroup>();
            joinTabGroup.allowSwitchOff = true;
            CreateSubTab(joinButtonBar, joinTabGroup, "JoinCode", "Use Code",    Page.CodeJoining);
            CreateSubTab(joinButtonBar, joinTabGroup, "JoinIp",   "Direct (IP)", Page.IpJoining);
            #if STEAM
            Toggle steamInvitesTab = CreateSubTab(joinButtonBar, joinTabGroup, "JoinSteam", (SteamIntegration.IsInitialized ? "Steam Invites" : "<line-height=35%>Steam Invites\n<b><color=\"red\"><size=60%>Not available"), Page.SteamJoining);
            steamInvitesTab.interactable = SteamIntegration.IsInitialized;
            #endif
        }

        private void CreateJoinCodePanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            TextMeshProUGUI btnText;

            gobj = CreateObject("JoinCodePanel");
            gobj.transform.SetParent(joinPanel);
            joinCodePanel = gobj.AddComponent<RectTransform>();
            
            gobj = CreateObject("JoinCodeInfoLabel");
            gobj.transform.SetParent(joinCodePanel);
            TextMeshProUGUI textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "Join Code:";
            textMesh.fontSize = 50;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Center;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 200);
            rect.localPosition = new Vector3(0, 180, 0);

            gobj = CreateObject("ButtonPanel");
            gobj.transform.SetParent(joinCodePanel.transform);
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
                btn.targetGraphic = CreateKeyBackground(gobj);
                btn.colors = keyColor;

                text = CreateObject("Text");
                text.transform.SetParent(btn.transform);
                btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = mynum.ToString();
                btnText.color = keyTextColor;
                btnText.fontSize = 80;
                btnText.alignment = TextAlignmentOptions.Center;

                btn.onClick.AddListener(() => {
                    if(joinCode.text != null && joinCode.text.Length > 6) return;
                    joinCode.text += mynum.ToString();
                });
            }

            gobj = CreateObject("Abort");
            gobj.transform.SetParent(joinCodePanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 150);
            rect.localPosition = new Vector3(400, -70, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateKeyBackground(gobj);
            btn.colors = keyColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "X";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = keyCancelColor;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                joinCode.text = "";
            });

            
            
            gobj = CreateObject("Confirm");
            gobj.transform.SetParent(joinCodePanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 150);
            rect.localPosition = new Vector3(400, -225, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateKeyBackground(gobj);
            btn.colors = keyColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = ">";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = keyConfirmColor;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                if(joinCode.text.Length == 0) return;
                StartCoroutine(JoinOnValidCode());
            });

            
            gobj = CreateObject("CurrentCode");
            gobj.transform.SetParent(joinCodePanel.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 100);
            rect.localPosition = new Vector3(0, 80, 0);
            Image img = gobj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);

            gobj = CreateObject("CurrentCodeText");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 150);
            rect.localPosition = new Vector3(0, 0, -20);
            joinCode = gobj.AddComponent<TextMeshProUGUI>();
            joinCode.color = Color.black;
            joinCode.alignment = TextAlignmentOptions.Center;
            joinCode.fontSize = 90;
        }

        private void CreateJoinIpPanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            TextMeshProUGUI btnText;
            TextMeshProUGUI textMesh;
            Image img;

            gobj = CreateObject("JoinIpPanel");
            gobj.transform.SetParent(joinPanel);
            joinIpPanel = gobj.AddComponent<RectTransform>();


            gobj = CreateObject("KeyboardPanel");
            gobj.transform.SetParent(joinIpPanel.transform);
            RectTransform keyboardPanel = gobj.AddComponent<RectTransform>();
            keyboardPanel.sizeDelta = new Vector2(850, 425);
            keyboardPanel.localPosition = new Vector3(-120, -150, 0);
            GridLayoutGroup keyboardLayout = gobj.AddComponent<GridLayoutGroup>();
            keyboardLayout.cellSize = new Vector2(80, 80);
            keyboardLayout.spacing = new Vector2(5, 5);
            char[] keyboard = {
                '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
                'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p',
                'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'z',
                'x', 'c', 'v', 'b', 'n', 'm', '-', '=', '[', ']',
                '/', '\\', ';', '\'', ',', '.', '`'
            };
            foreach (char key in keyboard) {
                char mykey = key;
                gobj = CreateObject("Key" + mykey);
                gobj.transform.SetParent(keyboardPanel);
                btn = gobj.AddComponent<Button>();
                btn.targetGraphic = CreateKeyBackground(gobj);
                btn.colors = keyColor;

                text = CreateObject("Text");
                text.transform.SetParent(btn.transform);
                btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = mykey.ToString();
                btnText.color = keyTextColor;
                btnText.fontSize = 60;
                btnText.rectTransform.sizeDelta = new Vector2(80, 80);
                btnText.alignment = TextAlignmentOptions.Center;

                btn.onClick.AddListener(() => {
                    switch (currentJoinIpSelection) {
                        case JoinIpSelection.Address:
                            joinIpAddress.text += mykey;
                            break;
                        case JoinIpSelection.Port:
                            if (joinIpPort.text.Length >= 5 || !char.IsDigit(mykey)) return;
                            joinIpPort.text += mykey;
                            break;
                        case JoinIpSelection.Password:
                            joinIpPassword.text += mykey;
                            break;
                    }
                });
            }

            gobj = CreateObject("KeyboardPanel");
            gobj.transform.SetParent(joinIpPanel.transform);
            RectTransform shiftedKeyboardPanel = gobj.AddComponent<RectTransform>();
            shiftedKeyboardPanel.sizeDelta = new Vector2(850, 425);
            shiftedKeyboardPanel.localPosition = new Vector3(-120, -150, 0);
            GridLayoutGroup shiftedKeyboardLayout = gobj.AddComponent<GridLayoutGroup>();
            shiftedKeyboardLayout.cellSize = new Vector2(80, 80);
            shiftedKeyboardLayout.spacing = new Vector2(5, 5);
            char[] shiftedKeyboard = {
                '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
                'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',
                'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'Z',
                'X', 'C', 'V', 'B', 'N', 'M', '_', '+', '{', '}',
                '?', '|', ':', '"', '<', '>', '~'
            };
            foreach (char key in shiftedKeyboard) {
                char mykey = key;
                gobj = CreateObject("Key" + mykey);
                gobj.transform.SetParent(shiftedKeyboardPanel);
                btn = gobj.AddComponent<Button>();
                btn.targetGraphic = CreateKeyBackground(gobj);
                btn.colors = keyColor;

                text = CreateObject("Text");
                text.transform.SetParent(btn.transform);
                btnText = text.AddComponent<TextMeshProUGUI>();
                btnText.text = mykey.ToString();
                btnText.color = keyTextColor;
                btnText.fontSize = 60;
                btnText.alignment = TextAlignmentOptions.Center;

                btn.onClick.AddListener(() => {
                    switch (currentJoinIpSelection) {
                        case JoinIpSelection.Address:
                            joinIpAddress.text += mykey;
                            break;
                        case JoinIpSelection.Port:
                            if (joinIpPort.text.Length >= 5 || !char.IsDigit(mykey)) return;
                            joinIpPort.text += mykey;
                            break;
                        case JoinIpSelection.Password:
                            joinIpPassword.text += mykey;
                            break;
                    }
                });
            }
            shiftedKeyboardPanel.gameObject.SetActive(false);


            gobj = CreateObject("IpAddressInfoLabel");
            gobj.transform.SetParent(joinIpPanel);
            textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "Address:";
            textMesh.fontSize = 30;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Center;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);
            rect.localPosition = new Vector3(-415, 200, 0);

            gobj = CreateObject("PortInfoLabel");
            gobj.transform.SetParent(joinIpPanel);
            textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "Port:";
            textMesh.fontSize = 30;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Center;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);
            rect.localPosition = new Vector3(-10, 200, 0);

            gobj = CreateObject("PasswordInfoLabel");
            gobj.transform.SetParent(joinIpPanel);
            textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = "Password:";
            textMesh.fontSize = 30;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Center;
            rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);
            rect.localPosition = new Vector3(400, 200, 0);


            gobj = CreateObject("Abort");
            gobj.transform.SetParent(joinIpPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 150);
            rect.localPosition = new Vector3(400, -55, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateKeyBackground(gobj);
            btn.colors = keyColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "<";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = keyCancelColor;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                switch (currentJoinIpSelection) {
                    case JoinIpSelection.Address:
                        joinIpAddress.text = joinIpAddress.text.Remove(joinIpAddress.text.Length - 1);
                        break;
                    case JoinIpSelection.Port:
                        joinIpPort.text = joinIpPort.text.Remove(joinIpPort.text.Length - 1);
                        break;
                    case JoinIpSelection.Password:
                        joinIpPassword.text = joinIpPassword.text.Remove(joinIpPassword.text.Length - 1);
                        break;
                }
            });


            gobj = CreateObject("Confirm");
            gobj.transform.SetParent(joinIpPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(150, 150);
            rect.localPosition = new Vector3(400, -240, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateKeyBackground(gobj);
            btn.colors = keyColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = ">";
            btnText.fontSize = 80;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = keyConfirmColor;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                StartCoroutine(IpJoin(joinIpAddress.text, joinIpPort.text, joinIpPassword.text));
            });


            gobj = CreateObject("Shift");
            gobj.transform.SetParent(joinIpPanel);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(225, 75);
            rect.localPosition = new Vector3(190, -320, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateKeyBackground(gobj);
            btn.colors = keyColor;

            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Shift";
            btnText.fontSize = 60;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = keyTextColor;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                if (isShifted) {
                    isShifted = false;
                    keyboardPanel.gameObject.SetActive(true);
                    shiftedKeyboardPanel.gameObject.SetActive(false);
                } else {
                    isShifted = true;
                    keyboardPanel.gameObject.SetActive(false);
                    shiftedKeyboardPanel.gameObject.SetActive(true);
                }
            });
            
            /*
            gobj = CreateObject("CurrentCode");
            gobj.transform.SetParent(joinIpPanel.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 100);
            rect.localPosition = new Vector3(0, 100, 0);
            img = gobj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);

            gobj = CreateObject("CurrentCodeText");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 150);
            rect.localPosition = new Vector3(0, 0, 0);
            joinCode = gobj.AddComponent<TextMeshProUGUI>();
            joinCode.color = Color.black;
            joinCode.alignment = TextAlignmentOptions.Center;
            joinCode.fontSize = 90;*/

            gobj = CreateObject("CurrentIp");
            gobj.transform.SetParent(joinIpPanel.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = new Vector3(-420, 140, 0);
            img = gobj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);
            joinIpAddressField = img;

            btn = gobj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                SelectJoinIpField(JoinIpSelection.Address);
            });

            gobj = CreateObject("CurrentIpText");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = new Vector3(0, 0, 0);
            joinIpAddress = gobj.AddComponent<TextMeshProUGUI>();
            joinIpAddress.enableAutoSizing = true;
            joinIpAddress.color = Color.black;
            joinIpAddress.alignment = TextAlignmentOptions.Center;
            joinIpAddress.text = ModManager.safeFile.inputCache.join_address;


            gobj = CreateObject("CurrentPort");
            gobj.transform.SetParent(joinIpPanel.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = new Vector3(-10, 140, 0);
            img = gobj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);
            joinIpPortField = img;

            btn = gobj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                SelectJoinIpField(JoinIpSelection.Port);
            });

            gobj = CreateObject("CurrentPortText");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = new Vector3(0, 0, 0);
            joinIpPort = gobj.AddComponent<TextMeshProUGUI>();
            joinIpPort.enableAutoSizing = true;
            joinIpPort.color = Color.black;
            joinIpPort.alignment = TextAlignmentOptions.Center;
            joinIpPort.text = ModManager.safeFile.inputCache.join_port.ToString();


            gobj = CreateObject("CurrentPassword");
            gobj.transform.SetParent(joinIpPanel.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = new Vector3(400, 140, 0);
            img = gobj.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);
            joinIpPasswordField = img;

            btn = gobj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                SelectJoinIpField(JoinIpSelection.Password);
            });

            gobj = CreateObject("CurrentPasswordText");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = new Vector3(0, 0, 0);
            joinIpPassword = gobj.AddComponent<TextMeshProUGUI>();
            joinIpPassword.enableAutoSizing = true;
            joinIpPassword.color = Color.black;
            joinIpPassword.alignment = TextAlignmentOptions.Center;
            joinIpPassword.text = ModManager.safeFile.inputCache.join_password;
            SelectJoinIpField(currentJoinIpSelection);
        }
        private class JoinCodeInfo {
            public string address = "";
            public short port = 0;
        }

        private void SelectJoinIpField(JoinIpSelection selection) {
            currentJoinIpSelection = selection;
            SetFieldSelected(joinIpAddressField,  selection == JoinIpSelection.Address);
            SetFieldSelected(joinIpPortField,     selection == JoinIpSelection.Port);
            SetFieldSelected(joinIpPasswordField, selection == JoinIpSelection.Password);
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

                GUIManager.JoinServer(info.address, info.port.ToString(), save_cache: false);
            } else {
                connectingMessage.text = "Getting Server Info failed! Is the code correct?";
                connectingMessage.color = Color.red;
                
                yield return new WaitForSeconds(5);
                
                ShowPage(Page.CodeJoining);
            }
        }

        private IEnumerator IpJoin(string address, string port, string password) {
            ShowPage(Page.Connecting);

            if (address != null && address.Length > 0 && port != null && port.Length > 0) {
                connectingMessage.text = "Connecting to server...";
                serverJoinCodeLabel.gameObject.SetActive(false);
                serverJoinCodeMessage.gameObject.SetActive(false);

                yield return new WaitForSeconds(1);

                GUIManager.JoinServer(address, port, password);

                UpdateConnectionScreen();
            }
            else {
                connectingMessage.text = "Cannot connect to server! Did you enter the correct information?";
                connectingMessage.color = Color.red;

                yield return new WaitForSeconds(5);

                ShowPage(Page.IpJoining);
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
    }
}
