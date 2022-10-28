using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    internal class NetworkPlayerCreature : NetworkCreature {

        internal Transform handLeftTarget;
        internal Transform handRightTarget;
        internal Transform headTarget;

        private Vector3 handLeftPos;
        private Quaternion handLeftRot;
        internal Quaternion handLeftTargetRot;
        internal Vector3 handLeftRotVelocity;
        internal Vector3 handLeftTargetPos;
        private Vector3 handLeftTargetVel;

        private Vector3 handRightPos;
        private Quaternion handRightRot;
        internal Quaternion handRightTargetRot;
        internal Vector3 handRightRotVelocity;
        internal Vector3 handRightTargetPos;
        private Vector3 handRightTargetVel;

        private Vector3 headPos;
        private Quaternion headRot;
        internal Quaternion headTargetRot;
        internal Vector3 headRotVelocity;
        internal Vector3 headTargetPos;
        private Vector3 headTargetVel;

        internal float targetRotation = 0f;
        private float rotationVelocity = 0f;

        protected PlayerNetworkData playerNetworkData;

        private float health = 100f;
        public TextMesh healthBar;

        internal void Init(PlayerNetworkData playerNetworkData) {
            if(this.playerNetworkData != playerNetworkData) registeredEvents = false;
            this.playerNetworkData = playerNetworkData;

            targetPos = this.playerNetworkData.position;
            //Log.Warn("INIT Player");

            UpdateCreature();
            RegisterEvents();
        }

        internal override bool IsSending() {
            return playerNetworkData.isSpawning; //playerNetworkData.clientId == ModManager.clientInstance.myClientId;
        }

        protected new void OnAwake() {
            base.OnAwake();
        }

        void FixedUpdate() {

        }

        protected override void ManagedUpdate() {
            if(IsSending()) return;

            base.ManagedUpdate();

            //Log.Info("NetworkPlayerCreature");

            transform.eulerAngles = new Vector3(0, Mathf.SmoothDampAngle(transform.eulerAngles.y ,targetRotation, ref rotationVelocity, Config.MOVEMENT_DELTA_TIME), 0);

            if(health != playerNetworkData.health && GameConfig.showPlayerHealthBars) {
                if(healthBar != null) {
                    health = Mathf.Lerp(health, playerNetworkData.health, Time.deltaTime * 2);

                    healthBar.text = HealthBar.calculateHealthBar(health);
                }
            }

            if(ragdollParts == null) {
                if(handLeftTarget == null) return;

                // Rotations
                handLeftRot = handLeftRot.SmoothDamp(handLeftTargetRot, ref handLeftRotVelocity, Config.MOVEMENT_DELTA_TIME);
                handRightRot = handRightRot.SmoothDamp(handRightTargetRot, ref handRightRotVelocity, Config.MOVEMENT_DELTA_TIME);
                headRot = headRot.SmoothDamp(headTargetRot, ref headRotVelocity, Config.MOVEMENT_DELTA_TIME);

                handLeftTarget.rotation = handLeftRot;
                handRightTarget.rotation = handRightRot;
                headTarget.rotation = headRot;

                // Positions
                handLeftPos = Vector3.SmoothDamp(handLeftPos, handLeftTargetPos, ref handLeftTargetVel, Config.MOVEMENT_DELTA_TIME);
                handRightPos = Vector3.SmoothDamp(handRightPos, handRightTargetPos, ref handRightTargetVel, Config.MOVEMENT_DELTA_TIME);
                headPos = Vector3.SmoothDamp(headPos, headTargetPos, ref headTargetVel, Config.MOVEMENT_DELTA_TIME);
                
                handLeftTarget.position = transform.position + handLeftPos;
                handRightTarget.position = transform.position + handRightPos;
                headTarget.position = headPos;
                headTarget.Translate(Vector3.forward);
            }
        }


        internal override void UpdateCreature() {
            base.UpdateCreature();

            creature.animator.enabled = false;
            creature.StopAnimation();
            creature.animator.speed = 0f;
            creature.locomotion.enabled = false;

            creature.ragdoll.standingUp = true;

            //creature.ragdoll.SetState(Ragdoll.State.Standing);
            //creature.fallState = FallState.NearGround;

            //foreach(Ragdoll.Bone bone in creature.ragdoll.bones) {
            //    if(bone.animationJoint == null) continue;
            //    Log.Debug(bone.animationJoint);
            //    bone.SetPinPositionForce(0, 0, 0);
            //    bone.SetPinRotationForce(0, 0, 0);
            //    bone.animationJoint.gameObject.SetActive(false);
            //}
            //
            //foreach(RagdollPart part in creature.ragdoll.parts) {
            //    part.rb.drag = 1000000;
            //    part.rb.useGravity = false;
            //}
        }

        private bool registeredEvents = false;
        internal new void RegisterEvents() {
            if(registeredEvents) return;

            creature.OnDamageEvent += (collisionInstance) => {
                if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health

                float damage = creature.currentHealth - creature.maxHealth; // Should be negative
                playerNetworkData.health = creature.currentHealth;
                creature.currentHealth = creature.maxHealth;

                new PlayerHealthChangePacket(playerNetworkData.clientId, damage).SendToServerReliable();
            };

            creature.OnHealEvent += (heal, healer) => {
                if(healer == null) return;
                if(!healer.player) return;

                new PlayerHealthChangePacket(playerNetworkData.clientId, heal).SendToServerReliable(); ;
            };

            creature.OnDespawnEvent += (eventTime) => {
                if(playerNetworkData.creature != creature) return;

                playerNetworkData.creature = null;

                if(Level.current != null && !Level.current.loaded) return; // If we are currently loading a level no need to try and spawn the player, it will automatically happen once we loaded the level

                Log.Warn("[Client] Player despawned, trying to respawn!");
                ClientSync.SpawnPlayer(playerNetworkData.clientId);
            };

            if(!IsSending())
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, true);
            
            registeredEvents = true;
        }
    }
}
