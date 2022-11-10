using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.Threading;
using System;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    internal class NetworkCreature : NetworkPosition {

        internal Creature creature;
        protected CreatureNetworkData creatureNetworkData;

        protected Vector3[] ragdollParts = null;
        private Vector3[] ragdollPartsVelocity = null;

        internal void Init(CreatureNetworkData creatureNetworkData) {
            if(this.creatureNetworkData != creatureNetworkData) registeredEvents = false;
            this.creatureNetworkData = creatureNetworkData;

            targetPos = this.creatureNetworkData.position;
            //Log.Warn("INIT Creature");

            UpdateCreature();
            RegisterEvents();
        }

        internal override bool IsSending() {
            return creatureNetworkData != null && creatureNetworkData.clientsideId > 0;
        }

        void Awake () {
            OnAwake();
        }

        protected void OnAwake() {
            creature = GetComponent<Creature>();

            //creature.locomotion.rb.drag = 0;
            //creature.locomotion.rb.angularDrag = 0;
        }

        protected override ManagedLoops ManagedLoops => ManagedLoops.FixedUpdate | ManagedLoops.Update;

        protected override void ManagedFixedUpdate() {
            //if(!IsSending()) {
            //    CheckForMagic();
            //} else {
            //
            //}
        }

        protected override void ManagedUpdate() {
            if(IsSending()) return;

            if(creature.lastInteractionTime < Time.time - Config.NET_COMP_DISABLE_DELAY) return;

            //if(creatureNetworkData != null) Log.Info("NetworkCreature");

            base.ManagedUpdate();

            if(ragdollParts != null) {
                creature.SmoothDampRagdoll(ragdollParts, ref ragdollPartsVelocity, transform.position);
            }

            creature.locomotion.rb.velocity = positionVelocity;
            creature.locomotion.velocity = positionVelocity;
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

        public void SetRagdollInfo(Vector3[] ragdollParts) {
            this.ragdollParts = ragdollParts;

            if(ragdollParts != null) {
                if(ragdollPartsVelocity == null || ragdollPartsVelocity.Length != ragdollParts.Length) { // We only want to set the velocity if ragdoll parts are synced
                    ragdollPartsVelocity = new Vector3[ragdollParts.Length];
                    UpdateCreature();
                }
            } else if(ragdollPartsVelocity != null) {
                ragdollPartsVelocity = null;
                UpdateCreature();
            }
        }

        #region Register Events
        private bool registeredEvents = false;
        internal void RegisterEvents() {
            if(registeredEvents) return;
            if(creature == null) return;

            creature.OnDamageEvent += Creature_OnDamageEvent;
            creature.OnHealEvent += Creature_OnHealEvent;
            creature.OnKillEvent += Creature_OnKillEvent;
            creature.OnDespawnEvent += Creature_OnDespawnEvent;

            creature.ragdoll.OnSliceEvent += Ragdoll_OnSliceEvent;
            creature.ragdoll.OnTelekinesisGrabEvent += Ragdoll_OnTelekinesisGrabEvent;

            RegisterGrabEvents();

            if(!IsSending()) {
                Dispatcher.Enqueue(() => {
                    ClientSync.EquipItemsForCreature(creatureNetworkData.networkedId, false);
                });
            } else if(creatureNetworkData.networkedId > 0) {
                foreach(ItemNetworkData ind in ModManager.clientSync.syncData.items.Values) {
                    if(ind.creatureNetworkId == creatureNetworkData.networkedId && ind.holderIsPlayer == false) {
                        ind.networkItem.OnHoldStateChanged();
                    }
                }
            }

            registeredEvents = true;
        }

        protected void RegisterGrabEvents() {
            foreach(RagdollHand rh in new RagdollHand[] { creature.handLeft, creature.handRight }) {
                rh.OnGrabEvent += RagdollHand_OnGrabEvent;
                rh.OnUnGrabEvent += RagdollHand_OnUnGrabEvent;
            }
            foreach(Holder holder in creature.holders) {
                holder.UnSnapped += Holder_UnSnapped;
                holder.Snapped += Holder_Snapped;
            }
        }
        #endregion

        #region Unregister Events
        protected override void ManagedOnDisable() {
            Destroy(this);
            UnregisterEvents();
        }

        protected void UnregisterEvents() {
            if(!registeredEvents) return;

            creature.OnDamageEvent -= Creature_OnDamageEvent;
            creature.OnHealEvent -= Creature_OnHealEvent;
            creature.OnKillEvent -= Creature_OnKillEvent;
            creature.OnDespawnEvent -= Creature_OnDespawnEvent;

            creature.ragdoll.OnSliceEvent -= Ragdoll_OnSliceEvent;

            UnregisterGrabEvents();

            registeredEvents = false;
        }

        protected void UnregisterGrabEvents() {
            foreach(RagdollHand rh in new RagdollHand[] { creature.handLeft, creature.handRight }) {
                rh.OnGrabEvent -= RagdollHand_OnGrabEvent;
                rh.OnUnGrabEvent -= RagdollHand_OnUnGrabEvent;
            }
            foreach(Holder holder in creature.holders) {
                holder.UnSnapped -= Holder_UnSnapped;
                holder.Snapped -= Holder_Snapped;
            }
        }
        #endregion

        #region Ragdoll Events
        private void Ragdoll_OnSliceEvent(RagdollPart ragdollPart, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            if(!IsSending()) return; //creatureNetworkData.TakeOwnershipPacket().SendToServerReliable();

            Log.Debug(Defines.CLIENT, $"Event: Creature {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId}) lost {ragdollPart.type}.");

            new CreatureSlicePacket(creatureNetworkData.networkedId, ragdollPart.type).SendToServerReliable();
        }

        private void Ragdoll_OnTelekinesisGrabEvent(SpellTelekinesis spellTelekinesis, HandleRagdoll handleRagdoll) {
            if(!IsSending()) {
                new CreatureOwnerPacket(creatureNetworkData.networkedId, true).SendToServerReliable();
            }
        }
        #endregion

        #region Creature Events
        private void Creature_OnDespawnEvent(EventTime eventTime) {
            if(eventTime == EventTime.OnEnd) return;
            if(creatureNetworkData.networkedId <= 0) return;
            if(IsSending()) {
                Log.Debug(Defines.CLIENT, $"Event: Creature {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId}) is despawned.");

                new CreatureDepawnPacket(creatureNetworkData).SendToServerReliable();

                ModManager.clientSync.syncData.creatures.Remove(creatureNetworkData.networkedId);

                creatureNetworkData.networkedId = 0;

                Destroy(this);
            }
        }

        private void Creature_OnKillEvent(CollisionInstance collisionInstance, EventTime eventTime) {
            if(eventTime == EventTime.OnEnd) return;
            if(creatureNetworkData.networkedId <= 0) return;

            if(creatureNetworkData.health != -1) {
                creatureNetworkData.health = -1;

                if(!IsSending()) new CreatureOwnerPacket(creatureNetworkData.networkedId, true).SendToServerReliable();
                new CreatureHealthSetPacket(creatureNetworkData).SendToServerReliable();
            }
        }

        private void Creature_OnHealEvent(float heal, Creature healer) {
            if(creatureNetworkData.networkedId <= 0) return;
            if(healer == null) return;

            new CreatureHealthChangePacket(creatureNetworkData.networkedId, heal).SendToServerReliable();
        }

        private void Creature_OnDamageEvent(CollisionInstance collisionInstance) {
            if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health
            if(creatureNetworkData.networkedId <= 0) return;

            float damage = creatureNetworkData.creature.currentHealth - creatureNetworkData.health; // Should be negative
            //Log.Debug(collisionInstance.damageStruct.damage + " / " + damage);
            creatureNetworkData.health = creatureNetworkData.creature.currentHealth;

            new CreatureHealthChangePacket(creatureNetworkData.networkedId, damage).SendToServerReliable();
        }
        #endregion

        #region Holder Events
        private void Holder_Snapped(Item item) {
            if(!IsSending()) return;

            NetworkItem networkItem = item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Log.Debug(Defines.CLIENT, $"Event: Snapped item {networkItem.itemNetworkData.dataId} to {networkItem.itemNetworkData.creatureNetworkId} in slot {networkItem.itemNetworkData.drawSlot}.");

            networkItem.OnHoldStateChanged();
        }

        private void Holder_UnSnapped(Item item) {
            if(!IsSending()) return;

            NetworkItem networkItem = item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Log.Debug(Defines.CLIENT, $"Event: Unsnapped item {networkItem.itemNetworkData.dataId} from {networkItem.itemNetworkData.creatureNetworkId}.");

            networkItem.OnHoldStateChanged();
        }
        #endregion

        #region RagdollHand Events
        private void RagdollHand_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation, EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return; // Needs to be at end so everything is applied
            if(!IsSending()) return;

            NetworkItem networkItem = handle.item?.GetComponent<NetworkItem>();
            if(networkItem != null) {
                Log.Debug(Defines.CLIENT, $"Event: Grabbed item {networkItem.itemNetworkData.dataId} by {networkItem.itemNetworkData.creatureNetworkId} with hand {networkItem.itemNetworkData.holdingSide}.");

                networkItem.OnHoldStateChanged();
            } else {
                NetworkCreature networkCreature = handle.GetComponentInParent<NetworkCreature>();
                if(networkCreature != null && !networkCreature.IsSending() && creatureNetworkData == null) { // Check if creature found and creature calling the event is player
                    Log.Debug(Defines.CLIENT, $"Event: Grabbed creature {networkCreature.creatureNetworkData.creatureType} by player with hand {side}.");

                    new CreatureOwnerPacket(networkCreature.creatureNetworkData.networkedId, true).SendToServerReliable();
                }
            }
        }

        private void RagdollHand_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return; // Needs to be at end so everything is applied
            if(!IsSending()) return;

            NetworkItem networkItem = handle.item?.GetComponent<NetworkItem>();
            if(networkItem != null) {
                Log.Debug(Defines.CLIENT, $"Event: Ungrabbed item {networkItem.itemNetworkData.dataId} by {networkItem.itemNetworkData.creatureNetworkId} with hand {networkItem.itemNetworkData.holdingSide}.");

                networkItem.OnHoldStateChanged();
            }
        }
        #endregion

        private SpellCastData spellLeft = null;
        private SpellCastData spellRight = null;
        private SpellMergeData spellMerge = null;
        internal void CheckForMagic() {
            Log.Debug(creature.mana.casterRight.spellInstance + " " + creature.mana.casterLeft.spellInstance + " " + creature.mana.mergeInstance);
            if(creature.mana.mergeInstance != spellMerge) {
                Log.Debug("Merge: " + creature.name + " " + creature.mana.casterRight.spellInstance.id);
                
                spellMerge = creature.mana.mergeInstance;
                return;
            }
            if(creature.mana.casterRight.spellInstance != spellRight) {
                Log.Debug("Right: " + creature.name + " " + creature.mana.casterRight.spellInstance.id);

                spellRight = creature.mana.casterRight.spellInstance;
            }
            if(creature.mana.casterLeft.spellInstance != spellLeft) {
                Log.Debug("Left: " + creature.name + " " + creature.mana.casterRight.spellInstance.id);

                spellLeft = creature.mana.casterLeft.spellInstance;
            }
            //creature.mana.casterRight.LoadSpell(this.spells[0]);
        }

        private bool hasPhysicsModifiers = false;
        internal virtual void UpdateCreature() {
            if(creature == null) return;

            bool owning = IsSending();

            creature.locomotion.rb.useGravity = owning;
            creature.climber.enabled = owning;
            creature.mana.enabled = owning;

            if(owning) {
                creature.brain.instance.Start();

                if(hasPhysicsModifiers) creature.ragdoll.ClearPhysicModifiers();
                hasPhysicsModifiers = false;
            } else {
                creature.brain.Stop();
                creature.brain.StopAllCoroutines();
                creature.locomotion.MoveStop();

                if(ragdollParts == null) {
                    if(hasPhysicsModifiers) creature.ragdoll.ClearPhysicModifiers();
                    hasPhysicsModifiers = false;

                    if(creature.ragdoll.state != Ragdoll.State.Standing) creature.ragdoll.StandUp();
                } else {
                    if(GameConfig.useAdvancedNpcSyncing) creature.ragdoll.standingUp = true;

                    creature.ragdoll.SetPhysicModifier(null, 0, 0, 99999999, 99999999);
                    hasPhysicsModifiers = true;
                    try {
                        creature.ragdoll.SetState(Ragdoll.State.Inert, true);
                    } catch(NullReferenceException e) {
                        Log.Err(e);
                    }
                }
            }

            //Log.Debug(">> " + creature + " " + owning + " " + ragdollParts);
        }
    }
}
