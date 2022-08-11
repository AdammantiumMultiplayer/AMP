using AMP.Data;
using AMP.Logging;
using AMP.Network.Helper;
using Chabuk.ManikinMono;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AMP.Extension {
    public static class CreatureExtension {

        public static bool IsOtherPlayer(this Creature creature) {
            bool isOtherPlayer;
            long networkId;
            SyncFunc.GetCreature(creature, out isOtherPlayer, out networkId);

            if(isOtherPlayer && networkId == ModManager.clientInstance.myClientId) isOtherPlayer = false;

            return isOtherPlayer;
        }

        public static List<string> ReadWardrobe(this Creature creature) {
            List<string> equipment_list = new List<string>();

            foreach(ContainerData.Content content in creature.container.contents) {
                if(content.itemData.type == ItemData.Type.Wardrobe) {
                    equipment_list.Add(content.referenceID);
                }
            }

            return equipment_list;
        }

        public static void ApplyWardrobe(this Creature creature, List<string> equipment_list) {
            // TODO: Figure out why the wardrobe is not applied correctly while changing the level to join the server

            bool changed = false;

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
                        // TODO: Maybe some default parts? At least for chest, pants and shoes
                        Log.Err($"[Client] Equipment {referenceID} for {creature.creatureId} not found, please check you mods.");
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

        public static string GetAttackAnimation(this Creature creature) {
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
        public static void PlayAttackAnimation(this Creature creature, string clipName) {
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
                Log.Debug("[Client] AnimationClips populated " + animationClips.Count + "\n" + string.Join("\n", animationClips.Keys));
            }
            
            // Check if the animation clip is inside the cache
            clipName = clipName.ToLower();
            if(!animationClips.ContainsKey(clipName)) {
                Log.Err($"Attack animation { clipName } not found, please check you mods.");
                return;
            }
            
            // Play the animation
            creature.PlayAnimation(animationClips[clipName], false);
            //creature.UpdateOverrideClip(new KeyValuePair<int, AnimationClip>(0, animationClips[clipName]));
        }
    }
}
