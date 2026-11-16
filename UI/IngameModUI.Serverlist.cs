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
    // Serverlist page: list of the master server and the info panel of the selected server
    public partial class IngameModUI {

        ScrollRect serverlist;
        RectTransform serverInfo;
        private static ServerInfo currentInfo = null;
        private static Button currentButton = null;
        private Button serverJoinButton = null; // Connect button of the selected server, disabled while connecting

        private void CreateServerlistPanel() {
            RectTransform rect;
            GameObject gobj;

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
            serverJoinButton = btn;
            obj.transform.SetParent(serverInfo);
            btn.targetGraphic = CreateTabBackground(obj);
            btn.colors = tabColor;
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
            rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 80);

            GameObject text = CreateObject("Text");
            text.transform.SetParent(obj.transform);
            TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Connect";
            btnText.color = tabTextColor;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => {
                btn.interactable = false; // Only one connection attempt at a time, the button comes back with the server list
                DoConnect();
            });

            FixSize();
        }
    }
}
