using AMP.Data;
using AMP.Extension;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Packets.Implementation;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.HandPoseData;

namespace AMP.Network.Data.Sync {
    public class PlayerNetworkData : NetworkData {
        #region Values
        internal int clientId = 0;
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
        internal float rotationYVel = 0f;
        internal Vector3 position = Vector3.zero;
        internal float rotationY   = 0f;

        internal Vector3[] ragdollPositions;
        internal Quaternion[] ragdollRotations;
        internal Vector3[] ragdollVelocity;
        internal Vector3[] ragdollAngularVelocity;

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

        internal bool receivedPos = false;
        #endregion

        #region Packet Generation and Reading
        internal void Apply(PlayerDataPacket p) {
            clientId   = p.clientId;
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

            receivedPos = true;
        }

        internal void PositionChanged() {
            if(creature != null) creature.lastInteractionTime = Time.time;
        }

        internal void Apply(PlayerRagdollPacket p) {
            position = p.position;
            rotationY = p.rotationY;

            if(  p.ragdollPositions.Length == 0 
              || p.ragdollRotations.Length == 0) {
                ragdollPositions       = null;
                ragdollRotations       = null;
                ragdollVelocity        = null;
                ragdollAngularVelocity = null;
            } else {
                ragdollPositions       = p.ragdollPositions;
                ragdollRotations       = p.ragdollRotations;
                ragdollVelocity        = p.velocities;
                ragdollAngularVelocity = p.angularVelocities;
            }

            receivedPos = true;
        }

        internal bool Apply(PlayerHealthSetPacket p) {
            float newHealth = p.health;

            bool gotKilled = (health > 0 && newHealth <= 0);

            health = newHealth;

            return gotKilled;
        }
        #endregion


        internal void UpdatePositionFromCreature() {
            position = Player.currentCreature.transform.position;
            rotationY = Player.local.head.transform.eulerAngles.y;
            velocity = Player.currentCreature.ragdoll.IsPhysicsEnabled() ? Player.currentCreature.ragdoll.rootPart.physicBody.velocity : Player.currentCreature.currentLocomotion.rb.velocity;
            rotationYVel = Player.currentCreature.ragdoll.IsPhysicsEnabled() ? Player.currentCreature.ragdoll.rootPart.physicBody.angularVelocity.y : Player.currentCreature.currentLocomotion.rb.angularVelocity.y;

            if(Config.PLAYER_FULL_BODY_SYNCING) {
                Player.currentCreature.ReadRagdoll(positions: out ragdollPositions
                                                  , rotations: out ragdollRotations
                                                  , velocity: out ragdollVelocity
                                                  , angularVelocity: out ragdollAngularVelocity
                                                  );
            } else {
                velocity = Player.local.locomotion.rb.velocity;

                handLeftPos = Player.currentCreature.ragdoll.ik.handLeftTarget.position - position;
                handLeftRot = Player.currentCreature.ragdoll.ik.handLeftTarget.eulerAngles;

                handRightPos = Player.currentCreature.ragdoll.ik.handRightTarget.position - position;
                handRightRot = Player.currentCreature.ragdoll.ik.handRightTarget.eulerAngles;

                headPos = Player.currentCreature.ragdoll.headPart.transform.position;
                headRot = Player.currentCreature.ragdoll.headPart.transform.eulerAngles;
            }

            RecalculateDataTimestamp();
        }
    }
}
