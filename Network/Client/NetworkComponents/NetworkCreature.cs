using AMP.Data;
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
            this.creatureNetworkData = creatureNetworkData;
            
            RegisterEvents();
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

                ModManager.clientInstance.nw.SendReliable(creatureNetworkData.CreateHealthChangePacket(damage));
            };

            creatureNetworkData.clientsideCreature.OnHealEvent += (heal, healer) => {
                if(creatureNetworkData.networkedId <= 0) return;
                if(healer == null) return;

                ModManager.clientInstance.nw.SendReliable(creatureNetworkData.CreateHealthChangePacket(heal));
            };

            creatureNetworkData.clientsideCreature.OnKillEvent += (collisionInstance, eventTime) => {
                if(eventTime == EventTime.OnEnd) return;
                if(creatureNetworkData.networkedId <= 0) return;

                if(creatureNetworkData.health != -1) {
                    creatureNetworkData.health = -1;

                    ModManager.clientInstance.nw.SendReliable(creatureNetworkData.CreateHealthPacket());
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

            registeredEvents = true;
        }

    }
}
