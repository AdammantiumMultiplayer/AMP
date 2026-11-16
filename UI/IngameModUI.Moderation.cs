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
    // Moderation page: player list of the server
    public partial class IngameModUI {

        RectTransform moderationPanel;
        ScrollRect moderationPanelPlayerList;

        private void CreateModerationPanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            TextMeshProUGUI btnText;

            gobj = CreateObject("ModerationPanel");
            gobj.transform.SetParent(transform);
            moderationPanel = gobj.AddComponent<RectTransform>();


            gobj = CreateObject("BackButton");
            gobj.transform.SetParent(moderationPanel);
            rect = gobj.AddComponent<RectTransform>();
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;
            btn.onClick.AddListener(() => {
                ShowPage(Page.Disconnect);
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform);
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Back";
            btnText.color = tabTextColor;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            rect.sizeDelta = new Vector2(200, 50);
            rect.localPosition = new Vector3(-500, 300, 0);


            moderationPanelPlayerList = CreateScrollRect("Players", false);
            moderationPanelPlayerList.transform.SetParent(transform);
            rect = moderationPanelPlayerList.gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(-80, -80);
            rect.localPosition = new Vector3(0, -35, 0);
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
                    banBtn.onClick.AddListener(() => {
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
    }
}
