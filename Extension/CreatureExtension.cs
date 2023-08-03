using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Reflection;
using ThunderRoad;
using UnityEngine;

namespace AMP.Extension {
    internal static class CreatureExtension {

        internal static bool IsOtherPlayer(this Creature creature) {
            ItemHolderType itemHolder;
            long networkId;
            SyncFunc.GetCreature(creature, out itemHolder, out networkId);

            if(itemHolder == ItemHolderType.PLAYER && networkId == ModManager.clientInstance.netclient.ClientId) itemHolder = ItemHolderType.NONE;

            return itemHolder == ItemHolderType.PLAYER;
        }

        internal static string[] ReadWardrobe(this Creature creature) {
            List<string> equipment_list = new List<string>();

            if(creature.container != null && creature.container.contents != null) {
                foreach(ContainerData.Content content in creature.container.contents) {
                    if(content.itemData.type == ItemData.Type.Wardrobe) {
                        equipment_list.Add(content.referenceID);
                    }
                }
            }

            return equipment_list.ToArray();
        }

        internal static void ApplyWardrobe(this Creature creature, string[] equipment_list) {
            bool changed = false;

            if(creature.container == null || creature.container.contents == null) return;

            foreach(string referenceID in equipment_list) {
                bool found = false;
                foreach(ContainerData.Content content in creature.container.contents) {
                    if(content.itemData.type != ItemData.Type.Wardrobe) continue;
                    if(content.referenceID.Equals(referenceID)) {
                        found = true;
                        break;
                    }
                }
                if(!found) {
                    ItemData itemData = Catalog.GetData<ItemData>(referenceID);
                    if(itemData == null) {
                        Log.Err(Defines.CLIENT, $"Equipment { referenceID } for { creature.creatureId } not found, please check you mods.");
                    }
                    if(itemData != null && itemData.type == ItemData.Type.Wardrobe) {
                        ContainerData.Content content = new ContainerData.Content(itemData);
                        creature.equipment.EquipWardrobe(content, false);
                        changed = true;
                    }
                }
            }
            if(changed) {
                creature.equipment.UpdateParts();
            }
        }

        internal static Color[] ReadColors(this Creature creature) {
            List<Color> color_list = new List<Color>();

            color_list.Add(creature.GetColor(Creature.ColorModifier.Hair));
            color_list.Add(creature.GetColor(Creature.ColorModifier.HairSecondary));
            color_list.Add(creature.GetColor(Creature.ColorModifier.HairSpecular));
            color_list.Add(creature.GetColor(Creature.ColorModifier.EyesIris));
            color_list.Add(creature.GetColor(Creature.ColorModifier.EyesSclera));
            color_list.Add(creature.GetColor(Creature.ColorModifier.Skin));

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
            if(finfo != null) {
                animationClipOverrides = (KeyValuePair<AnimationClip, AnimationClip>[])finfo.GetValue(creature);

                foreach(KeyValuePair<AnimationClip, AnimationClip> kvp in animationClipOverrides) {
                    return kvp.Value.name; // Return the first value, because it should be the attack animation (Have to check that sometime, BowtAI told me there are 2)
                }
            }
            return "";
        }

        private static Dictionary<string, AnimationClip> animationClips = new Dictionary<string, AnimationClip>();
        internal static void PlayAttackAnimation(this Creature creature, string clipName) {
            // Cache all animations from the AnimationData
            if(animationClips.Count == 0) {
                List<AnimationData> data = Catalog.GetDataList<AnimationData>();
                foreach(AnimationData ad in data) {
                    foreach(AnimationData.Clip adc in ad.animationClips) {
                        string name = adc.animationClip.name.ToLower();
                        if(!animationClips.ContainsKey(name)) {
                            animationClips.Add(name, adc.animationClip);
                        }
                    }
                }
                Log.Debug(Defines.CLIENT, "AnimationClips populated " + animationClips.Count + "\n" + string.Join("\n", animationClips.Keys));
            }
            
            // Check if the animation clip is inside the cache
            clipName = clipName.ToLower();
            if(!animationClips.ContainsKey(clipName)) {
                Log.Err(Defines.CLIENT, $"Attack animation { clipName } not found, please check you mods.");
                return;
            }
            
            // Play the animation
            creature.PlayAnimation(animationClips[clipName], false);
            //creature.UpdateOverrideClip(new KeyValuePair<int, AnimationClip>(0, animationClips[clipName]));
        }

        internal static void ReadRagdoll(this Creature creature, out Vector3[] positions, out Quaternion[] rotations, out Vector3[] velocity, out Vector3[] angularVelocity) {
            List<Vector3> vec3s = new List<Vector3>();
            List<Quaternion> quats = new List<Quaternion>();
            List<Vector3> vels = new List<Vector3>();
            List<Vector3> aVels = new List<Vector3>();

            foreach(RagdollPart part in creature.ragdoll.parts) {
                if(part == creature.ragdoll.rootPart) continue;
                vec3s.Add(part.transform.position - creature.transform.position);
                quats.Add(part.transform.rotation);
                vels .Add(part.physicBody.velocity);
                aVels.Add(part.physicBody.angularVelocity);
            }
            positions       = vec3s.ToArray();
            rotations       = quats.ToArray();
            velocity        = vels.ToArray();
            angularVelocity = aVels.ToArray();
        }

        internal static void ApplyRagdoll(this Creature creature, Vector3[] positions, Quaternion[] rotations) {
            int i = positions.Length - 1;
            for(int j = creature.ragdoll.parts.Count - 1; j >= 0; j--) { 
                RagdollPart part = creature.ragdoll.parts[j];

                if(part == creature.ragdoll.rootPart) continue;
                //foreach(Ragdoll.Bone bone in creature.ragdoll.bones) {
                if(positions.Length <= i) continue; // Prevent errors when the supplied vectors dont match the creatures
                if(rotations.Length <= i) continue; // Prevent errors when the supplied rotations dont match the creatures
                if(i < 0) continue;

                part.meshBone.position  = positions[i];
                part.bone.mesh.position = positions[i];
                part.transform.position = positions[i];

                part.meshBone.rotation  = rotations[i];
                part.bone.mesh.rotation = rotations[i];
                part.transform.rotation = rotations[i];

                i--;
            }
            creature.ragdoll.SavePartsPosition();
        }

        internal static void SmoothDampRagdoll(this Creature creature, Vector3[] positions, Quaternion[] rotations, ref Vector3[] positionVelocity, ref float[] rotationVelocity, float smoothTime = Config.MOVEMENT_DELTA_TIME) {
            Vector3[] new_vectors = new Vector3[positions.Length];
            Quaternion[] new_rots = new Quaternion[rotations.Length];
            int i = 0;
            foreach(RagdollPart part in creature.ragdoll.parts) {
                if(part == creature.ragdoll.rootPart) continue;
                if(positions.Length <= i) continue; // Prevent errors when the supplied vectors dont match the creatures

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
