using AMP.Data;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Modules;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using Netamite.Unity.Voice.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
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
        
        internal float currentRotation = 0f;
        internal float targetRotation = 0f;
        private float rotationVelocity = 0f;

        internal PlayerNetworkData playerNetworkData;

        private float health = 1f;
        public HealthbarObject healthBar;

        public JawController jawController;
        
        // Override instead of new, otherwise the code in NetworkCreature / NetworkPosition still uses the slower creature smoothing time for players
        internal override float SMOOTHING_TIME {
            get { return Config.PLAYER_MOVEMENT_DELTA_TIME; }
        }
        
        private NetamiteAudioSource audioSource = null;
        public NetamiteAudioSource AudioSource {
            get {
                if(audioSource == null) {
                    audioSource = ModManager.clientSync?.voiceClient?.GetAudioPlayer(playerNetworkData.clientId);
                    
                    if(audioSource) {
                        audioSource.Start();
                        
                        audioSource.gameObject.transform.parent = playerNetworkData.creature.transform;
                        
                        UpdateAudioSource();
                    }
                }

                return audioSource;
            }
        }

        internal void Init(PlayerNetworkData playerNetworkData) {
            if(this.playerNetworkData != playerNetworkData) registeredEvents = false;
            this.playerNetworkData = playerNetworkData;

            targetPos = this.playerNetworkData.position;
            //Log.Warn("INIT Player");

            UpdateCreature();
            RegisterEvents();

            if (ModLoader._EnableVoiceChat) {
                Log.Debug($"Enable Audio Source for {playerNetworkData.clientId} " + AudioSource.name);
                UpdateAudioSource();
            }
        }

        internal override bool IsSending() {
            return ModManager.clientInstance != null && playerNetworkData.clientId == ModManager.clientInstance.netclient.ClientId;
        }

        protected new void OnAwake() {
            base.OnAwake();
        }

        public override void ManagedUpdate() {
            if(IsSending()) return;

            //slowmo = playerNetworkData.health == 0;

            base.ManagedUpdate();

            UpdateRemoteSpellCasters();

            //Log.Info("NetworkPlayerCreature");

            if(health != playerNetworkData.health) {
                if(healthBar != null) {
                    healthBar.SetHealth(playerNetworkData.health);
                }
                health = playerNetworkData.health;
            }

            currentRotation = Mathf.SmoothDampAngle(currentRotation, targetRotation, ref rotationVelocity, Config.MOVEMENT_DELTA_TIME);
            if(ragdollPositions == null) {
                transform.eulerAngles = new Vector3(0, currentRotation, 0);

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
            } else {
                if(healthBar != null) healthBar.transform.localEulerAngles = new Vector3(0, 180 + currentRotation, 0);
            }

            if(creature.mana != null) {
                creature.mana.mergePoint.position = Vector3.Lerp(creature.handLeft.transform.position, creature.handRight.transform.position, 0.5f);

                // Pointing away from the head, like Mana.UpdateMerge does it (the mana loop is disabled), merged spells like the beams go that way.
                // The game uses the animated head, but the animator of other players is off, so the ragdoll head is used
                if(creature.ragdoll.headPart != null) {
                    Vector3 direction = creature.mana.mergePoint.position - creature.ragdoll.headPart.transform.position;
                    if(direction.sqrMagnitude > 0.0001f) creature.mana.mergePoint.rotation = Quaternion.LookRotation(direction, creature.transform.up);
                }
            }
            
            if(jawController != null && AudioSource != null && AudioSource.enabled && AudioSource.AudioSource.isPlaying) {
                const float maxLevel = 0.8f; // level that counts as "fully open"
                jawController.targetWeight = Mathf.Clamp01(AudioSource.GetRms() / maxLevel);
            }
        }


        #region Spell effects
        private readonly HashSet<SpellCaster> casterErrorLogged = new HashSet<SpellCaster>();

        /// <summary>
        /// The mana loop of remote players is disabled (see UpdateCreature), it would also do the gameplay part of the spells and throws for them.
        /// But it's also what updates the spells visuals (charge effect intensity, finger effects, ready effect, orb position), so without it
        /// none of the effects play. This runs the casters own update instead, which works for every spell type, including modded ones.
        /// </summary>
        private void UpdateRemoteSpellCasters() {
            if(creature.mana == null) return;

            // Run the update loops manually so the effects get applied correclty
            UpdateRemoteSpellCaster(creature.mana.casterLeft);
            UpdateRemoteSpellCaster(creature.mana.casterRight);

            // Merged spells (beams, zaps, charge effect), same reason, the merge update is part of the disabled mana loop as well
            if(creature.mana.mergeInstance != null) RemoteSpellMerge.UpdateMerge(creature.mana);
        }

        private void UpdateRemoteSpellCaster(SpellCaster caster) {
            if(caster == null || caster.spellInstance == null || !caster.isFiring) return;

            SpellCastCharge charge = caster.spellInstance as SpellCastCharge;
            float syncedCharge = 0f;
            if(charge != null) {
                // Grip casting reads our own cast button. Spraying stays on for the visuals, its damage gets ignored (IsDoneByRemotePlayerSpell)
                charge.allowGripCast = false;
                syncedCharge = charge.currentCharge;
            }

            try {
                caster.ManaUpdate();
            } catch(Exception e) {
                if(casterErrorLogged.Add(caster)) Log.Err(Defines.CLIENT, $"Updating the spell {caster.spellInstance.id} of {playerNetworkData.name} failed: {e}");
            }

            // The update charges the spell by itself, the charge comes from the caster (MagicChargePacket)
            if(charge != null) charge.currentCharge = syncedCharge;
        }
        #endregion

        internal override void UpdateCreature(bool reset_pos = false) {
            if(creature == null) return;
            
            base.UpdateCreature(reset_pos);
            
            if(ModLoader._EnableJawAnimation)
                jawController = JawController.Get(creature);
            
            creature.animator.enabled = false;
            creature.StopAnimation();
            creature.animator.speed = 0f;
            creature.locomotion.enabled = false;
            
            creature.ragdoll.standingUp = true;
            
            if(creature.mana != null) {
                // Stops the mana loop (regen, merging, powers, spell updates). It runs the gameplay part of spells and expects the hands
                // to be the local players, so it throws for other players. The spells are run by UpdateRemoteSpellCasters instead.
                // EnabledManagedLoops is a property, setting it through reflection didn't do anything, disabling the component does
                creature.mana.enabled = false;
            }
            
            //creature.ragdoll.SetState(Ragdoll.State.Standing);
            
            // Freeze some components of the ragdoll so we dont have issues with gravity
            
            foreach(RagdollPart ragdollPart in creature.ragdoll.parts.Where(part => Config.playerRagdollTypesToFreeze.Contains(part.type))) {
                ragdollPart.transform.SetParent(creature.ragdoll.transform, true);
                ragdollPart.meshBone.SetParentOrigin(ragdollPart.transform);

                ragdollPart.physicBody.constraints = RigidbodyConstraints.FreezeAll;
                //ragdollPart.physicBody.ForceFreeze();
                ragdollPart.physicBody.isKinematic = false;
                ragdollPart.physicBody.useGravity = false;
            }

            foreach(RagdollPart ragdollPart in creature.ragdoll.parts.Where(part => !Config.playerRagdollTypesToFreeze.Contains(part.type))) {
                ragdollPart.physicBody.isKinematic = false;
                ragdollPart.physicBody.useGravity = false;
            }

            creature.locomotion.enabled = false;
            creature.SetAnimatorHeightRatio(0f);

            if(playerNetworkData.clientId != ModManager.clientInstance.netclient.ClientId) // Only because of DEBUG_SELF
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, Datatypes.ItemHolderType.PLAYER);
        }

        private bool registeredEvents = false;
        internal new void RegisterEvents() {
            if(registeredEvents) return;
            if(creature == null) return;

            creature.OnDamageEvent += Creature_OnDamageEvent;
            creature.OnHealEvent += Creature_OnHealEvent;
            creature.OnHeightChanged += Creature_OnHeightChanged;
            creature.ragdoll.OnGrabEvent += Ragdoll_OnGrabEvent;
            creature.ragdoll.OnUngrabEvent += Ragdoll_OnUngrabEvent;

            if (playerNetworkData.clientId != ModManager.clientInstance.netclient.ClientId) // Only because of DEBUG_SELF
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, Datatypes.ItemHolderType.PLAYER);

            registeredEvents = true;
        }

        internal new void UnregisterEvents() {
            if(!registeredEvents) return;

            creature.OnDamageEvent -= Creature_OnDamageEvent;
            creature.OnHealEvent -= Creature_OnHealEvent;
            creature.OnHeightChanged -= Creature_OnHeightChanged;
            creature.ragdoll.OnGrabEvent -= Ragdoll_OnGrabEvent;
            creature.ragdoll.OnUngrabEvent -= Ragdoll_OnUngrabEvent;

            registeredEvents = false;
        }

        private void Ragdoll_OnUngrabEvent(RagdollHand ragdollHand, HandleRagdoll handleRagdoll, bool lastHandler) {
            if (lastHandler)
                disabled_parts = new RagdollPart.Type[0];
            else
                disabled_parts = creature.GetDisabledParts();
        }

        private void Ragdoll_OnGrabEvent(RagdollHand ragdollHand, HandleRagdoll handleRagdoll) {
            disabled_parts = creature.GetDisabledParts();
        }

        private void Creature_OnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            if(collisionInstance.IsDoneByRemotePlayerSpell()) { creature.currentHealth = creature.maxHealth; return; } // The caster of the spell reports the hit
            //if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health
            if (collisionInstance.IsDoneByCreature(creature)) { Log.Warn(Defines.AMP, "SelfDamage"); return; } // If the damage is done by the creature itself, ignore it
            if(!collisionInstance.IsDoneByPlayer() && !collisionInstance.IsDoneByAnyCreature() && !collisionInstance.IsDoneByAnyProjectile()) { Log.Warn(Defines.AMP, "NonPlayerDamage"); return; }; // Only if the damage is done by a creature and not some random debris, should stop people from random death
            
            // Damage needs to come from a held item if it comes from an item, but this will probably prevent arrows and magic projectiles from working :/
            if(collisionInstance.sourceColliderGroup && collisionInstance.sourceColliderGroup.collisionHandler.item != null) {
                Item item = collisionInstance.sourceColliderGroup.collisionHandler.item;
                if(!item.IsHeld() && !item.isMoving && !item.isFlying) { Log.Warn(Defines.AMP, "NonActiveItemDamage"); return; }; // Maybe that prevents unwanted damage?
            }
            
            float damage = creature.currentHealth - creature.maxHealth; // Should be negative
            if(damage >= 0) return;
            creature.currentHealth = creature.maxHealth;

            new PlayerHealthChangePacket(playerNetworkData.clientId, damage, collisionInstance.impactVelocity, collisionInstance.IsDoneByPlayer()).SendToServerReliable();
            
            Log.Debug(Defines.CLIENT, $"Damaged player {playerNetworkData.name} with {damage} damage and {collisionInstance.impactVelocity} force.");
        }

        private void Creature_OnHealEvent(float heal, Creature healer, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            if(healer == null) return;
            if(!healer.isPlayer) return;

            new PlayerHealthChangePacket(playerNetworkData.clientId, heal, Vector3.zero).SendToServerReliable();
            Log.Debug(Defines.CLIENT, $"Healed {playerNetworkData.name} with {heal} heal.");
        }

        private void Creature_OnHeightChanged() {
            if(creature.GetHeight() != playerNetworkData.height) {
                new SizeChangePacket(playerNetworkData);
            }
        }

        private void UpdateAudioSource() {
            AudioSource.gameObject.transform.localPosition = new Vector3(0, playerNetworkData.height - 0.1f, 0);

            AudioSource.SetVolume(ModLoader._VoiceChatVolume);
            AudioSource.SetProximity(ModLoader._EnableProximityChat);
        }

        
    }
}
