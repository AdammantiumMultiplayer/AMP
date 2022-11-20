using AMP.Network.Client.NetworkComponents;
using AMP.Network.Packets.Implementation;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class PlayerNetworkData {
        #region Values
        internal long clientId = 0;
        internal string name = "";

        internal string creatureId = "HumanMale";
        internal float height = 1.8f;

        internal Vector3 handLeftPos = Vector3.zero;
        internal Vector3 handLeftRot = Vector3.zero;

        internal Vector3 handRightPos = Vector3.zero;
        internal Vector3 handRightRot = Vector3.zero;

        internal Vector3 headPos = Vector3.zero;
        internal Vector3 headRot = Vector3.zero;

        internal Vector3 velocity = Vector3.zero;
        internal Vector3 position = Vector3.zero;
        internal float rotationY   = 0f;

        internal Vector3[] ragdollParts;

        internal float health = 1f;

        internal string[] equipment = new string[0];
        internal Color[] colors = new Color[6];

        // Client only stuff
        internal bool isSpawning = false;
        internal Creature creature;
        private NetworkPlayerCreature _networkCreature;
        internal NetworkPlayerCreature networkCreature {
            get {
                if(_networkCreature == null && creature != null) _networkCreature = creature.GetComponent<NetworkPlayerCreature>();
                return _networkCreature;
            }
        }
        #endregion

        #region Packet Generation and Reading
        internal void Apply(PlayerDataPacket p) {
            clientId   = p.playerId;
            name       = p.name;

            creatureId = p.creatureId;
            height     = p.height;

            position  = p.playerPos;
            rotationY = p.playerRotY;
        }

        internal void Apply(PlayerEquipmentPacket p) {
            colors    = p.colors;
            equipment = p.equipment;
        }

        internal void Apply(PlayerPositionPacket p) {
            handLeftPos  = p.handLeftPos;
            handLeftRot  = p.handLeftRot;

            handRightPos = p.handRightPos;
            handRightRot = p.handRightRot;

            headPos      = p.headPos;
            headRot      = p.headRot;

            position     = p.position;
            rotationY    = p.rotationY;
        }

        internal void PositionChanged() {
            if(creature != null) creature.lastInteractionTime = Time.time;
        }

        internal void Apply(PlayerRagdollPacket p, bool add_player_pos = false) {
            position = p.position;
            rotationY = p.rotationY;

            if(p.ragdollParts.Length == 0) {
                ragdollParts = null;
            } else {
                ragdollParts = p.ragdollParts;
                if(add_player_pos) {
                    for(byte i = 0; i < ragdollParts.Length; i++) {
                        if(i % 2 == 0) ragdollParts[i] += position; // Add offset only to positions, they are at the even indexes
                    }
                }
            }
        }

        internal bool Apply(PlayerHealthSetPacket p) {
            float newHealth = p.health;

            bool gotKilled = (health > 0 && newHealth <= 0);

            health = newHealth;

            return gotKilled;
        }
        #endregion
    }
}
