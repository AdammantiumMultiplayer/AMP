using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using AMP.SupportFunctions;
using System;
using ThunderRoad;
using UnityEngine;

namespace AMP.GameInteraction {
    internal class Spawner {

        #region Player
        internal static void TrySpawnPlayer(PlayerNetworkData pnd) {
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

                    creature.factionId = 2; // Should be the Player Layer so wont get ignored by the ai anymore

                    NetworkPlayerCreature networkPlayerCreature = pnd.StartNetworking();

                    if(!Config.FULL_BODY_SYNCING) {
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

                    if(GameConfig.showPlayerNames) {
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

                    if(GameConfig.showPlayerHealthBars) {
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

                    if(pnd.equipment.Length > 0) {
                        PlayerEquipment.Update(pnd);
                    }

                    creature.SetHeight(pnd.height);

                    Creature.all.Remove(creature);
                    Creature.allActive.Remove(creature);

                    pnd.isSpawning = false;

                    Log.Debug(Defines.CLIENT, $"Spawned Character for Player " + pnd.clientId + " (" + pnd.creatureId + ")");
                }));

            }
        }
        #endregion

        #region NPCs
        internal static void TrySpawnCreature(CreatureNetworkData creatureSync) {
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

                ModManager.clientSync.StartCoroutine(creatureData.SpawnCoroutine(position, rotationY, ModManager.instance.transform, pooled: false, result: (creature) => {
                    creatureSync.creature = creature;

                    creature.factionId = creatureSync.factionId;

                    creature.maxHealth = creatureSync.maxHealth;
                    creature.currentHealth = creatureSync.maxHealth;

                    creature.ApplyWardrobe(creatureSync.equipment);

                    creature.SetHeight(creatureSync.height);

                    creature.transform.position = creatureSync.position;

                    creatureSync.StartNetworking();

                    Creature.all.Remove(creature);
                    Creature.allActive.Remove(creature);

                    creatureSync.isSpawning = false;
                }));
            } else {
                Log.Err(Defines.CLIENT, $"Couldn't spawn {creatureSync.creatureType}. #SNHE003");
            }
        }
        #endregion

        #region Items
        internal static void TrySpawnItem(ItemNetworkData itemNetworkData) {
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
                itemData.SpawnAsync((item) => {
                    if(item == null) return;
                    //if(ModManager.clientSync.syncData.items.ContainsKey(itemSync.networkedId) && ModManager.clientSync.syncData.items[itemSync.networkedId].clientsideItem != item) {
                    //    item.Despawn();
                    //    return;
                    //}

                    itemNetworkData.clientsideItem = item;

                    item.disallowDespawn = true;

                    Log.Debug(Defines.CLIENT, $"Item {itemNetworkData.dataId} ({itemNetworkData.networkedId}) spawned from server.");

                    itemNetworkData.StartNetworking();

                    if(itemNetworkData.creatureNetworkId > 0) {
                        itemNetworkData.UpdateHoldState();
                    }

                    itemNetworkData.isSpawning = false;
                }, itemNetworkData.position, Quaternion.Euler(itemNetworkData.rotation));
            } else {
                Log.Err(Defines.CLIENT, $"Couldn't spawn {itemNetworkData.dataId}. #SNHE002");
            }
        }
        #endregion
    
    }
}
