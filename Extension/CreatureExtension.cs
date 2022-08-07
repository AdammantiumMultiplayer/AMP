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

            Equipment equipment = creature.equipment;

            // Loop through all equipment layers and put it in a List as a ; seperated string (Hopefully, nobody uses ; as itemId :P)
            for(int i = 0; i < equipment.wearableSlots.Count; i++) {
                for(int j = equipment.wearableSlots[i].wardrobeLayers.Length - 1; j >= 0; j--) {
                    if(equipment.wearableSlots[i].IsEmpty()) {
                        continue;
                    }

                    ContainerData.Content equipmentOnLayer = equipment.wearableSlots[i].GetEquipmentOnLayer(equipment.wearableSlots[i].wardrobeLayers[j].layer);
                    if(equipmentOnLayer == null) {
                        continue;
                    }

                    ItemModuleWardrobe module = equipmentOnLayer.itemData.GetModule<ItemModuleWardrobe>();
                    if(module == null || equipment.wearableSlots[i].IsEmpty()) {
                        continue;
                    }

                    string str = equipment.wearableSlots[i].wardrobeChannel + ";" + equipment.wearableSlots[i].wardrobeLayers[j].layer + ";" + module.itemData.id;
                    if(!equipment_list.Contains(str)) equipment_list.Add(str);
                    break;
                }
            }

            return equipment_list;
        }

        private static Dictionary<Creature, int> equipmentWaiting = new Dictionary<Creature, int>();
        public static void ApplyWardrobe(this Creature creature, List<string> equipment_list) {
            // TODO: Figure out why the wardrobe is not applied correctly while changing the level to join the server

            if(equipmentWaiting.ContainsKey(creature)) {
                if(equipmentWaiting[creature] > 0) return;
            } else {
                equipmentWaiting.Add(creature, 0);
            }

            List<string> to_fill = equipment_list.ToArray().ToList();
            Equipment equipment = creature.equipment;
            for(int i = 0; i < equipment.wearableSlots.Count; i++) {
                bool already_done = false;
                for(int j = 0; j < equipment.wearableSlots[i].wardrobeLayers.Length; j++) {
                    if(already_done) continue;

                    do {
                        if(equipment.wearableSlots[i].IsEmpty()) {
                            continue;
                        }

                        ContainerData.Content equipmentOnLayer = equipment.wearableSlots[i].GetEquipmentOnLayer(equipment.wearableSlots[i].wardrobeLayers[j].layer);
                        if(equipmentOnLayer == null) {
                            continue;
                        }

                        ItemModuleWardrobe module = equipmentOnLayer.itemData.GetModule<ItemModuleWardrobe>();
                        if(module == null || equipment.wearableSlots[i].IsEmpty()) {
                            continue;
                        }

                        if(equipment_list.Contains(equipment.wearableSlots[i].wardrobeChannel + ";" + equipment.wearableSlots[i].wardrobeLayers[j].layer + ";" + module.itemData.id)) {
                            already_done = true; // Item is already equiped
                        }
                    } while(false);

                    if(already_done) continue;

                    // Unequip item
                    if(!equipment.wearableSlots[i].IsEmpty()) {
                        try {
                            equipment.wearableSlots[i].UnEquip(equipment.wearableSlots[i].wardrobeLayers[j].layer, (item) => { item.Despawn(); });
                        } catch {
                            // Sometimes some sound bugs happen and stop the code :(
                        }
                    }

                    // Check if a item is in the slot otherwise leave it empty
                    foreach(string line in equipment_list) {
                        Debug.Log(line);
                        if(!to_fill.Contains(line)) continue;
                        if(line.StartsWith(equipment.wearableSlots[i].wardrobeChannel + ";" + equipment.wearableSlots[i].wardrobeLayers[j].layer + ";")) {
                            string itemId = line.Split(';')[2];

                            if(equipment.wearableSlots[i].IsEmpty()) {
                                ItemData itemData = Catalog.GetData<ItemData>(itemId);
                                if(itemData == null) {
                                    // TODO: Maybe some default parts? At least for chest, pants and shoes
                                    Log.Err($"[Client] Equipment {itemId} for {creature.creatureId} not found, please check you mods.");
                                }
                                if(itemData != null) {
                                    Wearable wearable = equipment.wearableSlots[i];
                                    if(wearable != null) {
                                        equipmentWaiting[creature]++;
                                        int ic = i;
                                        int jc = j;
                                        itemData.SpawnAsync((item) => {
                                            equipmentWaiting[wearable.Creature]--;

                                            if(!wearable.IsEmpty()) {
                                                wearable.UnEquip(equipment.wearableSlots[ic].wardrobeLayers[jc].layer, (uitem) => { uitem.Despawn(); });
                                            }

                                            wearable.EquipItem(item);
                                        });
                                    }
                                }
                            }
                            to_fill.Remove(line);
                            break;
                        }
                    }
                }
            }
        }

        public static List<string> ReadDetails(this Creature creature) {
            List<string> ids = new List<string>();
            foreach(int layer in Config.headDetailLayers) {
                ManikinWardrobeData mwd = creature.manikinLocations.GetWardrobeData("Head", layer);
                if(mwd != null) ids.Add(mwd.assetPrefab.AssetGUID);
            }
            return ids;
        }

        public static void ApplyDetails(this Creature creature, List<string> ids) {
            for(int i = 0; i < ids.Count; i++) {

                ManikinWardrobeData mwd = ScriptableObject.CreateInstance<ManikinWardrobeData>();
                mwd.assetPrefab = new AssetReferenceManikinPart(ids[i]);
                mwd.channels = new string[] { "Head" };
                mwd.layers = new int[] { Config.headDetailLayers[i] };

                mwd.partialOccludedLayers = mwd.fullyOccludedLayers = new int[] { 0 };

                creature.manikinLocations.AddPart(mwd);
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
