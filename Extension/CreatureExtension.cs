using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace AMP.Extension {
    public static class CreatureExtension {

        public static bool IsOtherPlayer(this Creature creature) {
            bool isOtherPlayer;
            int networkId;
            SyncFunc.GetCreature(creature, out isOtherPlayer, out networkId);

            if(isOtherPlayer && networkId == ModManager.clientInstance.myClientId) isOtherPlayer = false;

            return isOtherPlayer;
        }

        public static List<string> ReadWardrobe(this Creature creature) {
            List<string> equipment_list = new List<string>();

            Equipment equipment = creature.equipment;

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
                                        itemData.SpawnAsync((item) => {
                                            equipmentWaiting[wearable.Creature]--;

                                            if(!wearable.IsEmpty()) {
                                                wearable.UnEquip(equipment.wearableSlots[i].wardrobeLayers[j].layer, (uitem) => { uitem.Despawn(); });
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
    }
}
