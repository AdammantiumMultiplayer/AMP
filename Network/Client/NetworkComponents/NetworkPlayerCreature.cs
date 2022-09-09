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
    public class NetworkPlayerCreature : NetworkCreature {

        public Transform handLeftTarget;
        public Transform handRightTarget;
        public Transform headTarget;

        private Vector3 handLeftPos;
        private Quaternion handLeftRot;
        public Quaternion handLeftTargetRot;
        public Vector3 handLeftRotVelocity;
        public Vector3 handLeftTargetPos;
        private Vector3 handLeftTargetVel;

        private Vector3 handRightPos;
        private Quaternion handRightRot;
        public Quaternion handRightTargetRot;
        public Vector3 handRightRotVelocity;
        public Vector3 handRightTargetPos;
        private Vector3 handRightTargetVel;

        private Vector3 headPos;
        private Quaternion headRot;
        public Quaternion headTargetRot;
        public Vector3 headRotVelocity;
        public Vector3 headTargetPos;
        private Vector3 headTargetVel;

        protected PlayerNetworkData playerNetworkData;

        public void Init(PlayerNetworkData playerNetworkData) {
            if(this.playerNetworkData != playerNetworkData) registeredEvents = false;
            this.playerNetworkData = playerNetworkData;

            //Log.Warn("INIT Player");

            RegisterEvents();
        }

        public override bool IsSending() {
            return playerNetworkData.clientId == ModManager.clientInstance.myClientId;
        }

        protected new void OnAwake() {
            base.OnAwake();


        }

        protected override void ManagedUpdate() {
            base.ManagedUpdate();

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

            creature.lastInteractionTime = Time.time - 1;
            creature.spawnTime = Time.time - 1;
        }



        private bool registeredEvents = false;
        public new void RegisterEvents() {
            if(registeredEvents) return;

            playerNetworkData.creature.OnDamageEvent += (collisionInstance) => {
                if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health

                float damage = creature.currentHealth - creature.maxHealth; // Should be negative
                playerNetworkData.health = creature.currentHealth;
                creature.currentHealth = creature.maxHealth;

                playerNetworkData.CreateHealthChangePacket(damage).SendToServerReliable(); ;
            };

            playerNetworkData.creature.OnHealEvent += (heal, healer) => {
                if(healer == null) return;
                if(!healer.player) return;

                playerNetworkData.CreateHealthChangePacket(heal).SendToServerReliable(); ;
            };

            playerNetworkData.creature.OnDespawnEvent += (eventTime) => {
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
