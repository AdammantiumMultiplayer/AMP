﻿using AMP.Data;
using AMP.Datatypes;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ThunderRoad;
using ThunderRoad.Skill.SpellPower;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    internal class NetworkCreature : NetworkPosition {

        internal Creature creature;
        internal CreatureNetworkData creatureNetworkData;

        internal Vector3[] ragdollPositions = null;
        internal Vector3[] ragdollPartsVelocity = null;
        internal Quaternion[] rotationVelocity = null;
        internal Quaternion[] ragdollRotations = null;

        internal bool slowmo = false;

        internal void Init(CreatureNetworkData creatureNetworkData) {
            if(this.creatureNetworkData != creatureNetworkData) registeredEvents = false;
            this.creatureNetworkData = creatureNetworkData;

            targetPos = this.creatureNetworkData.position;
            //Log.Warn("INIT Creature");

            if(ModManager.clientSync.syncData.owningCreatures.Contains(creatureNetworkData.networkedId) != IsSending()) {
                creatureNetworkData.SetOwnership(ModManager.clientSync.syncData.owningCreatures.Contains(creatureNetworkData.networkedId));
            }

            if(!IsSending()) UpdateCreature();
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

            DisableSelfCollision();

            //creature.locomotion.rb.drag = 0;
            //creature.locomotion.rb.angularDrag = 0;

            NetworkComponentManager.SetTickRate(this, (int) (0.2f / Time.fixedDeltaTime), ManagedLoops.FixedUpdate);
        }

        public override ManagedLoops EnabledManagedLoops => ManagedLoops.FixedUpdate | ManagedLoops.Update;

        public override void ManagedFixedUpdate() {
            if(IsSending()) {
                CheckForMagic();
            } else {
            
            }
        }

        public override void ManagedUpdate() {
            if(IsSending()) return;

            //Log.Debug(creature.lastInteractionTime + " / " + Time.time);
            if(creature.lastInteractionTime < Time.time - (Config.NET_COMP_DISABLE_DELAY / 1000f)) return;
            if(!creature.initialized) return;
            if(creatureNetworkData != null && creatureNetworkData.isSpawning) return;

            //if(creatureNetworkData != null) Log.Info("NetworkCreature");

            base.ManagedUpdate();

            try {
                if(!creature.IsVisible()) return;
            }catch(Exception) { }

            if(ragdollPositions != null && ragdollRotations != null) {
                //creature.ApplyRagdoll(ragdollPositions, ragdollRotations);
                creature.SmoothDampRagdoll(ragdollPositions, ragdollRotations, ref ragdollPartsVelocity, ref rotationVelocity, SMOOTHING_TIME);
            }

            creature.locomotion.physicBody.velocity = positionVelocity;
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

        public void SetRagdollInfo(Vector3[] positions, Quaternion[] rotations) {
            this.ragdollPositions = positions;
            this.ragdollRotations = rotations;

            if(positions != null) {
                if(ragdollPartsVelocity == null || ragdollPartsVelocity.Length != positions.Length) { // We only want to set the velocity if ragdoll parts are synced
                    ragdollPartsVelocity = new Vector3[positions.Length];
                    rotationVelocity = new Quaternion[positions.Length];
                    UpdateCreature(true);
                }
            } else if(ragdollPartsVelocity != null) {
                ragdollPartsVelocity = null;
                rotationVelocity = null;
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
            creature.OnHeightChanged += Creature_OnHeightChanged;

            creature.ragdoll.OnSliceEvent += Ragdoll_OnSliceEvent;
            creature.ragdoll.OnTelekinesisGrabEvent += Ragdoll_OnTelekinesisGrabEvent;
            creature.ragdoll.OnGrabEvent += Ragdoll_OnGrabEvent;

            RegisterGrabEvents();
            RegisterBrainEvents();

            if(!IsSending()) {
                ClientSync.EquipItemsForCreature(creatureNetworkData.networkedId, ItemHolderType.CREATURE);
            }

            registeredEvents = true;
        }

        private void Creature_OnHeightChanged() {
            if(creature.GetHeight() != creatureNetworkData.height) {
                new SizeChangePacket(creatureNetworkData);
            }
        }

        protected void RegisterGrabEvents() {
            if(creature.handLeft != null && creature.handRight != null) {
                foreach(RagdollHand rh in new RagdollHand[] { creature.handLeft, creature.handRight }) {
                    rh.OnGrabEvent += RagdollHand_OnGrabEvent;
                    rh.OnUnGrabEvent += RagdollHand_OnUnGrabEvent;

                    if(rh.grabbedHandle != null && IsSending()) RagdollHand_OnGrabEvent(rh.side, rh.grabbedHandle, 0, null, EventTime.OnEnd);
                }
            }
            if(creature.holders != null) {
                foreach(Holder holder in creature.holders) {
                    holder.UnSnapped += Holder_UnSnapped;
                    holder.Snapped += Holder_Snapped;

                    if(holder.items.Count > 0 && IsSending()) Holder_Snapped(holder.items[0]);
                }
            }
        }

        protected void RegisterBrainEvents() {
            if(creature.brain == null) return;

        }
        #endregion

        #region Unregister Events
        public override void ManagedOnDisable() {
            Destroy(this);
            UnregisterEvents();
        }

        internal void UnregisterEvents() {
            if(!registeredEvents) return;

            creature.OnDamageEvent -= Creature_OnDamageEvent;
            creature.OnHealEvent -= Creature_OnHealEvent;
            creature.OnKillEvent -= Creature_OnKillEvent;
            creature.OnDespawnEvent -= Creature_OnDespawnEvent;

            creature.ragdoll.OnSliceEvent -= Ragdoll_OnSliceEvent;

            UnregisterGrabEvents();
            UnregisterBrainEvents();

            registeredEvents = false;
        }

        protected void UnregisterGrabEvents() {
            if(creature.handLeft != null && creature.handRight != null) {
                foreach(RagdollHand rh in new RagdollHand[] { creature.handLeft, creature.handRight }) {
                    rh.OnGrabEvent -= RagdollHand_OnGrabEvent;
                    rh.OnUnGrabEvent -= RagdollHand_OnUnGrabEvent;
                }
            }
            foreach(Holder holder in creature.holders) {
                holder.UnSnapped -= Holder_UnSnapped;
                holder.Snapped -= Holder_Snapped;
            }
        }

        protected void UnregisterBrainEvents() {
            if(creature.brain == null) return;

        }
        #endregion

        #region Ragdoll Events
        private void Ragdoll_OnSliceEvent(RagdollPart ragdollPart, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            if(!IsSending()) return; //creatureNetworkData.TakeOwnershipPacket().SendToServerReliable();

            Log.Debug(Defines.CLIENT, $"Event: Creature {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId}) lost {ragdollPart.type}.");

            new CreatureSlicePacket(creatureNetworkData.networkedId, ragdollPart).SendToServerReliable();
        }

        private void Ragdoll_OnTelekinesisGrabEvent(SpellTelekinesis spellTelekinesis, HandleRagdoll handleRagdoll) {
            if(!IsSending()) {
                creatureNetworkData.RequestOwnership();
            }
        }

        private void Ragdoll_OnGrabEvent(RagdollHand ragdollHand, HandleRagdoll handleRagdoll) {
            if(!ragdollHand.creature.isPlayer) return;
            if(!IsSending()) {
                creatureNetworkData.RequestOwnership();
            }
        }
        #endregion

        #region Creature Events
        private void Creature_OnDespawnEvent(EventTime eventTime) {
            if(eventTime == EventTime.OnEnd) return;
            //if(!ModManager.clientInstance.allowTransmission) return; // Always send it, or they might get stuck in T Pose
            if(creatureNetworkData.networkedId <= 0) return;

            //if(IsSending()) {
                Log.Debug(Defines.CLIENT, $"Event: Creature {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId}) is despawned.");

                new CreatureDepawnPacket(creatureNetworkData).SendToServerReliable();

                ModManager.clientSync.syncData.creatures.TryRemove(creatureNetworkData.networkedId, out _);

                creatureNetworkData.networkedId = 0;

                Destroy(this);
            //}
        }

        private void Creature_OnKillEvent(CollisionInstance collisionInstance, EventTime eventTime) {
            if(eventTime == EventTime.OnEnd) UpdateCreature();
            if(eventTime == EventTime.OnStart) return;
            if(creatureNetworkData.networkedId <= 0) return;

            /*if(creatureNetworkData.health > 0 && IsSending()) {
                if(!collisionInstance.IsDoneByPlayer()) {
                    creature.currentHealth = creatureNetworkData.health;
                    throw new Exception("Creature died by unknown causes, need to throw this exception to prevent it.");
                }
            }*/
            if(creatureNetworkData.health > 0) {
                creatureNetworkData.health = -1;

                creatureNetworkData.RequestOwnership();
                new CreatureHealthSetPacket(creatureNetworkData).SendToServerReliable();
            }

            if(creature.isKilled) return;
            if(!hasPhysicsModifiers) hasPhysicsModifiers = true;
            UpdateCreature();
        }

        private void Creature_OnHealEvent(float heal, Creature healer, EventTime eventTime) {
            if(creatureNetworkData.networkedId <= 0) return;
            if(healer == null) return;

            new CreatureHealthChangePacket(creatureNetworkData.networkedId, heal).SendToServerReliable();
            Log.Debug(Defines.CLIENT, $"Healed {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId}) with {heal} heal.");
        }

        private void Creature_OnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime) {
            //if(!collisionInstance.IsDoneByPlayer()) {
            //    creature.currentHealth = creatureNetworkData.health;
            //    return; // Damage is not caused by the local player, so no need to mess with the other clients health
            //}
            if(collisionInstance.IsDoneByCreature(creatureNetworkData.creature)) return; // If the damage is done by the creature itself, ignore it
            if(creatureNetworkData.networkedId <= 0) return;

            float damage = creatureNetworkData.creature.currentHealth - creatureNetworkData.health; // Should be negative
            if(damage >= 0) return; // No need to send it, if there was no damaging
            //Log.Debug(collisionInstance.damageStruct.damage + " / " + damage);
            creatureNetworkData.health = creatureNetworkData.creature.currentHealth;

            //creatureNetworkData.RequestOwnership();
            new CreatureHealthChangePacket(creatureNetworkData.networkedId, damage).SendToServerReliable();
            Log.Debug(Defines.CLIENT, $"Damaged {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId}) with {damage} damage.");
        }
        #endregion

        #region Holder Events
        private void Holder_Snapped(Item item) {
            if(!IsSending()) return;

            NetworkItem networkItem = item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Dispatcher.Enqueue(() => {
                networkItem.OnHoldStateChanged();

                Log.Debug(Defines.CLIENT, $"Event: Snapped item {networkItem.itemNetworkData.dataId} to {networkItem.itemNetworkData.holdingStatesInfo}.");
            });
        }

        private void Holder_UnSnapped(Item item) {
            if(!IsSending()) return;

            NetworkItem networkItem = item.GetComponent<NetworkItem>();
            if(networkItem == null) return;

            Dispatcher.Enqueue(() => {
                networkItem.OnHoldStateChanged();

                Log.Debug(Defines.CLIENT, $"Event: Unsnapped item {networkItem.itemNetworkData.dataId}.");
            });
        }
        #endregion

        #region RagdollHand Events
        private void RagdollHand_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation, EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return; // Needs to be at end so everything is applied
            if(!IsSending()) return;

            NetworkItem networkItem = handle?.item?.GetComponentInParent<NetworkItem>();
            if(networkItem != null) {
                networkItem.OnHoldStateChanged();

                Log.Debug(Defines.CLIENT, $"Event: Grabbed item {networkItem.itemNetworkData.dataId} by {networkItem.itemNetworkData.holdingStatesInfo}.");
            } else {
                NetworkCreature networkCreature = handle?.GetComponentInParent<NetworkCreature>();
                if(networkCreature != null && !networkCreature.IsSending() && creatureNetworkData != null) { // Check if creature found and creature calling the event is player
                    Log.Debug(Defines.CLIENT, $"Event: Grabbed creature {networkCreature.creatureNetworkData.creatureType} by player with hand {side}.");

                    creatureNetworkData.RequestOwnership();
                }
            }
        }

        private void RagdollHand_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return; // Needs to be at end so everything is applied
            if(!IsSending()) return;
            if(handle == null) return;

            NetworkItem networkItem = handle.item?.GetComponentInParent<NetworkItem>();
            if(networkItem != null) {
                networkItem.OnHoldStateChanged();

                Log.Debug(Defines.CLIENT, $"Event: Ungrabbed item {networkItem.itemNetworkData.dataId}.");
            }
        }
        #endregion

        #region Brain Events

        #endregion

        #region Creature Fixes
        private bool hasPhysicsModifiers = false;
        internal virtual void UpdateCreature(bool reset_pos = false) {
            if(creature == null) return;

            bool owning = IsSending();

            if(owning) {
                NetworkComponentManager.SetTickRate(this, UnityEngine.Random.Range(50, 200), ManagedLoops.Update);
            } else {
                NetworkComponentManager.SetTickRate(this, 1, ManagedLoops.Update);
            }

            bool enablePhysics = owning || (ragdollPositions == null || ragdollPositions.Length == 0);
            bool enableBrain = owning;
            bool enableSelfDamage = owning;
            bool shouldBeKilled = creatureNetworkData != null && (creatureNetworkData.health <= 0 || creature.currentHealth <= 0) && !creature.isKilled;

            if(shouldBeKilled) {
                enablePhysics = true;
                enableBrain = false;
                enableSelfDamage = false;

                creature.Kill();
                Log.Warn("Killed " + creature.gameObject.name);
            } else if(creature.isKilled) {
                enableBrain = false;
                enableSelfDamage = false;
            }

            if(enableBrain) {
                creature.brain?.instance?.Start();
            } else {
                creature.brain?.instance?.Stop();
            }

            creature.ragdoll.allowSelfDamage = enableSelfDamage;
            creature.SetSelfCollision(enableSelfDamage);

            if(enablePhysics) {
                if(!creature.isKilled) {
                    if(creature.ragdoll.state == Ragdoll.State.Inert) {
                        creature.ragdoll.SetState(Ragdoll.State.Standing, true);
                    }
                }
                creature.ragdoll.physicToggle = true;

                if(hasPhysicsModifiers) creature.ragdoll.ClearPhysicModifiers();
                hasPhysicsModifiers = false;

                creature.locomotion.enabled = !creature.isKilled;

                reset_pos = true;

                Log.Warn("Enabled " + creature.gameObject.name + " " + owning + " " + ragdollPositions);
            } else {
                if(!creature.isKilled) {
                    if(creature.ragdoll.state != Ragdoll.State.Inert) {
                        creature.ragdoll.SetState(Ragdoll.State.Inert, true);
                    }
                }
                creature.ragdoll.physicToggle = false;

                creature.ragdoll.SetPhysicModifier(null, 0, 0, 99999999, 99999999);
                hasPhysicsModifiers = true;

                creature.locomotion.enabled = false;

                Log.Warn("Disabled " + creature.gameObject.name);
            }

            if(reset_pos) {
                if(creatureNetworkData != null) {
                    creature.transform.position = creatureNetworkData.position;
                    if(creature.animator != null) creature.animator.rootPosition = creatureNetworkData.position;
                }
            }

            if(creature.locomotion != null && creatureNetworkData != null) {
                creature.locomotion.prevPosition = creatureNetworkData.position;
                creature.locomotion.transform.position = creatureNetworkData.position;
            }
        }

        private void DisableSelfCollision() {
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
            foreach(Collider collider in colliders) {
                foreach(Collider collider2 in colliders) {
                    if(collider == collider2) continue;
                    Physics.IgnoreCollision(collider, collider2, true);
                }
            }
        }
        #endregion

        #region Magic Syncing
        private class CastingInfo {
            public float currentCharge = 0f;
            public SpellCaster caster;
            public SpellCastCharge charge;
            public int casterId;
            public ItemHolderType casterType;
            public string magicId;
            public bool stopped = false;

            public CastingInfo(Creature c, Side side, string magicId, int casterId, ItemHolderType casterType) {
                this.caster = c.GetHand(side).caster;
                if(this.caster.spellInstance != null) {
                    charge = (SpellCastCharge) this.caster.spellInstance;
                    currentCharge = charge.currentCharge;
                }
                this.magicId = magicId;
                this.casterId = casterId;
                this.casterType = casterType;
            }
        }

        private Dictionary<Side, CastingInfo> currentActiveSpells = new Dictionary<Side, CastingInfo>();
        internal void OnSpellUsed(string spellId, Side side) {
            if(!IsSending()) return;
            if(spellId == null || spellId.Length == 0) return;

            if(currentActiveSpells.ContainsKey(side)) {
                if(currentActiveSpells[side].magicId.Equals(spellId)) {
                    return;
                }
            }

            ItemHolderType casterType;
            int casterNetworkId;
            if(SyncFunc.GetCreature(creature, out casterType, out casterNetworkId)) {
                //Log.Debug("Spell: " + creature.name + " " + side + " " + spellId);

                if(currentActiveSpells.ContainsKey(side)) {
                    currentActiveSpells[side] = new CastingInfo(creature, side, spellId, casterNetworkId, casterType);
                } else {
                    currentActiveSpells.Add(side, new CastingInfo(creature, side, spellId, casterNetworkId, casterType));
                }

                new MagicSetPacket(spellId, (byte) side, casterNetworkId, casterType).SendToServerReliable();
            }
        }

        internal void OnSpellStopped(Side side) {
            int casterNetworkId = 0;
            ItemHolderType casterType = ItemHolderType.NONE;

            if(currentActiveSpells.ContainsKey(side)) {
                CastingInfo castingInfo = currentActiveSpells[side];
                casterType = castingInfo.casterType;
                casterNetworkId = castingInfo.casterId;
                currentActiveSpells.Remove(side);
            }

            if(!IsSending()) return;

            if(casterType == ItemHolderType.NONE) if(!SyncFunc.GetCreature(creature, out casterType, out casterNetworkId)) return;

            new MagicSetPacket("", (byte) side, casterNetworkId, casterType).SendToServerReliable();
        }

        private void CheckForMagic() {
            if(currentActiveSpells.Count > 0) {
                foreach(KeyValuePair<Side, CastingInfo> entry in currentActiveSpells) {

                    if(entry.Value.stopped) continue;
                    if(entry.Value.caster.mana.mergeActive) {
                        if(entry.Value.currentCharge != entry.Value.caster.mana.mergeInstance.currentCharge) {
                            new MagicChargePacket(byte.MaxValue, entry.Value.casterId, entry.Value.casterType, entry.Value.caster.mana.mergeInstance.currentCharge, entry.Value.caster.GetShootDirection()).SendToServerUnreliable();

                            entry.Value.currentCharge = entry.Value.caster.mana.mergeInstance.currentCharge;
                        }
                        return; // No need to sync a merge spell twice
                    }else if(!entry.Value.caster.isFiring || entry.Value.caster.intensity <= 0) {
                        entry.Value.stopped = true;
                        Dispatcher.Enqueue(() => {
                            OnSpellStopped(entry.Key);
                        });
                    } else {
                        if(entry.Value.currentCharge != entry.Value.charge.currentCharge) { // Charge changed
                            new MagicChargePacket((byte) entry.Key, entry.Value.casterId, entry.Value.casterType, entry.Value.currentCharge, entry.Value.caster.GetShootDirection()).SendToServerUnreliable();

                            entry.Value.currentCharge = entry.Value.charge.currentCharge;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
