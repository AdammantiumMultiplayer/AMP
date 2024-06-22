using AMP.Extension;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Packets.Implementation;
using AMP.Threading;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class CreatureNetworkData : NetworkData {
        #region Values
        public int networkedId = 0;

        public string creatureType;
        public string containerID;
        public byte factionId;

        internal Vector3 velocity = Vector3.zero;
        internal float rotationYVel = 0f;
        public Vector3 position;
        internal float rotationY;

        internal Vector3[] ragdollPositions = null;
        internal Quaternion[] ragdollRotations = null;
        internal Vector3[] ragdollVelocity;
        internal Vector3[] ragdollAngularVelocity;

        internal bool loaded = false;

        internal bool isSpawning = false;
        public int clientsideId = 0;
        internal Creature creature;
        private NetworkCreature _networkCreature;
        internal NetworkCreature networkCreature {
            get {
                if(_networkCreature == null && creature != null) _networkCreature = creature.GetComponent<NetworkCreature>();
                return _networkCreature;
            }
        }

        internal int clientTarget = 0;

        internal float maxHealth = 100;
        public float health = 100;

        internal float height = 2f;

        internal string[] equipment = new string[0];
        internal Color[] colors = new Color[0];

        internal ClientData lastDamager;

        internal long lastRagdollTimestamp = 0;
        #endregion

        #region Packet Parsing
        internal void Apply(CreatureSpawnPacket p) {
            networkedId  = p.creatureId;
            clientsideId = p.clientsideId;
            creatureType = p.type;
            containerID  = p.container;
            factionId    = p.factionId;
            position     = p.position;
            rotationY    = p.rotationY;
            health       = p.health;
            maxHealth    = p.maxHealth;
            height       = p.height;

            equipment    = p.equipment;
            colors       = p.colors;
        }

        internal void Apply(CreaturePositionPacket p) {
            if(isSpawning) return;
            if(ragdollPositions != null) {
                ragdollPositions = null;
                ragdollRotations = null;
                if(networkCreature != null) networkCreature?.SetRagdollInfo(null, null);
            }
            position  = p.position;
            rotationY = p.rotationY;
        }

        internal void ApplyPositionToCreature() {
            if(creature == null) return;
            if(networkCreature == null) this.StartNetworking();
            if(networkCreature.IsSending()) return;

            if(ragdollPositions != null && ragdollRotations != null) networkCreature.SetRagdollInfo(ragdollPositions, ragdollRotations);

            networkCreature.targetPos = position;
            creature.transform.eulerAngles = new Vector3(0, rotationY, 0);

            PositionChanged();
        }

        internal void Apply(CreatureRagdollPacket p) {
            position = p.position;

            if(  p.ragdollPositions.Length == 0
              || p.ragdollRotations.Length == 0) {
                ragdollPositions = null;
                ragdollRotations = null;
            } else {
                ragdollPositions = p.ragdollPositions;
                ragdollRotations = p.ragdollRotations;
            }
        }

        internal bool Apply(CreatureHealthSetPacket p) {
            return SetHealth(p.health);
        }

        internal bool Apply(CreatureHealthChangePacket p) {
            return SetHealth(health + p.change);
        }

        private bool SetHealth(float newHealth) {
            bool gotKilled = (health > 0 && newHealth <= 0);

            health = newHealth;

            return gotKilled;
        }

        internal void ApplyHealthToCreature() {
            if(creature != null) {
                creature.currentHealth = health;

                //Log.Debug($"Creature {clientsideCreature.creatureId} is now at health {health}.");

                if(health <= 0 && !isSpawning) {
                    creature.Kill();
                }
            }
        }

        internal void UpdatePositionFromCreature() {
            if(creature == null) return;

            if(creature.IsRagdolled()) {
                creature.ReadRagdoll(out ragdollPositions, out ragdollRotations, out ragdollVelocity, out ragdollAngularVelocity);
            } else {
                ragdollPositions       = null;
                ragdollRotations       = null;
                ragdollVelocity        = null;
                ragdollAngularVelocity = null;
            }
            position = creature.transform.position;
            rotationY = creature.transform.eulerAngles.y;

            velocity = creature.ragdoll.IsPhysicsEnabled() ? creature.ragdoll.rootPart.physicBody.velocity : creature.currentLocomotion.physicBody.velocity;
            rotationYVel = creature.ragdoll.IsPhysicsEnabled() ? creature.ragdoll.rootPart.physicBody.angularVelocity.y : creature.currentLocomotion.physicBody.angularVelocity.y;

            RecalculateDataTimestamp();
        }

        internal void PositionChanged() {
            Dispatcher.Enqueue(() => {
                if(creature != null) creature.lastInteractionTime = Time.time;
            });
        }
        #endregion

        #region Ownership stuff
        internal void RequestOwnership() {
            if(networkedId <= 0) return;

            if(clientsideId <= 0){
                clientsideId = ModManager.clientSync.syncData.currentClientCreatureId++;
                new CreatureOwnerPacket(networkedId, true).SendToServerReliable();
            }
        }

        internal void SetOwnership(bool owner) {
            if(owner) {
                if(clientsideId <= 0) clientsideId = ModManager.clientSync.syncData.currentClientCreatureId++;
            } else {
                clientsideId = 0;
            }

            if(isSpawning) return;

            networkCreature?.UpdateCreature();
        }
        #endregion
    }
}
