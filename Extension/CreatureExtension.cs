using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.Extension {
    internal static class CreatureExtension {

        internal static bool IsOtherPlayer(this Creature creature) {
            ItemHolderType itemHolder;
            int networkId;
            SyncFunc.GetCreature(creature, out itemHolder, out networkId);

            if (itemHolder == ItemHolderType.PLAYER && networkId == ModManager.clientInstance.netclient.ClientId) itemHolder = ItemHolderType.NONE;

            return itemHolder == ItemHolderType.PLAYER;
        }

        internal static string[] ReadWardrobe(this Creature creature) {
            List<string> equipment_list = new List<string>();

            if (creature.container != null && creature.container.contents != null) {
                foreach (ItemData item in creature.container.GetAllWardrobe()) {
                    equipment_list.Add(item.id);
                }
            }

            return equipment_list.ToArray();
        }

        internal static void ApplyWardrobe(this Creature creature, string[] equipment_list) {
            bool changed = false;

            if (creature.container == null || creature.container.contents == null) return;

            foreach (string referenceID in equipment_list) {
                bool found = false;
                foreach (ItemData item in creature.container.GetAllWardrobe()) {
                    if (item.id.Equals(referenceID)) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    ItemData itemData = Catalog.GetData<ItemData>(referenceID);
                    if (itemData == null) {
                        string replacement = DataReplacementFinder.FindWardrobeReplacement(referenceID);

                        itemData = Catalog.GetData<ItemData>(replacement);
                        if (itemData == null) {
                            Log.Err(Defines.CLIENT, $"Equipment { referenceID } for { creature.creatureId } not found, no replacement found, please check you mods.");
                        } else {
                            Log.Err(Defines.CLIENT, $"Equipment { referenceID } for { creature.creatureId } not found, using { replacement } instead.");
                        }
                    }
                    if (itemData != null && itemData.type == ItemData.Type.Wardrobe) {
                        ItemContent content = new ItemContent(itemData);
                        creature.equipment.EquipWardrobe(content, false);
                        changed = true;
                    }
                }
            }
            if (changed) {
                creature.equipment.UpdateParts();
            }
        }

        internal static Color[] ReadColors(this Creature creature) {
            List<Color> color_list = new List<Color> {
                creature.GetColor(Creature.ColorModifier.Hair),
                creature.GetColor(Creature.ColorModifier.HairSecondary),
                creature.GetColor(Creature.ColorModifier.HairSpecular),
                creature.GetColor(Creature.ColorModifier.EyesIris),
                creature.GetColor(Creature.ColorModifier.EyesSclera),
                creature.GetColor(Creature.ColorModifier.Skin)
            };

            return color_list.ToArray();
        }


        internal static void ApplyColors(this Creature creature, Color[] colors) {
            int i = 0;
            creature.SetColor(colors[i++], Creature.ColorModifier.Hair);
            creature.SetColor(colors[i++], Creature.ColorModifier.HairSecondary);
            creature.SetColor(colors[i++], Creature.ColorModifier.HairSpecular);
            creature.SetColor(colors[i++], Creature.ColorModifier.EyesIris);
            creature.SetColor(colors[i++], Creature.ColorModifier.EyesSclera);
            creature.SetColor(colors[i++], Creature.ColorModifier.Skin, true);
        }

        /// <summary>
        /// Reads the melee attack the creature is currently performing, together with the animation speed the brain plays it with
        /// </summary>
        internal static bool ReadCurrentAttack(this Creature creature, out string attackId, out float speedMultiplier) {
            attackId = "";
            speedMultiplier = 1f;

            if(creature.brain == null || creature.brain.instance == null) return false;
            BrainModuleMelee melee = creature.brain.instance.GetModule<BrainModuleMelee>(false);
            if(melee == null || melee.attack == null || string.IsNullOrEmpty(melee.attack.id)) return false;

            attackId = melee.attack.id;
            speedMultiplier = melee.GetAnimationSpeedMultiplier(melee.attack);
            return true;
        }

        /// <summary>
        /// Plays a melee attack by its id. Attacks belong to the stance (StanceData), they are not part of the AnimationData catalog.
        /// </summary>
        internal static bool PlayAttack(this Creature creature, string stanceId, string attackId, float speedMultiplier) {
            AttackMotion attack = FindAttackMotion(creature.GetStanceModule()?.currentStanceData, attackId);
            if(attack == null && !string.IsNullOrEmpty(stanceId)) {
                // The current stance is a filtered copy (difficulty, weapons), so also check the stance from the catalog
                StanceData stanceData;
                if(Catalog.TryGetData(stanceId, out stanceData, false)) attack = FindAttackMotion(stanceData, attackId);
            }

            if(attack == null || attack.animationClip == null) {
                Log.Err(Defines.CLIENT, $"Attack {attackId} of stance {stanceId} not found for {creature.creatureId}, please check you mods.");
                return false;
            }

            // Same as the game plays it (BrainModuleMelee.PerformAttack)
            creature.PlayAnimation(attack.animationClip, false, speedMultiplier, null, false, true, false);
            return true;
        }

        private static AttackMotion FindAttackMotion(StanceData stanceData, string attackId) {
            if(stanceData == null) return null;
            return stanceData.AllStanceNodes().OfType<AttackMotion>().FirstOrDefault(motion => motion.id == attackId);
        }

        internal static void ReadRagdoll(this Creature creature, out Vector3[] positions, out Quaternion[] rotations, out Vector3[] velocity, out Vector3[] angularVelocity, bool animJawBone = false, bool readMeshBones = false) {
            List<Vector3> vec3s = new List<Vector3>();
            List<Quaternion> quats = new List<Quaternion>();
            List<Vector3> vels = new List<Vector3>();
            List<Vector3> aVels = new List<Vector3>();

            Ragdoll.Bone jawBone = creature.ragdoll.GetBone(creature.jaw);
            foreach (RagdollPart part in creature.ragdoll.parts) {
                if (part == creature.ragdoll.rootPart) continue;

                Transform source = (readMeshBones && part.meshBone != null) ? part.meshBone : part.transform;

                vec3s.Add(source.position - creature.transform.position);

                if (animJawBone && part.bone == jawBone) {
                    quats.Add(jawBone.animation.rotation);
                } else {
                    quats.Add(source.rotation);
                }

                vels.Add(part.physicBody.velocity);
                aVels.Add(part.physicBody.angularVelocity);
            }
            positions = vec3s.ToArray();
            rotations = quats.ToArray();
            velocity = vels.ToArray();
            angularVelocity = aVels.ToArray();
        }

        private static readonly RagdollPart.Type[][] typeGroups = {
            new[] { RagdollPart.Type.LeftHand , RagdollPart.Type.LeftArm  },
            new[] { RagdollPart.Type.RightHand, RagdollPart.Type.RightArm },
            new[] { RagdollPart.Type.LeftFoot , RagdollPart.Type.LeftLeg  },
            new[] { RagdollPart.Type.RightFoot, RagdollPart.Type.RightLeg },
            new[] { RagdollPart.Type.Head     , RagdollPart.Type.Neck     },
        };

        /// <summary>
        /// Returns the types of all grabbed ragdoll parts. If a grabbed part belongs to a type group (like hand and arm),
        /// the whole group gets returned, so the limb is disabled as a whole.
        /// </summary>
        internal static RagdollPart.Type[] GetDisabledParts(this Creature creature) {
            return creature.ragdoll.parts
                           .Where(part => part.isGrabbed)
                           .SelectMany(part => typeGroups.FirstOrDefault(group => group.Contains(part.type)) ?? new[] { part.type })
                           .Distinct()
                           .ToArray();
        }

        internal static void ApplyRagdoll(this Creature creature, Vector3[] positions, Quaternion[] rotations) {
            int i = positions.Length - 1;
            
            for (int j = creature.ragdoll.parts.Count - 1; j >= 0; j--) { 
                RagdollPart part = creature.ragdoll.parts[j];

                if(part == creature.ragdoll.rootPart) continue;
                //foreach(Ragdoll.Bone bone in creature.ragdoll.bones) {
                if(positions.Length <= i) continue; // Prevent errors when the supplied vectors dont match the creatures
                if(rotations.Length <= i) continue; // Prevent errors when the supplied rotations dont match the creatures
                if(i < 0) continue;

                //part.meshBone.position       = positions[i];
                //part.bone.mesh.position      = positions[i];
                part.transform.position      = positions[i];
                //part.bone.animation.position = positions[i];

                //part.meshBone.rotation  = rotations[i];
                //part.bone.mesh.rotation = rotations[i];
                part.transform.rotation = rotations[i];
                //part.bone.animation.rotation = rotations[i];

                i--;
            }
            creature.ragdoll.SavePartsPosition();
        }

        internal static void SmoothDampRagdoll(this Creature creature, Vector3[] positions, Quaternion[] rotations, ref Vector3[] positionVelocity, ref Quaternion[] rotationVelocity, float smoothTime = -1f, RagdollPart.Type[] disabled_parts = null) {
            if(smoothTime < 0) smoothTime = Config.MOVEMENT_DELTA_TIME;

            if (disabled_parts == null) disabled_parts = new RagdollPart.Type[0];
            
            Vector3[] new_vectors = new Vector3[positions.Length];
            Quaternion[] new_rots = new Quaternion[rotations.Length];
            int i = 0;
            foreach(RagdollPart part in creature.ragdoll.parts) {
                if(part == creature.ragdoll.rootPart) continue;
                if(positions.Length <= i) continue; // Prevent errors when the supplied vectors dont match the creatures
                if(disabled_parts.Contains(part.type)) { // Skip parts that are disabled
                    i++;
                    continue;
                }

                new_vectors[i] = part.transform.position.InterpolateTo(positions[i] + creature.transform.position, ref positionVelocity[i], smoothTime);
                new_rots   [i] = part.transform.rotation.InterpolateTo(rotations[i],                               ref rotationVelocity[i], smoothTime);
                i++;
            }
            creature.ApplyRagdoll(new_vectors, new_rots);
        }

        private static BrainModuleStance GetStanceModule(this Creature creature) {
            if(creature.brain == null || creature.brain.instance == null) return null;
            return creature.brain.instance.GetModule<BrainModuleStance>(false);
        }

        /// <summary>
        /// Reads the current stance (combat stance like shield, dual wield, ...) and the idle pose within it
        /// </summary>
        internal static void ReadStance(this Creature creature, out string stanceId, out string idlePoseId) {
            BrainModuleStance module = creature.GetStanceModule();
            stanceId   = module?.currentStanceData?.id ?? "";
            idlePoseId = module?.currentPose?.id ?? "";
        }

        /// <summary>
        /// Applies a synced stance and idle pose. The brain isn't running on creatures we don't own, so the pose is applied instantly,
        /// the stance module only blends between poses (and picks random new ones) in its Update.
        /// </summary>
        /// <summary>
        /// Runs the stance module, even though the brain is stopped (creatures we don't own).
        /// SetStance only sets the goal, the animator only takes it while it is in the idle stance, the module keeps setting it in its Update
        /// until it did, and blends the upper body into the pose. Without it the creature stays in the default idle.
        /// It doesn't pick random poses, so it doesn't fight the synced one.
        /// </summary>
        internal static void UpdateStance(this Creature creature) {
            if(!creature.initialized || creature.animator == null) return;
            if(creature.brain != null && creature.brain.instance != null && creature.brain.instance.isActive) return; // The brain does it itself

            creature.GetStanceModule()?.Update();
        }

        internal static void ApplyStance(this Creature creature, string stanceId, string idlePoseId) {
            BrainModuleStance module = creature.GetStanceModule();
            if(module == null) return;

            if(!string.IsNullOrEmpty(stanceId) && (module.currentStanceData == null || module.currentStanceData.id != stanceId)) {
                module.SetStance(stanceId);
            }
            if(!string.IsNullOrEmpty(idlePoseId) && (module.currentPose == null || module.currentPose.id != idlePoseId)) {
                module.ChangeIdlePose(idlePoseId, instant: true, ignoreDelay: true);
            }
        }

        /// <summary>
        /// Moves the ragdoll parts towards the synced pose by setting their velocity, so they stay physical (react to hits, push and get pushed)
        /// and get pulled back into the pose afterwards. Needs to be called from FixedUpdate.
        /// </summary>
        /// <param name="positions">Part positions relative to the creature root, same order as ReadRagdoll</param>
        /// <param name="rootVelocity">Velocity the creature root is moving with, so the parts move along with it</param>
        /// <returns>Average distance in meters between the parts and the pose, before moving them</returns>
        internal static float DriveRagdoll(this Creature creature, Vector3[] positions, Quaternion[] rotations, Vector3 rootVelocity) {
            float responsiveness = Config.RAGDOLL_PHYSICS_RESPONSIVENESS;
            float snapDistanceSqr = Config.RAGDOLL_PHYSICS_SNAP_DISTANCE * Config.RAGDOLL_PHYSICS_SNAP_DISTANCE;
            Vector3 origin = creature.transform.position;

            float totalError = 0f;
            int i = 0;
            foreach(RagdollPart part in creature.ragdoll.parts) {
                if(part == creature.ragdoll.rootPart) continue; // Not part of the synced pose, follows through its joints
                if(i >= positions.Length || i >= rotations.Length) break; // Prevent errors when the supplied vectors dont match the creatures

                PhysicBody body = part.physicBody;
                Vector3 targetPos = origin + positions[i];
                Vector3 offset = targetPos - part.transform.position;
                totalError += offset.magnitude;

                if(offset.sqrMagnitude > snapDistanceSqr) {
                    // Too far off, pulling it over would just stretch the ragdoll
                    part.transform.SetPositionAndRotation(targetPos, rotations[i]);
                    body.velocity = rootVelocity;
                    body.angularVelocity = Vector3.zero;
                    i++;
                    continue;
                }

                body.velocity = Vector3.ClampMagnitude(rootVelocity + offset * responsiveness, Config.RAGDOLL_PHYSICS_MAX_SPEED);

                Quaternion delta = rotations[i] * Quaternion.Inverse(part.transform.rotation);
                float angle;
                Vector3 axis;
                delta.ToAngleAxis(out angle, out axis);
                if(angle > 180f) angle -= 360f;
                if(Mathf.Abs(angle) > 0.1f && !float.IsInfinity(axis.x) && !float.IsNaN(axis.x)) {
                    body.angularVelocity = axis * (angle * Mathf.Deg2Rad * responsiveness);
                } else {
                    body.angularVelocity = Vector3.zero;
                }
                i++;
            }

            return i > 0 ? totalError / i : 0f;
        }

        /// <summary>
        /// Places the ragdoll parts between a captured pose (from ReadRagdoll) and the current animation pose.
        /// Used to blend over from the synced ragdoll to the creatures own animation, instead of snapping to it.
        /// </summary>
        /// <param name="t">0 = captured pose, 1 = animation</param>
        internal static void BlendRagdollToAnimation(this Creature creature, Vector3[] fromPositions, Quaternion[] fromRotations, float t) {
            t = Mathf.SmoothStep(0f, 1f, t);
            Vector3 origin = creature.transform.position;

            int i = 0;
            foreach(RagdollPart part in creature.ragdoll.parts) {
                if(part == creature.ragdoll.rootPart) continue;
                if(i >= fromPositions.Length || i >= fromRotations.Length) break;

                Transform animation = part.bone != null ? part.bone.animation : null;
                if(animation != null) {
                    // The captured positions are relative to the root, so the pose moves along with the creature instead of being left behind
                    Vector3 from = origin + fromPositions[i];
                    part.transform.SetPositionAndRotation(Vector3.Lerp(from, animation.position, t), Quaternion.Slerp(fromRotations[i], animation.rotation, t));
                }
                i++;
            }
        }

        internal static bool IsRagdolled(this Creature creature) {
            return (ModManager.safeFile.modSettings.useAdvancedNpcSyncing && creature.ragdoll.state != Ragdoll.State.NoPhysic)
                || creature.isKilled
                || (creature.spawnTime + 2 > Time.time && creature.ragdoll != null && creature.ragdoll.state == Ragdoll.State.Inert)
                ;
        }

        internal static void SetSelfCollision(this Creature creature, bool allowCollision) {
            Collider[] colliders = creature.GetComponentsInChildren<Collider>(includeInactive: true);
            for(int i = 0; i < colliders.Length; i++) {
                for(int j = i + 1; j < colliders.Length; j++) {
                    if(colliders[i] == colliders[j])
                        continue;
                    Physics.IgnoreCollision(colliders[i], colliders[j], ignore: !allowCollision);
                }
            }
        }
    }
}
