using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        protected PlayerNetworkData playerNetworkData;

        internal void Init(PlayerNetworkData playerNetworkData) {
            if(this.playerNetworkData != playerNetworkData) registeredEvents = false;
            this.playerNetworkData = playerNetworkData;

            //Log.Warn("INIT Player");

            UpdateCreature();
            RegisterEvents();
        }

        internal override bool IsSending() {
            return false; //playerNetworkData.clientId == ModManager.clientInstance.myClientId;
        }

        protected new void OnAwake() {
            base.OnAwake();
        }

        protected override void ManagedUpdate() {
            base.ManagedUpdate();

            if(playerNetworkData != null && playerNetworkData.ragdollParts != null) {
                creature.SmoothDampRagdoll(playerNetworkData.ragdollParts);
                creature.locomotion.transform.up = Vector3.up;
                creature.ragdoll.standingUp = true;
            } else {
                if(handLeftTarget == null) return;

                // Rotations
                handLeftRot = handLeftRot.SmoothDamp(handLeftTargetRot, ref handLeftRotVelocity, MOVEMENT_DELTA_TIME);
                handRightRot = handRightRot.SmoothDamp(handRightTargetRot, ref handRightRotVelocity, MOVEMENT_DELTA_TIME);
                headRot = headRot.SmoothDamp(headTargetRot, ref headRotVelocity, MOVEMENT_DELTA_TIME);

                handLeftTarget.rotation = handLeftRot;
                handRightTarget.rotation = handRightRot;
                headTarget.rotation = headRot;

                // Positions
                handLeftPos = Vector3.SmoothDamp(handLeftPos, handLeftTargetPos, ref handLeftTargetVel, MOVEMENT_DELTA_TIME);
                handRightPos = Vector3.SmoothDamp(handRightPos, handRightTargetPos, ref handRightTargetVel, MOVEMENT_DELTA_TIME);
                headPos = Vector3.SmoothDamp(headPos, headTargetPos, ref headTargetVel, MOVEMENT_DELTA_TIME);
                
                handLeftTarget.position = transform.position + handLeftPos;
                handRightTarget.position = transform.position + handRightPos;
                headTarget.position = headPos;
                headTarget.Translate(Vector3.forward);
            }

            creature.lastInteractionTime = Time.time - 1;
            creature.spawnTime = Time.time - 1;
        }


        internal override void UpdateCreature() {
            base.UpdateCreature();

            //creature.animator.enabled = false;
            //creature.StopAnimation();
            //creature.animator.speed = 0f;
            ////creature.ragdoll.enabled = false;
            //
            //creature.locomotion.flyDrag = 100000;
            //creature.locomotion.groundDrag = 100000;

            //creature.ragdoll.SetState(Ragdoll.State.NoPhysic);
            //creature.fallState = Creature.FallState.StabilizedOnGround;
        }

        private bool registeredEvents = false;
        internal new void RegisterEvents() {
            if(registeredEvents) return;

            creature.OnDamageEvent += (collisionInstance) => {
                if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health

                float damage = creature.currentHealth - creature.maxHealth; // Should be negative
                playerNetworkData.health = creature.currentHealth;
                creature.currentHealth = creature.maxHealth;

                playerNetworkData.CreateHealthChangePacket(damage).SendToServerReliable(); ;
            };

            creature.OnHealEvent += (heal, healer) => {
                if(healer == null) return;
                if(!healer.player) return;

                playerNetworkData.CreateHealthChangePacket(heal).SendToServerReliable(); ;
            };

            creature.OnDespawnEvent += (eventTime) => {
                if(playerNetworkData.creature != creature) return;

                playerNetworkData.creature = null;

                if(Level.current != null && !Level.current.loaded) return; // If we are currently loading a level no need to try and spawn the player, it will automatically happen once we loaded the level

                ClientSync.SpawnPlayer(playerNetworkData.clientId);
                Log.Debug("[Client] Player despawned, trying to respawn!");
            };

            if(!IsSending())
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, true);
            
            registeredEvents = true;
        }
    }
}
