using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.Threading;
using Netamite.Network.Packet;
using Netamite.Server.Data;
using UnityEngine;
using static AMP.Data.Banlist;

namespace AMP.Network.Data {
    public class ClientData : ClientInformation {

        public static ClientData SERVER = new ClientData() {
            ClientId = -1,
            ClientName = "<color=#2c91af>[SERVER]</color>",
            greeted = true
        };

        internal bool greeted = false;

        internal PlayerNetworkData _player = null;

        internal PermissionLevel _permissionLevel = PermissionLevel.NORMAL;
        public PermissionLevel permissionLevel {
            get {
                return _permissionLevel;
            }
            set {
                _permissionLevel = value;
                SendPacket(new ModerationPermissionLevelPacket(value));
            }
        }
        
        public PlayerNetworkData player {
            get {
                if(_player == null) {
                    _player = new PlayerNetworkData() { clientId = ClientId };
                }

                return _player;
            }
            set { _player = value; }
        }

        public bool LoadedLevel = false;

        public ClientData() { }

        public List<ClientData> votersForKick = new List<ClientData>();
        
        private float damageMultiplicator = ModManager.safeFile.hostingSettings.pvpDamageMultiplier;

        #region Damaging
        public void SetInvulnerable(bool invulnerable) {
            if(invulnerable) {
                damageMultiplicator = 0f;
            } else {
                damageMultiplicator = ModManager.safeFile.hostingSettings.pvpDamageMultiplier;
            }
        }

        public bool IsInvulnerable() {
            return damageMultiplicator <= 0;
        }

        public float GetDamageMultiplicator() {
            return damageMultiplicator;
        }

        public void SetDamageMultiplicator(float multiplicator) {
            damageMultiplicator = multiplicator;
        }
        #endregion

        #region Pushback
        private float pushbackMultiplicator = ModManager.safeFile.hostingSettings.pvpPushbackMultiplier;

        public float GetPlayerPushbackMultiplicator() {
            return pushbackMultiplicator;
        }
        
        public void SetPushbackMultiplicator(float multiplicator) {
            pushbackMultiplicator = multiplicator;
        }
        
        public Vector3 ApplyPushbackMultiplicator(Vector3 vec, float intensity = 1f) {
            float magnitude = vec.magnitude;
            
            if (magnitude > 0) {
                float factor = (float) (Math.Log(1 + magnitude * intensity) / (magnitude * intensity));
                vec *= factor;
            }
            
            vec *= GetPlayerPushbackMultiplicator();
            
            return vec;
        }
        #endregion

        #region Teleport
        public void Teleport(Vector3 position) {
            Teleport(position, player.rotationY);
        }

        public void Teleport(Vector3 position, float rotation) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new PlayerTeleportPacket(position, rotation));
        }
        #endregion

        #region Text Stuff
        public void ShowText(string id, string message, Color color, float displayTime = 10f, float fadeTime = 0.5f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, Vector3.forward * 2, true, true, displayTime).SetFadeTime(fadeTime));
        }
        public void ShowText(string id, string message, float yOffset, Color color, float displayTime = 10f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, Vector3.forward * 2 + (Vector3.left * (yOffset / 5)), true, true, displayTime));
        }

        public void ShowText(string id, string message, float yOffset, int textSize, Color color, float displayTime = 10f, float fadeTime = 0.5f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, Vector3.forward * 2 + (Vector3.left * (yOffset / 5)), true, true, displayTime).SetTextSize(textSize).SetFadeTime(fadeTime));
        }

        public void ShowText(string id, string message, float yOffset, float xOffset, int textSize, Color color, float displayTime = 10f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, Vector3.forward * 2 + (Vector3.left * (yOffset / 5)) + (Vector3.up * (xOffset / 5)), true, true, displayTime).SetTextSize(textSize));
        }


        public void ShowTextInWorld(string id, string message, Color color, Vector3 position, Vector3 rotation, float displayTime = 10f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, position, rotation, displayTime));
        }

        public void ShowTextInWorld(string id, string message, Color color, Vector3 position, bool lookAtPlayer, float displayTime = 10f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, position, lookAtPlayer, false, displayTime));
        }

        public void ClearText(string id) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, "", Color.white, Vector3.zero, false, false, 0));
        }
        #endregion

        #region Paper UI
        // Identifier of the open papers and the actions that are allowed on them
        internal ConcurrentDictionary<string, string[]> openPaperUIs = new ConcurrentDictionary<string, string[]>();

        /// <summary>
        /// Shows a floating paper with text and action buttons in front of the player.
        /// Button presses arrive at ServerEvents.onActionButtonPressed, the paper stays open until ClosePaperUI is called
        /// or the player walks away from it (ServerEvents.onPaperUIClosed).
        /// </summary>
        public void ShowPaperUI(PaperUIPacket packet) {
            if(packet == null || packet.identifier == null) return;

            openPaperUIs[packet.identifier] = (string[]) (packet.buttonActions ?? new string[0]).Clone();
            SendPacketReliable(packet);
        }

        public void ShowPaperUI(string id, string text, string[] actions, string[] labels) {
            ShowPaperUI(new PaperUIPacket(id, text, actions, labels));
        }

        public void ClosePaperUI(string id) {
            if(id == null) return;

            openPaperUIs.TryRemove(id, out _);
            SendPacketReliable(new PaperUIClosePacket(id));
        }

        public bool IsPaperUIOpen(string id) {
            return id != null && openPaperUIs.ContainsKey(id);
        }
        #endregion

        #region Nametag stuff
        public void SetOthersNametagVisibility(bool is_visible) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new NametagVisibilityPacket(is_visible));
        }

        public void SetOwnNametagVisibility(bool is_visible) {
            ModManager.serverInstance.netamiteServer.SendToAllExcept(new NametagVisibilityPacket(is_visible, ClientId), ClientId);
        }
        
        public void SetPlayerNametagVisibility(ClientData client, bool is_visible) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new NametagVisibilityPacket(is_visible, client.ClientId));
        }
        #endregion

        #region Send Packets
        public void SendPacket(NetPacket packet) {
            ModManager.serverInstance.netamiteServer.SendTo(this, packet);
        }
        public void SendPacketReliable(NetPacket packet) {
            ModManager.serverInstance.netamiteServer.SendReliableTo(this, packet);
        }
        public void SendPacketUnreliable(NetPacket packet) {
            ModManager.serverInstance.netamiteServer.SendUnreliableTo(this, packet);
        }
        #endregion

        #region Kick and Ban
        public void Kick(string reason) {
            Log.Err(Defines.SERVER, $"{ClientName} kicked: {reason}");
            ModManager.serverInstance.netamiteServer.ClientLeft(this, reason);
            ModManager.serverInstance.LeavePlayer(this, reason);
        }
        
        // Bans a player for the duration of this session
        public void TempBan(string reason) {
            BanEntry banEntry = new BanEntry();
            banEntry.id = player.uniqueId;
            banEntry.name = ClientName;
            banEntry.reason = reason;
            
            ModManager.tempBanlist.Add(banEntry);
            Kick(reason);
        }
        
        public void Ban(string reason) {
            ModManager.banlist.Ban(this, reason);
            Kick(reason);
        }

        internal bool IsBanned() {
            foreach (BanEntry entry in ModManager.tempBanlist) {
                if(entry.id == player.uniqueId) { return true; }
            }
            
            if (ModManager.safeFile.hostingSettings.useGlobalPlayerBanlist) {
                Thread thread = new Thread(IsGloballyBanned) {
                    Name = "CheckGlobalBan"
                };
                thread.Start();
            }
            
            return ModManager.banlist.IsBanned(this);
        }
        
        internal void IsGloballyBanned() {
            if (player.uniqueId == null || player.uniqueId.Length == 0) {
                Dispatcher.Enqueue(() => {
                    Log.Err(Defines.SERVER, $"Couldn't check global banlist for {ClientName}. There was no id supllied, not sure what to do.");
                });
                return;
            }
            
            try {
                WebRequest request = WebRequest.Create($"https://{ModManager.safeFile.hostingSettings.masterServerUrl}/api/bancheck.php?id={player.uniqueId}");
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                if (response.StatusCode == HttpStatusCode.OK) {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    
                    string responseFromServer = reader.ReadToEnd();
                    
                    if("[{\"ok\":true}]".Equals(responseFromServer)) {
                        Ban("You are banned from all public servers.");
                    }
                } else {
                    Dispatcher.Enqueue(() => {
                        Log.Err(Defines.SERVER, $"Couldn't check global banlist for {ClientName} ({ClientId}), this should not happen. Please check server status. Player will be able to join.");
                    });
                }
            }catch(WebException exp) {
                Dispatcher.Enqueue(() => {
                    Log.Err(Defines.SERVER, $"Couldn't check global banlist for {ClientName} ({ClientId}), this should not happen. Please check server status. Player will be able to join.");
                    Log.Err(Defines.SERVER, exp);
                });
            }
        }
        #endregion
    }
}
