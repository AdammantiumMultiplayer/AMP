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

        private float health = 1f;
        public TextMesh healthBar;
        private float healthBarVel = 0f;

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

            slowmo = playerNetworkData.health == 0;

            base.ManagedUpdate();

            //Log.Info("NetworkPlayerCreature");

            transform.eulerAngles = new Vector3(0, Mathf.SmoothDampAngle(transform.eulerAngles.y ,targetRotation, ref rotationVelocity, Config.MOVEMENT_DELTA_TIME), 0);

            if(ModManager.safeFile.modSettings.showPlayerHealthBars && health != playerNetworkData.health) {
                if(healthBar != null) {
                    health = Mathf.SmoothDamp(health, playerNetworkData.health, ref healthBarVel, 0.2f);

                    healthBar.text = HealthBar.calculateHealthBar(health);
                }
            }

            if(ragdollPositions == null) {
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

            if(creature == null) return;

            creature.animator.enabled = false;
            creature.StopAnimation();
            creature.animator.speed = 0f;
            creature.locomotion.enabled = false;

            creature.ragdoll.standingUp = true;


            creature.ragdoll.SetState(Ragdoll.State.Standing);

            foreach(RagdollPart part5 in creature.ragdoll.parts) {
                if((bool)part5.bone.fixedJoint) {
                    UnityEngine.Object.Destroy(part5.bone.fixedJoint);
                }

                //part5.collisionHandler.RemovePhysicModifier(this);
                part5.bone.SetPinPositionForce(0f, 0f, 0f);
                part5.bone.SetPinRotationForce(0f, 0f, 0f);
            }

            creature.locomotion.enabled = false;
            creature.SetAnimatorHeightRatio(0f);

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
            if(creature == null) return;

            creature.OnDamageEvent += (collisionInstance, eventTime) => {
                //if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health
                if(collisionInstance.IsDoneByCreature(creature)) return; // If the damage is done by the creature itself, ignore it

                float damage = creature.currentHealth - creature.maxHealth; // Should be negative
                if(damage >= 0) return;
                creature.currentHealth = creature.maxHealth;

                new PlayerHealthChangePacket(playerNetworkData.clientId, damage).SendToServerReliable();

                Log.Debug(Defines.CLIENT, $"Damaged {playerNetworkData.name} with {damage} damage.");
                };

            creature.OnHealEvent += (heal, healer, eventTime) => {
                if(healer == null) return;
                if(!healer.isPlayer) return;

                new PlayerHealthChangePacket(playerNetworkData.clientId, heal).SendToServerReliable();
                Log.Debug(Defines.CLIENT, $"Healed {playerNetworkData.name} with {heal} heal.");
            };

            if(playerNetworkData.clientId != ModManager.clientInstance.myPlayerId) // Only because of DEBUG_SELF
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, Datatypes.ItemHolderType.PLAYER);
            
            registeredEvents = true;
        }
    }
}
