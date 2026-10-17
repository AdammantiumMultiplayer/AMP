using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        internal static string GetAttackAnimation(this Creature creature) {
            // Use Reflection to read the current animationClipOverrides
            Type typecontroller = typeof(Creature);
            FieldInfo finfo = typecontroller.GetField("animationClipOverrides", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

            KeyValuePair<AnimationClip, AnimationClip>[] animationClipOverrides;
            if (finfo != null) {
                animationClipOverrides = (KeyValuePair<AnimationClip, AnimationClip>[])finfo.GetValue(creature);

                foreach (KeyValuePair<AnimationClip, AnimationClip> kvp in animationClipOverrides) {
                    if (GetAnimation(kvp.Value.name) != null) {
                        return kvp.Value.name; // Return the first value, because it should be the attack animation (Have to check that sometime, BowtAI told me there are 2)
                    } else {
                        //Log.Err(kvp.Value.name + " " + kvp.Key.name);
                    }
                }
            }
            return "";
        }

        private static Dictionary<string, AnimationClip> animationClips = new Dictionary<string, AnimationClip>();
        internal static AnimationClip GetAnimation(string clipName) {
            // Cache all animations from the AnimationData
            if (animationClips.Count == 0) {
                List<AnimationData> data = Catalog.GetDataList<AnimationData>();
                foreach (AnimationData ad in data) {
                    foreach (AnimationData.Clip adc in ad.animationClips) {
                        string name = adc.animationClip.name.ToLower();
                        if (!animationClips.ContainsKey(name)) {
                            animationClips.Add(name, adc.animationClip);
                        }
                    }
                }

                // TODO: Better way than to do this
                //AnimationClip[] d = Resources.FindObjectsOfTypeAll<AnimationClip>();
                //foreach(AnimationClip ac in d) {
                //    string name = ac.name.ToLower();
                //    if(!animationClips.ContainsKey(name)) {
                //        animationClips.Add(name, ac);
                //    }
                //}

                //Log.Debug(Defines.CLIENT, "AnimationClips populated " + animationClips.Count + "\n" + string.Join("\n", animationClips.Keys));
            }


            string lClipName = clipName.ToLower();
            if (animationClips.ContainsKey(lClipName)) {
                return animationClips[lClipName];
            }
            return null;
        }

        internal static void PlayAttackAnimation(this Creature creature, string clipName) {
            // Check if the animation clip is inside the cache
            AnimationClip clip = GetAnimation(clipName);
            if (clip == null) {
                Log.Err(Defines.CLIENT, $"Attack animation { clipName } not found, please check you mods.");
                return;
            }

            // Play the animation
            creature.PlayAnimation(clip, false);
            //creature.UpdateOverrideClip(new KeyValuePair<int, AnimationClip>(0, animationClips[clipName]));
        }

        /// <param name="readMeshBones">Read the visible mesh bones instead of the physics parts. Depending on the ragdoll state the game attaches the mesh
        /// to the animation bones, then the physics parts only follow through their joints and lag behind (an attack swing with a weapon doesn't
        /// complete), while the owner sees the animation. The mesh bones are always what is actually shown.</param>
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
