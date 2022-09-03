using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data.Sync;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    public class NetworkCreature : NetworkPosition {

        protected Creature creature;
        protected CreatureNetworkData creatureNetworkData;

        public void Init(CreatureNetworkData creatureNetworkData) {
            if(this.creatureNetworkData != creatureNetworkData) registeredEvents = false;
            this.creatureNetworkData = creatureNetworkData;
            
            RegisterEvents();
        }

        protected new bool IsOwning() {
            return creatureNetworkData.clientsideId > 0;
        }

        void Awake () {
            OnAwake();
        }

        protected void OnAwake() {
            creature = GetComponent<Creature>();

            creature.locomotion.rb.drag = 0;
            creature.locomotion.rb.angularDrag = 0;
        }

        protected override ManagedLoops ManagedLoops => ManagedLoops.FixedUpdate | ManagedLoops.Update;

        protected override void ManagedFixedUpdate() {
            if(!creature.enabled) UpdateLocomotionAnimation();
        }

        protected override void ManagedUpdate() {
            base.ManagedUpdate();

            creature.locomotion.rb.velocity = currentVelocity;
            creature.locomotion.velocity = currentVelocity;
        }

        private void UpdateLocomotionAnimation() {
            if(creature.currentLocomotion.isGrounded && creature.currentLocomotion.horizontalSpeed + Mathf.Abs(creature.currentLocomotion.angularSpeed) > creature.stationaryVelocityThreshold) {
                Vector3 vector = creature.transform.InverseTransformDirection(creature.currentLocomotion.velocity);
                creature.animator.SetFloat(Creature.hashStrafe, vector.x * (1f / creature.transform.lossyScale.x), creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashTurn, creature.currentLocomotion.angularSpeed * (1f / creature.transform.lossyScale.y) * creature.turnAnimSpeed, creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashSpeed, vector.z * (1f / creature.transform.lossyScale.z), creature.animationDampTime, Time.fixedDeltaTime);
            } else {
                creature.animator.SetFloat(Creature.hashStrafe, 0f, creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashTurn, 0f, creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashSpeed, 0f, creature.animationDampTime, Time.fixedDeltaTime);
            }
        }

        protected override void ManagedOnDisable() {
            Destroy(this);
        }




        private bool registeredEvents = false;
        public void RegisterEvents() {
            if(registeredEvents) return;

            creatureNetworkData.clientsideCreature.OnDamageEvent += (collisionInstance) => {
                if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health
                if(creatureNetworkData.networkedId <= 0) return;

                float damage = creatureNetworkData.clientsideCreature.currentHealth - creatureNetworkData.health; // Should be negative
                //Log.Debug(collisionInstance.damageStruct.damage + " / " + damage);
                creatureNetworkData.health = creatureNetworkData.clientsideCreature.currentHealth;

                creatureNetworkData.CreateHealthChangePacket(damage).SendToServerReliable();
            };

            creatureNetworkData.clientsideCreature.OnHealEvent += (heal, healer) => {
                if(creatureNetworkData.networkedId <= 0) return;
                if(healer == null) return;

                creatureNetworkData.CreateHealthChangePacket(heal).SendToServerReliable();
            };

            creatureNetworkData.clientsideCreature.OnKillEvent += (collisionInstance, eventTime) => {
                if(eventTime == EventTime.OnEnd) return;
                if(creatureNetworkData.networkedId <= 0) return;

                if(creatureNetworkData.health != -1) {
                    creatureNetworkData.health = -1;

                    creatureNetworkData.CreateHealthPacket().SendToServerReliable();
                }
            };

            //creatureNetworkData.clientsideCreature.brain.OnAttackEvent  += (attackType, strong, target) => {
            //    // Log.Debug("OnAttackEvent " + attackType);
            //};
            //
            //creatureNetworkData.clientsideCreature.brain.OnStateChangeEvent += (state) => {
            //    // TODO: Sync creature brain state if necessary
            //};
            //
            //creatureNetworkData.clientsideCreature.ragdoll.OnSliceEvent += (ragdollPart, eventTime) => {
            //    // TODO: Sync the slicing - ragdollPart.type
            //};

            RegisterGrabEvents();

            registeredEvents = true;
        }

        protected void RegisterGrabEvents() {
            foreach(RagdollHand rh in creature.ragdoll.handlers) {
                rh.OnGrabEvent += RagdollHand_OnGrabEvent;
                rh.OnUnGrabEvent += RagdollHand_OnUnGrabEvent;
            }
            foreach(Holder holder in creature.holders) {
                holder.UnSnapped += Holder_UnSnapped;
                holder.Snapped += Holder_Snapped;
            }
        }

        private void Holder_Snapped(Item item) {
            if(!IsOwning()) return;

            NetworkItem networkItem = item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Log.Debug($"[Client] Event: Snapped item {networkItem.itemNetworkData.dataId} to {networkItem.itemNetworkData.creatureNetworkId} in slot {networkItem.itemNetworkData.drawSlot}.");

            if(!networkItem.IsOwning()) networkItem.itemNetworkData.TakeOwnershipPacket().SendToServerReliable();

            networkItem.itemNetworkData.UpdateFromHolder();
            networkItem.itemNetworkData.SnapItemPacket().SendToServerReliable();
        }

        private void Holder_UnSnapped(Item item) {
            if(!IsOwning()) return;

            NetworkItem networkItem = item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Log.Debug($"[Client] Event: Unsnapped item {networkItem.itemNetworkData.dataId} from {networkItem.itemNetworkData.creatureNetworkId}.");

            if(!networkItem.IsOwning()) networkItem.itemNetworkData.TakeOwnershipPacket().SendToServerReliable();

            networkItem.itemNetworkData.UpdateFromHolder();
            networkItem.itemNetworkData.UnSnapItemPacket().SendToServerReliable();
        }

        private void RagdollHand_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation, EventTime eventTime) {
            if(eventTime != EventTime.OnStart) return; // Needs to be at start because we still know the item
            if(!IsOwning()) return;

            NetworkItem networkItem = handle.item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Log.Debug($"[Client] Event: Grabbed item {networkItem.itemNetworkData.dataId} by {networkItem.itemNetworkData.creatureNetworkId} with hand {networkItem.itemNetworkData.holdingSide}.");

            if(!networkItem.IsOwning()) networkItem.itemNetworkData.TakeOwnershipPacket().SendToServerReliable();

            networkItem.itemNetworkData.UpdateFromHolder();
            networkItem.itemNetworkData.SnapItemPacket().SendToServerReliable();
        }

        private void RagdollHand_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime) {
            if(eventTime != EventTime.OnStart) return; // Needs to be at start because we still know the item
            if(!IsOwning()) return;

            NetworkItem networkItem = handle.item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Log.Debug($"[Client] Event: Ungrabbed item {networkItem.itemNetworkData.dataId} by {networkItem.itemNetworkData.creatureNetworkId} with hand {networkItem.itemNetworkData.holdingSide}.");

            if(!networkItem.IsOwning()) networkItem.itemNetworkData.TakeOwnershipPacket().SendToServerReliable();

            networkItem.itemNetworkData.UpdateFromHolder();
            networkItem.itemNetworkData.UnSnapItemPacket().SendToServerReliable();
        }
    }
}
