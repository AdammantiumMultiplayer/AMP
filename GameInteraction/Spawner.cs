using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using AMP.SupportFunctions;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.GameInteraction {
    internal class Spawner {

        #region Player
        internal static void TrySpawnPlayer(PlayerNetworkData pnd) {
            if(LevelInfo.IsLoading()) return;
            if(pnd.creature != null || pnd.isSpawning) return;

            CreatureData creatureData = Catalog.GetData<CreatureData>(pnd.creatureId);
            if(creatureData == null) { // If the client doesnt have the creature, just spawn a HumanMale or HumanFemale (happens when mod is not installed)
                string creatureId = new System.Random().Next(0, 2) == 1 ? "HumanMale" : "HumanFemale";

                Log.Err(Defines.CLIENT, $"Couldn't find playermodel for {pnd.name} ({creatureData.id}), please check you mods. Instead {creatureId} is used now.");
                creatureData = Catalog.GetData<CreatureData>(creatureId);
            }
            if(creatureData != null) {
                pnd.isSpawning = true;
                Vector3 position = pnd.position;
                float rotationY = pnd.rotationY;

                creatureData.containerID = "Empty";

                ModManager.clientSync.StartCoroutine(creatureData.SpawnCoroutine(position, rotationY, ModManager.instance.transform, pooled: false, result: (creature) => {
                    pnd.creature = creature;

                    creature.SetFaction(2); // Should be the Player Layer so wont get ignored by the ai anymore

                    NetworkPlayerCreature networkPlayerCreature = pnd.StartNetworking();

                    if(!Config.PLAYER_FULL_BODY_SYNCING) {
                        IKControllerFIK ik = creature.GetComponentInChildren<IKControllerFIK>();

                        try {
                            Transform handLeftTarget = new GameObject("HandLeftTarget" + pnd.clientId).transform;
                            handLeftTarget.parent = creature.transform;
                            #if DEBUG_INFO
                            TextMesh tm = handLeftTarget.gameObject.AddComponent<TextMesh>();
                            tm.text = "L";
                            tm.alignment = TextAlignment.Center;
                            tm.anchor = TextAnchor.MiddleCenter;
                            #endif
                            networkPlayerCreature.handLeftTarget = handLeftTarget;
                            ik.SetHandAnchor(Side.Left, handLeftTarget);
                        } catch(Exception) { Log.Err($"[Err] {pnd.clientId} ik target for left hand failed."); }

                        try {
                            Transform handRightTarget = new GameObject("HandRightTarget" + pnd.clientId).transform;
                            handRightTarget.parent = creature.transform;
                            #if DEBUG_INFO
                            TextMesh tm = handRightTarget.gameObject.AddComponent<TextMesh>();
                            tm.text = "R";
                            tm.alignment = TextAlignment.Center;
                            tm.anchor = TextAnchor.MiddleCenter;
                            #endif
                            networkPlayerCreature.handRightTarget = handRightTarget;
                            ik.SetHandAnchor(Side.Right, handRightTarget);
                        } catch(Exception) { Log.Err($"[Err] {pnd.clientId} ik target for right hand failed."); }

                        try {
                            Transform headTarget = new GameObject("HeadTarget" + pnd.clientId).transform;
                            headTarget.parent = creature.transform;
                            #if DEBUG_INFO
                            TextMesh tm = headTarget.gameObject.AddComponent<TextMesh>();
                            tm.text = "H";
                            tm.alignment = TextAlignment.Center;
                            tm.anchor = TextAnchor.MiddleCenter;
                            #endif
                            networkPlayerCreature.headTarget = headTarget;
                            ik.SetLookAtTarget(headTarget);
                        } catch(Exception) { Log.Err($"[Err] {pnd.clientId} ik target for head failed."); }

                        ik.handLeftEnabled = true;
                        ik.handRightEnabled = true;
                    }

                    if(ModManager.safeFile.modSettings.showPlayerNames) {
                        Transform playerNameTag = new GameObject("PlayerNameTag" + pnd.clientId).transform;
                        playerNameTag.parent = creature.transform;
                        playerNameTag.transform.localPosition = new Vector3(0, 2.5f, 0);
                        playerNameTag.transform.localEulerAngles = new Vector3(0, 180, 0);
                        TextMesh textMesh = playerNameTag.gameObject.AddComponent<TextMesh>();
                        textMesh.text = pnd.name;
                        textMesh.alignment = TextAlignment.Center;
                        textMesh.anchor = TextAnchor.MiddleCenter;
                        textMesh.fontSize = 500;
                        textMesh.characterSize = 0.0025f;
                    }

                    if(ModManager.safeFile.modSettings.showPlayerHealthBars) {
                        Transform playerHealthBar = new GameObject("PlayerHealthBar" + pnd.clientId).transform;
                        playerHealthBar.parent = creature.transform;
                        playerHealthBar.transform.localPosition = new Vector3(0, 2.375f, 0);
                        playerHealthBar.transform.localEulerAngles = new Vector3(0, 180, 0);
                        TextMesh textMesh = playerHealthBar.gameObject.AddComponent<TextMesh>();
                        textMesh.text = HealthBar.calculateHealthBar(1f);
                        textMesh.alignment = TextAlignment.Center;
                        textMesh.anchor = TextAnchor.MiddleCenter;
                        textMesh.fontSize = 500;
                        textMesh.characterSize = 0.0003f;
                        networkPlayerCreature.healthBar = textMesh;
                    }

                    creature.gameObject.name = pnd.name;

                    creature.maxHealth = 1000;
                    creature.currentHealth = creature.maxHealth;

                    creature.isPlayer = false;

                    foreach(RagdollPart ragdollPart in creature.ragdoll.parts) {
                        foreach(HandleRagdoll hr in ragdollPart.handles) { UnityEngine.Object.Destroy(hr.gameObject); }// hr.enabled = false;
                        ragdollPart.handles.Clear();
                        ragdollPart.sliceAllowed = false;
                        ragdollPart.DisableCharJointLimit();
                    }

                    if(pnd.equipment != null && pnd.equipment.Length > 0) {
                        CreatureEquipment.Apply(pnd);
                    }

                    creature.SetHeight(pnd.height);
                    creature.SetSelfCollision(false);

                    // Need to do that, otherwise players are seen as still alive, so waves dont spawn new enemies
                    //Creature.all.Remove(creature);
                    //Creature.allActive.Remove(creature);
                    creature.countTowardsMaxAlive = false;

                    creature.lastInteractionTime = Time.time;
                    pnd.isSpawning = false;

                    Log.Debug(Defines.CLIENT, $"Spawned Character for Player {pnd.name} ({pnd.clientId} / {pnd.creatureId})");
                }));

            }
        }
        #endregion

        #region NPCs
        internal static void TrySpawnCreature(CreatureNetworkData creatureSync) {
            if(!ModManager.clientInstance.allowTransmission) return;
            if(LevelInfo.IsLoading()) return;
            if(creatureSync.creature != null) return;
            if(creatureSync.isSpawning) return;

            creatureSync.isSpawning = true;
            CreatureData creatureData = Catalog.GetData<CreatureData>(creatureSync.creatureType);
            if(creatureData == null) { // If the client doesnt have the creature, just spawn a HumanMale or HumanFemale (happens when mod is not installed)
                string creatureId = new System.Random().Next(0, 2) == 1 ? "HumanMale" : "HumanFemale";

                Log.Err(Defines.CLIENT, $"Couldn't spawn enemy {creatureData.id}, please check you mods. Instead {creatureId} is used now.");
                creatureData = Catalog.GetData<CreatureData>(creatureId);
            }

            if(creatureData != null) {
                Vector3 position = creatureSync.position;
                float rotationY = creatureSync.rotationY;

                creatureData.containerID = "Empty";

                ModManager.clientSync.StartCoroutine(creatureData.InstantiateCoroutine(position, rotationY, ModManager.instance.transform, result: (creature) => {
                    creature.LoadCoroutine(creatureData);

                //ModManager.clientSync.StartCoroutine(creatureData.SpawnCoroutine(position, rotationY, ModManager.instance.transform, pooled: false, result: (creature) => { // Not used anymore as this will call the on spawn event
                    creatureSync.creature = creature;

                    creature.SetFaction(creatureSync.factionId);

                    creature.maxHealth = creatureSync.maxHealth;
                    creature.currentHealth = creatureSync.maxHealth;

                    CreatureEquipment.Apply(creature, creatureSync.colors, creatureSync.equipment);

                    creature.SetHeight(creatureSync.height);

                    creature.transform.position = creatureSync.position;

                    creatureSync.StartNetworking();

                    // Need to do that, otherwise other players creatures are seen as still alive, so waves dont spawn new enemies
                    //Creature.all.Remove(creature);
                    //Creature.allActive.Remove(creature);
                    creature.countTowardsMaxAlive = false;

                    creature.lastInteractionTime = Time.time;
                    creatureSync.isSpawning = false;
                }));
            } else {
                Log.Err(Defines.CLIENT, $"Couldn't spawn {creatureSync.creatureType}. #SNHE003");
            }
        }
        #endregion

        #region Items
        internal static void TrySpawnItem(ItemNetworkData itemNetworkData) {
            if(!ModManager.clientInstance.allowTransmission) return;
            if(LevelInfo.IsLoading()) return;
            if(itemNetworkData.clientsideItem != null) return;
            if(itemNetworkData.isSpawning) return;

            itemNetworkData.isSpawning = true;
            ItemData itemData = Catalog.GetData<ItemData>(itemNetworkData.dataId);

            if(itemData == null) { // If the client doesnt have the item, just spawn a sword (happens when mod is not installed)
                string replacement = (string)Config.itemCategoryReplacement[Config.itemCategoryReplacement.GetLength(0) - 1, 1];

                for(int i = 0; i < Config.itemCategoryReplacement.GetLength(0); i++) {
                    if(itemNetworkData.category == (ItemData.Type)Config.itemCategoryReplacement[i, 0]) {
                        replacement = (string)Config.itemCategoryReplacement[i, 1];
                        break;
                    }
                }

                Log.Err(Defines.CLIENT, $"Couldn't spawn {itemNetworkData.dataId}, please check you mods. Instead a {replacement} is used now.");
                itemData = Catalog.GetData<ItemData>(replacement);
            }

            if(itemData != null) {
                if(ModManager.clientSync.unfoundItemMode == ClientSync.UnfoundItemMode.DESPAWN) {
                    List<Item> unsynced_items = Item.allActive.Where(item => ModManager.clientSync.syncData.items.All(entry => !item.Equals(entry.Value.clientsideItem))).ToList();
                    foreach(Item item in unsynced_items) {
                        if(item.GetComponent<NetworkItem>() != null) continue;
                        if(item.transform.position.DistanceSqr(itemNetworkData.position) < 5f * 5f) {
                            item.Despawn();
                        }
                    }
                }


                itemData.SpawnAsync((item) => {
                    if(item == null) return;
                    //if(ModManager.clientSync.syncData.items.ContainsKey(itemSync.networkedId) && ModManager.clientSync.syncData.items[itemSync.networkedId].clientsideItem != item) {
                    //    item.Despawn();
                    //    return;
                    //}

                    itemNetworkData.clientsideItem = item;

                    //item.disallowDespawn = true;

                    Log.Debug(Defines.CLIENT, $"Item {itemNetworkData.dataId} ({itemNetworkData.networkedId}) spawned from server.");

                    itemNetworkData.StartNetworking();

                    if(itemNetworkData.holderNetworkId > 0) {
                        itemNetworkData.UpdateHoldState();
                    }

                    item.lastInteractionTime = Time.time;
                    itemNetworkData.isSpawning = false;
                }, itemNetworkData.position, Quaternion.Euler(itemNetworkData.rotation));
            } else {
                Log.Err(Defines.CLIENT, $"Couldn't spawn {itemNetworkData.dataId}. #SNHE002");
            }
        }
        #endregion
    
    }
}
