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

#if STEAM
namespace AMP.UI {
    // Steam: hosting through Steam, invites, friend list and the invite popup
    public partial class IngameModUI {

#if STEAM
        RectTransform steamHost;
        RectTransform steamInvites;
        ScrollRect friendsPanel;
        RectTransform friendInvitePanel;
        private Image inviteFriendImage;
        private TextMeshProUGUI inviteFriendName;
        private Button inviteFriendButton;
        private TextMeshProUGUI inviteFriendButtonText;
        private Coroutine inviteFriendClosing;
#endif
        bool steamHostPvp = true;
        #if STEAM        
        private static FriendInfo currentFriend = null;
        #endif

        private void CreateSteamHostPanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            TextMeshProUGUI btnText;
            VerticalLayoutGroup vlg;

            // Hosting through Steam, laid out like the master server hosting (rows with a label on the left)
            gobj = CreateObject("SteamHost");
            gobj.transform.SetParent(transform);
            steamHost = gobj.AddComponent<RectTransform>();

            CreateRowLabel(steamHost, "PvP:", 120);
            CreateEnabledToggle(steamHost, "PvP", new Vector3(120, 120, 0), steamHostPvp, (value) => steamHostPvp = value);

            gobj = CreateObject("HostSteamButton");
            gobj.transform.SetParent(steamHost.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600, 150);
            rect.localPosition = new Vector3(0, -150, 0);
            btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateTabBackground(gobj);
            btn.colors = tabColor;
            btn.onClick.AddListener(() => {
#if AMP
                serverJoinCodeMessage.text = "";
                // The server checks this setting when players damage each other (PlayerHealthChangePacket)
                ModManager.safeFile.hostingSettings.pvpEnable = steamHostPvp;
                GUIManager.HostSteam(10);
#endif
                UpdateConnectionScreen();
            });
            text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            rect = text.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = "Host Steam >";
            btnText.fontSize = 70;
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = keyConfirmColor;
            btnText.alignment = TextAlignmentOptions.Center;


            // Open invites, shown in the join menu
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
            steamInvites.sizeDelta = new Vector2(-640, -130);
            steamInvites.localPosition = new Vector3(0, -90, 0); // Below the join tab bar
        }

        private void CreateSteamFriendsPanel() {
            RectTransform rect;
            GameObject gobj;
            Button btn;
            GameObject text;
            Image img;

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
            rect.sizeDelta = new Vector2(-40, -350);
            rect.localPosition = new Vector3(0, 100, 0);


            // Invite popup, opens when clicking a friend. Covers the whole UI with a dark backdrop, clicking it closes the popup.
            gobj = CreateObject("InviteFriend");
            gobj.transform.SetParent(transform);
            friendInvitePanel = gobj.AddComponent<RectTransform>();
            friendInvitePanel.anchorMin = Vector2.zero;
            friendInvitePanel.anchorMax = Vector2.one;
            friendInvitePanel.sizeDelta = Vector2.zero;
            friendInvitePanel.localPosition = new Vector3(0, 0, -5);
            img = gobj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.5f);
            btn = gobj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(CloseInvitePopup);

            gobj = CreateObject("Dialog");
            gobj.transform.SetParent(friendInvitePanel);
            RectTransform dialog = gobj.AddComponent<RectTransform>();
            dialog.sizeDelta = new Vector2(500, 520);
            dialog.localPosition = Vector3.zero;
            img = gobj.AddComponent<Image>();
            img.color = new Color(0.95f, 0.88f, 0.72f, 1f); // Parchment
            UnityEngine.UI.Outline dialogOutline = gobj.AddComponent<UnityEngine.UI.Outline>();
            dialogOutline.effectColor = tabTextColor;
            dialogOutline.effectDistance = new Vector2(5, 5);
            gobj.AddComponent<Button>().transition = Selectable.Transition.None; // Catches clicks, so they don't close the popup

            CreateKey(dialog, "Close", "X", new Vector2(60, 60), new Vector3(205, 225, 0), keyCancelColor, CloseInvitePopup);

            gobj = CreateObject("ProfileImage");
            gobj.transform.SetParent(dialog);
            rect = gobj.AddComponent<RectTransform>();
            inviteFriendImage = gobj.AddComponent<Image>();
            rect.sizeDelta = new Vector2(200, 200);
            rect.localPosition = new Vector3(0, 95, 0);
            rect.localScale = new Vector3(1, -1, 1); // Steam images are upside down

            gobj = CreateObject("ProfileName");
            gobj.transform.SetParent(dialog);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(440, 60);
            rect.localPosition = new Vector3(0, -50, 0);
            inviteFriendName = gobj.AddComponent<TextMeshProUGUI>();
            inviteFriendName.alignment = TextAlignmentOptions.Center;
            inviteFriendName.enableAutoSizing = true;
            inviteFriendName.fontSizeMax = 45;
            inviteFriendName.fontStyle = FontStyles.Bold;
            inviteFriendName.color = Color.black;
            inviteFriendName.text = "";

            gobj = CreateObject("InviteProfileButton");
            gobj.transform.SetParent(dialog);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320, 90);
            rect.localPosition = new Vector3(0, -170, 0);
            inviteFriendButton = gobj.AddComponent<Button>();
            inviteFriendButton.targetGraphic = CreateTabBackground(gobj);
            inviteFriendButton.colors = tabColor;
            inviteFriendButton.onClick.AddListener(() => {
                if(currentFriend == null || inviteFriendClosing != null) return;

                Invite(currentFriend);

                inviteFriendButton.colors = invitedColor;
                inviteFriendButtonText.text = "Invited!";
                inviteFriendButtonText.color = Color.white;
                inviteFriendClosing = StartCoroutine(CloseInvitePopupDelayed(3f));
            });

            text = CreateObject("Text");
            text.transform.SetParent(inviteFriendButton.transform, false);
            rect = text.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            inviteFriendButtonText = text.AddComponent<TextMeshProUGUI>();
            inviteFriendButtonText.fontSize = 45;
            inviteFriendButtonText.fontStyle = FontStyles.Bold;
            inviteFriendButtonText.alignment = TextAlignmentOptions.Center;

            friendInvitePanel.gameObject.SetActive(false);
        }

        #if STEAM
        private Dictionary<ulong, GameObject> steamFriendButtons = new Dictionary<ulong, GameObject>();
        #endif

        #if STEAM
        private void UpdateSteamFriendList() {
            if (ModManager.serverInstance != null && ModManager.serverInstance.netamiteServer is SteamServer) {
                //foreach (Transform t in friendsPanel.content) Destroy(t.gameObject);

                friendsPanel.gameObject.SetActive(true);

                foreach (FriendInfo friendInfo in currentlyPlaying) {
                    if (steamFriendButtons.ContainsKey(friendInfo.steamId)) {
                        TextMeshProUGUI text = steamFriendButtons[friendInfo.steamId].GetComponentInChildren<TextMeshProUGUI>();
                        switch (friendInfo.status) {
                            case FriendInfo.Status.Offline:
                                text.color = Color.black;
                                break;
                            case FriendInfo.Status.Online:
                            case FriendInfo.Status.OtherGame:
                                text.color = Color.blue;
                                break;
                            case FriendInfo.Status.SameGame:
                                text.color = Color.green;
                                break;
                            default: break;
                        }
                    } else {
                        GameObject obj = friendInfo.GetPrefab();
                        obj.transform.SetParent(friendsPanel.content, false);
                        steamFriendButtons.Add(friendInfo.steamId, obj);
                    }
                }
                if (friendInvitePanel.gameObject.activeSelf) friendInvitePanel.SetAsLastSibling(); // Stay above the rebuilt list
            } else {
                CloseInvitePopup();
            }
        }
        #endif

#if STEAM
        internal IEnumerator LoadInvites() {
            if (!SteamIntegration.IsInitialized) yield break;

            foreach (Transform t in steamInvites) {
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
                btn.targetGraphic = CreateTabBackground(gobj);
                btn.colors = tabColor;
                btn.onClick.AddListener(() => {
                    currentFriend = this;
                    currentUI?.OpenInvitePopup();
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
                tmp.alignment = TextAlignmentOptions.Center;
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
                // No visible background for now (the list gets rebuilt and the tab background flashed white), the image is only there to be clickable
                btn.targetGraphic = gobj.AddComponent<Image>();
                btn.targetGraphic.color = new Color(0, 0, 0, 0);
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
#if STEAM
        // Green while invited, also when disabled
        static ColorBlock invitedColor = new ColorBlock() {
            normalColor      = new Color(0.3f, 0.75f, 0.25f, 1f),
            highlightedColor = new Color(0.3f, 0.75f, 0.25f, 1f),
            pressedColor     = new Color(0.3f, 0.75f, 0.25f, 1f),
            selectedColor    = new Color(0.3f, 0.75f, 0.25f, 1f),
            disabledColor    = new Color(0.3f, 0.75f, 0.25f, 1f),
            colorMultiplier  = 1,
            fadeDuration     = 0.15f
        };

        private void OpenInvitePopup() {
            if(currentFriend == null) return;
            ResetInvitePopup();

            UpdateFriendInfo();
            friendInvitePanel.gameObject.SetActive(true);
            friendInvitePanel.SetAsLastSibling(); // Above everything else
            FixSize(friendInvitePanel);
        }

        private void CloseInvitePopup() {
            if(friendInvitePanel == null) return;
            ResetInvitePopup();
            friendInvitePanel.gameObject.SetActive(false);
        }

        private void ResetInvitePopup() {
            if(inviteFriendClosing != null) {
                StopCoroutine(inviteFriendClosing);
                inviteFriendClosing = null;
            }
            inviteFriendButton.colors = tabColor;
            inviteFriendButtonText.text = "Invite";
            inviteFriendButtonText.color = tabTextColor;
        }

        private IEnumerator CloseInvitePopupDelayed(float delay) {
            yield return new WaitForSecondsRealtime(delay);
            inviteFriendClosing = null;
            CloseInvitePopup();
        }

        private static Coroutine friendAvatarLoading;
        private static void UpdateFriendInfo() {
            if(currentUI == null) return;

            currentUI.inviteFriendName.text = currentFriend.steamName;

            if(friendAvatarLoading != null) {
                currentUI.StopCoroutine(friendAvatarLoading);
                friendAvatarLoading = null;
            }

            // -1 means Steam is still downloading the avatar (the first call starts the download), so keep trying until it's there
            int handle = SteamFriends.GetLargeFriendAvatar((CSteamID) currentFriend.steamId);
            if(handle == -1) {
                currentUI.inviteFriendImage.gameObject.SetActive(false);
                friendAvatarLoading = currentUI.StartCoroutine(WaitForFriendAvatar(currentFriend));
            } else {
                ShowFriendAvatar(handle);
            }
        }

        private static IEnumerator WaitForFriendAvatar(FriendInfo friend) {
            float timeout = Time.unscaledTime + 5f;
            while(Time.unscaledTime < timeout) {
                yield return new WaitForSecondsRealtime(0.1f);
                if(currentFriend != friend) yield break; // Another friend got selected in the meantime

                int handle = SteamFriends.GetLargeFriendAvatar((CSteamID) friend.steamId);
                if(handle != -1) {
                    ShowFriendAvatar(handle);
                    break;
                }
            }
            friendAvatarLoading = null;
        }

        private static void ShowFriendAvatar(int handle) {
            Texture2D img = GetSteamImageAsTexture2D(handle);

            if(img != null) {
                currentUI.inviteFriendImage.sprite = Sprite.Create(img, new Rect(0, 0, img.width, img.height), Vector2.zero);
                currentUI.inviteFriendImage.gameObject.SetActive(true);
            } else {
                currentUI.inviteFriendImage.gameObject.SetActive(false);
            }
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
                    // Steam delivers sRGB colors, so the texture must not be linear, otherwise the game (linear color space) shows it washed out
                    ret = new Texture2D((int)ImageWidth, (int)ImageHeight, TextureFormat.RGBA32, false, false);
                    ret.LoadRawTextureData(Image);
                    ret.Apply();
                }
            }

            return ret;
        }
#endif
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
    }
}
#endif
