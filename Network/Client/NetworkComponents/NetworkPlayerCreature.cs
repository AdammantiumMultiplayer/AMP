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
                    
                    if(audioSource != null) {
                        audioSource.Start();
                        
                        audioSource.gameObject.transform.parent = playerNetworkData.creature.transform;
                        
                        UpdateAudioSource();
                    } else {
                        audioSource = null;
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
                Log.Debug($"Enable Audio Source for {playerNetworkData?.name} " + AudioSource?.gameObject?.name);
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

            // The performance option got changed, set the ragdoll up for the other mode
            if(physicsArmsApplied != ModLoader.PhysicsBasedPlayerRagdolls && creature != null) UpdateCreature();

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
            
            SetupRagdollPhysics();

            creature.locomotion.enabled = false;
            creature.SetAnimatorHeightRatio(0f);

            if(playerNetworkData.clientId != ModManager.clientInstance.netclient.ClientId) // Only because of DEBUG_SELF
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, Datatypes.ItemHolderType.PLAYER);
        }

        #region Ragdoll physics
        // Arm and hand parts with the index of their synced pose, driven with physics in FixedUpdate
        private readonly List<KeyValuePair<RagdollPart, int>> drivenParts = new List<KeyValuePair<RagdollPart, int>>();

        // Mode the ragdoll got set up with, it gets set up again when the option changes (see ManagedUpdate)
        private bool physicsArmsApplied = false;

        /// <summary>
        /// Physics based (option "Physics Based Player Ragdolls", only with the full body syncing): The body is placed where the other player is
        /// (kinematic, nothing to simulate), the arms and hands are driven with physics like the close NPCs. So a weapon they hold can get blocked,
        /// instead of being forced through.
        /// Otherwise all parts are placed (SmoothDampRagdoll), the arms are frozen so nothing else moves them.
        /// </summary>
        private void SetupRagdollPhysics() {
            drivenParts.Clear();
            bool fullBody = ragdollPositions != null && ragdollPositions.Length > 0;
            bool driveArms = fullBody && ModLoader.PhysicsBasedPlayerRagdolls;
            physicsArmsApplied = ModLoader.PhysicsBasedPlayerRagdolls;

            // Remove the drag of the placed mode (see NetworkCreature.UpdateCreature), it would stop the driven arms
            if(driveArms && hasPhysicsModifiers) {
                creature.ragdoll.ClearPhysicModifiers();
                hasPhysicsModifiers = false;
            }

            int index = 0;
            foreach(RagdollPart part in creature.ragdoll.parts) {
                bool isRoot = part == creature.ragdoll.rootPart;
                bool isArm = Config.playerRagdollTypesToFreeze.Contains(part.type);
                PhysicBody body = part.physicBody;

                if(isArm) {
                    // Not moved along with the body, the arms get their own pose
                    part.transform.SetParent(creature.ragdoll.transform, true);
                    part.meshBone.SetParentOrigin(part.transform);
                }

                body.useGravity = false;
                if(isRoot) {
                    // Not part of the synced pose, follows through its joints
                    body.isKinematic = false;
                } else if(!driveArms) {
                    body.isKinematic = false;
                    if(isArm) {
                        body.constraints = RigidbodyConstraints.FreezeAll;
                        if(body.rigidBody != null) body.rigidBody.maxDepenetrationVelocity = Physics.defaultMaxDepenetrationVelocity; // In case the physics mode was on before
                    }
                } else if(isArm) {
                    body.constraints = RigidbodyConstraints.None;
                    body.isKinematic = false;
                    part.DisableCharJointLimit(); // Like the spawner does it, the state change can bring the limits back, they would stop poses the player can do
                    if(body.rigidBody != null) {
                        body.rigidBody.maxAngularVelocity = 50f; // Unitys default of 7 rad/s is too slow for swings
                        body.rigidBody.maxDepenetrationVelocity = Config.PLAYER_PHYSICS_MAX_DEPENETRATION;
                    }
                    drivenParts.Add(new KeyValuePair<RagdollPart, int>(part, index));
                } else {
                    body.isKinematic = true;
                }

                if(!isRoot) index++;
            }

            physicsParts = drivenParts.Count > 0 ? new HashSet<RagdollPart>(drivenParts.Select(entry => entry.Key)) : null;

            // The arms are moved by physics now, so they have to be able to pass through the body (the arm swing goes down along the legs).
            // The self collision gets turned off when the player spawns, but the ragdoll state change (base.UpdateCreature) can bring parts of it back.
            if(drivenParts.Count > 0) creature.SetSelfCollision(false);
        }

        public override void ManagedFixedUpdate() {
            base.ManagedFixedUpdate();
            if(IsSending()) return;

            DriveArms();
        }

        private void DriveArms() {
            if(drivenParts.Count == 0) return;
            if(ragdollPositions == null || ragdollRotations == null) return;
            if(creature == null || !creature.initialized) return;

            Vector3 origin = creature.transform.position;
            float snapDistanceSqr = Config.RAGDOLL_PHYSICS_SNAP_DISTANCE * Config.RAGDOLL_PHYSICS_SNAP_DISTANCE;
            float maxError = 0f;
            foreach(KeyValuePair<RagdollPart, int> entry in drivenParts) {
                if(entry.Key == null || entry.Value >= ragdollPositions.Length || entry.Value >= ragdollRotations.Length) continue;

                // The arms are children of the creature (see SetupRagdollPhysics), moving it already carries them along, so only the pose is driven
                float error = CreatureExtension.DrivePart(entry.Key, origin + ragdollPositions[entry.Value], ragdollRotations[entry.Value], Vector3.zero,
                                                          Config.PLAYER_PHYSICS_RESPONSIVENESS, Config.PLAYER_PHYSICS_MAX_SPEED, snapDistanceSqr);
                if(error > maxError) maxError = error;
            }

            // RAGDOLL-DEBUG: If the arms don't follow, this tells if they are held back (big error) or the target itself is wrong (small error)
            if(Config.LOG_RAGDOLL_DEBUGGING) RagdollDebug("arms" + GetInstanceID(), $"{playerNetworkData.name}: arms max {maxError:0.00}m off the synced pose, {drivenParts.Count} driven parts");
        }

        // Original depenetration speed of the items held by this player, restored when they let go
        private readonly Dictionary<Item, float> heldItemDepenetration = new Dictionary<Item, float>();

        /// <summary>
        /// The game only ignores the collision between a held item and the hand / arm of creatures that aren't the local player,
        /// so the weapon of another player would hit their own head and body. Also lets the weapon get pushed out of overlaps softer, like the arms.
        /// </summary>
        private void OnItemHeld(Handle handle) {
            if(handle == null || handle.item == null || creature == null || creature.ragdoll == null) return;
            Item item = handle.item;

            item.IgnoreRagdollCollision(creature.ragdoll);

            Rigidbody rigidbody = item.physicBody?.rigidBody;
            if(rigidbody != null && ModLoader.PhysicsBasedPlayerRagdolls) { // Only matters for blocking the physics driven arms
                if(!heldItemDepenetration.ContainsKey(item)) heldItemDepenetration.Add(item, rigidbody.maxDepenetrationVelocity);
                rigidbody.maxDepenetrationVelocity = Config.PLAYER_PHYSICS_MAX_DEPENETRATION;
            }
        }

        /// <param name="force">Restore it even though the item is still held (player gets removed)</param>
        private void OnItemReleased(Handle handle, bool force = false) {
            if(handle == null || handle.item == null) return;
            Item item = handle.item;
            if(!force && item.handlers.Exists(hand => hand.creature == creature)) return; // Still held with the other hand

            float depenetration;
            if(heldItemDepenetration.TryGetValue(item, out depenetration)) {
                Rigidbody rigidbody = item.physicBody?.rigidBody;
                if(rigidbody != null) rigidbody.maxDepenetrationVelocity = depenetration;
                heldItemDepenetration.Remove(item);
            }
        }

        private void RagdollHand_OnGrabEvent(Side side, Handle handle, float axisPosition, HandlePose orientation, EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return; // The grab refreshes the item collision, so do it afterwards
            OnItemHeld(handle);
        }

        private void RagdollHand_OnUnGrabEvent(Side side, Handle handle, bool throwing, EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return; // Letting go resets the item collision by itself (Item.RefreshCollision)
            OnItemReleased(handle);
        }
        #endregion

        private bool registeredEvents = false;
        internal new void RegisterEvents() {
            if(registeredEvents) return;
            if(creature == null) return;

            creature.OnDamageEvent += Creature_OnDamageEvent;
            creature.OnHealEvent += Creature_OnHealEvent;
            creature.OnHeightChanged += Creature_OnHeightChanged;
            creature.ragdoll.OnGrabEvent += Ragdoll_OnGrabEvent;
            creature.ragdoll.OnUngrabEvent += Ragdoll_OnUngrabEvent;

            foreach(RagdollHand hand in new RagdollHand[] { creature.handLeft, creature.handRight }) {
                if(hand == null) continue;
                hand.OnGrabEvent += RagdollHand_OnGrabEvent;
                hand.OnUnGrabEvent += RagdollHand_OnUnGrabEvent;
                OnItemHeld(hand.grabbedHandle); // Items that got equipped before the events were registered
            }

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

            foreach(RagdollHand hand in new RagdollHand[] { creature.handLeft, creature.handRight }) {
                if(hand == null) continue;
                hand.OnGrabEvent -= RagdollHand_OnGrabEvent;
                hand.OnUnGrabEvent -= RagdollHand_OnUnGrabEvent;
                OnItemReleased(hand.grabbedHandle, true);
            }

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
            if(AudioSource == null) {
                Log.Err("Error setting audio source for " + playerNetworkData?.name);
                return;
            }
            AudioSource.gameObject.transform.localPosition = new Vector3(0, playerNetworkData.height - 0.1f, 0);

            AudioSource.SetVolume(ModLoader._VoiceChatVolume);
            AudioSource.SetProximity(ModLoader._EnableProximityChat);
        }

        
    }
}
