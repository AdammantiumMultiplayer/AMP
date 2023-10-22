using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using Netamite.Server.Data;
using UnityEngine;

namespace AMP.Network.Data {
    public class ClientData : ClientInformation {

        public static ClientData SERVER = new ClientData() {
              
        };

        internal bool greeted = false;

        internal PlayerNetworkData _player = null;

        public PlayerNetworkData player {
            get {
                if(_player == null) {
                    _player = new PlayerNetworkData() { clientId = ClientId };
                }

                return _player;
            }
            set { _player = value; }
        }

        public ClientData() { }

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

        #region Teleport
        public void Teleport(Vector3 position) {
            Teleport(position, player.rotationY);
        }

        public void Teleport(Vector3 position, float rotation) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new PlayerTeleportPacket(position, rotation));
        }
        #endregion

        #region Text Stuff
        public void ShowText(string id, string message, Color color, float displayTime = 10f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, Vector3.forward * 2, true, true, displayTime));
        }
        public void ShowText(string id, string message, float yOffset, Color color, float displayTime = 10f) {
            ModManager.serverInstance.netamiteServer.SendTo(this, new DisplayTextPacket(id, message, color, Vector3.forward * 2 + (Vector3.left * (yOffset / 3)), true, true, displayTime));
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
    }
}
