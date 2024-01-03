using AMP.Data;
using AMP.Datatypes;
using AMP.Extension;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Helper {
    internal class SyncFunc {

        /// <summary>
        /// Check if the same item is already there, so we don't spawn it twice
        /// </summary>
        /// <param name="new_item">Item to check</param>
        /// <param name="items">List of currently know items</param>
        /// <returns>ID of the found item</returns>
        internal static ItemNetworkData DoesItemAlreadyExist(ItemNetworkData new_item, List<ItemNetworkData> items) {
            float dist = getCloneDistance(new_item.dataId);

            ItemNetworkData found_item = null;
            float distance = float.MaxValue;
            foreach(ItemNetworkData item in items) {
                if(item.dataId.Equals(new_item.dataId)) {
                    if(item.position.CloserThan(new_item.position, dist)) {
                        float this_distance = item.position.SqDist(new_item.position);
                        if(this_distance < distance) {
                            distance = this_distance;
                            found_item = item;
                        }
                    }
                }
            }

            return found_item;
        }

        /// <summary>
        /// Check if a creature is already close to it and still alive
        /// </summary>
        /// <param name="new_creature">Creature to check</param>
        /// <param name="creatures">List of currently know creatures</param>
        /// <returns>ID of the found creature</returns>
        internal static CreatureNetworkData DoesCreatureAlreadyExist(CreatureNetworkData new_creature, List<CreatureNetworkData> creatures) {
            float dist = 1f;

            CreatureNetworkData found_creature = null;
            float distance = float.MaxValue;
            foreach(CreatureNetworkData creature in creatures) {
                if(creature.health <= 0) continue;
                if(creature.position.CloserThan(new_creature.position, dist)) {
                    float this_distance = creature.position.SqDist(new_creature.position);
                    if(this_distance < distance) {
                        distance = this_distance;
                        found_creature = creature;
                    }
                }
            }

            return found_creature;
        }

        internal static float getCloneDistance(string itemId) {
            float dist = Config.MEDIUM_ITEM_CLONE_MAX_DISTANCE;

            switch(itemId.ToLower()) {
                case "arrow":
                case "jugpart":
                case "potterypartsmall":
                case "barrelpart":
                    dist = Config.SMALL_ITEM_CLONE_MAX_DISTANCE;
                    break;

                case "barrel1":
                case "barrel2":
                case "bucket1":
                case "wheelbarrowassembly_01":
                case "cart_01_forsaken":
                case "cart_01":
                case "cart_02":
                case "cart_03":
                case "bench2m":
                case "shoebench":
                case "workbench1":
                case "table1":
                case "table2":
                case "table4m":
                case "crate":
                case "crateopen1":
                case "crateopen2":
                case "sack01":
                case "pottery_01":
                case "pottery_02":
                case "pottery_03":
                case "pottery_04":
                case "pottery_05":
                case "pottery_06":
                case "pottery_07":
                case "stool1":
                case "chair1":
                case "wickerbasket_01":
                case "wickerbasket_02":
                case "wickerbasket_03":
                case "wickerbasket_04":
                case "wickerbasket_05":
                case "wickerbaskettop":
                    dist = Config.BIG_ITEM_CLONE_MAX_DISTANCE;
                    break;
                    
                case "cranecrate":
                case "chandelier":
                    dist = 100 * 100; //100m should be enough
                    break;

                default: break;
            }

            return dist;
        }

        internal static Item DoesItemAlreadyExist(ItemNetworkData new_item, List<Item> items) {
            float dist = getCloneDistance(new_item.dataId);

            Item found_item = null;
            float distance = float.MaxValue;
            foreach(Item item in items) {
                if(item.transform.position.CloserThan(new_item.position, dist)) {
                    if(item.itemId.Equals(new_item.dataId)) {
                        float this_distance = item.transform.position.SqDist(new_item.position);
                        if(this_distance < distance) {
                            distance = this_distance;
                            found_item = item;
                        }
                    }
                }
            }

            return found_item;
        }

        internal static bool hasItemMoved(ItemNetworkData item) {
            if(item.clientsideItem == null) return false;
            if(item.clientsideItem.holder != null) return false;
            if(item.clientsideItem.mainHandler != null) return false;

            if(!item.position.CloserThan(item.clientsideItem.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(!item.rotation.CloserThan(item.clientsideItem.transform.eulerAngles, Config.REQUIRED_ROTATION_DISTANCE)) {
                return true;
            } else if(!item.velocity.CloserThan(item.clientsideItem.physicBody.velocity, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(!item.angularVelocity.CloserThan(item.clientsideItem.physicBody.angularVelocity, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            }

            return false;
        }

        internal static bool hasCreatureMoved(CreatureNetworkData creature) {
            if(creature.creature == null) return false;

            if(creature.creature.IsRagdolled()) {
                Vector3[] ragdollPositions = null;
                Quaternion[] ragdollRotations = null;
                creature.creature.ReadRagdoll(out ragdollPositions, out ragdollRotations, out _, out _);

                if(creature.ragdollPositions == null) return true;

                float distance = 0f;
                for(int i = 0; i < ragdollPositions.Length; i += 2) {
                    distance += ragdollPositions[i].SqDist(creature.ragdollPositions[i]);
                }
                return distance > Config.REQUIRED_RAGDOLL_MOVE_DISTANCE;
            } else {
                if(!creature.position.CloserThan(creature.creature.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
                    return true;
                } else if(Mathf.Abs(creature.rotationY - creature.creature.transform.eulerAngles.y) > Config.REQUIRED_ROTATION_DISTANCE) {
                    return true;
                }/* else if(!creature.velocity.Approximately(creature.clientsideCreature.locomotion.rb.velocity, Config.REQUIRED_MOVE_DISTANCE)) {
                    return true;
                }*/
            }

            return false;
        }

        internal static bool hasPlayerMoved() {
            if(Player.currentCreature == null) return false;
            if(ModManager.clientSync == null) return false;
            if(ModManager.clientSync.syncData == null) return false;

            PlayerNetworkData playerSync = ModManager.clientSync.syncData.myPlayerData;
            
            if(Config.PLAYER_FULL_BODY_SYNCING) {
                Vector3[] ragdollPositions;
                Quaternion[] ragdollRotations;
                playerSync.creature.ReadRagdoll(out ragdollPositions, out ragdollRotations, out _, out _, animJawBone: true);

                if(playerSync.ragdollPositions == null) return true;

                float distance = 0f;
                for(int i = 0; i < ragdollPositions.Length; i += 2) {
                    distance += ragdollPositions[i].SqDist(playerSync.ragdollPositions[i]);
                }
                return distance > Config.REQUIRED_RAGDOLL_MOVE_DISTANCE;
            } else {
                if(!Player.currentCreature.transform.position.CloserThan(playerSync.position, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
                //if(Mathf.Abs(Player.local.transform.eulerAngles.y - playerSync.playerRot) > REQUIRED_ROTATION_DISTANCE) return true;
                if(!Player.currentCreature.ragdoll.ik.handLeftTarget.position.CloserThan(playerSync.handLeftPos + playerSync.position, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
                if(!Player.currentCreature.ragdoll.ik.handRightTarget.position.CloserThan(playerSync.handRightPos + playerSync.position, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
                //if(Mathf.Abs(Player.currentCreature.ragdoll.headPart.transform.eulerAngles.y - playerSync.playerRot) > Config.REQUIRED_ROTATION_DISTANCE) { return true; }
            }
            return false;
        }

        internal static bool GetCreature(Creature creature, out ItemHolderType holderType, out int networkId) {
            holderType = ItemHolderType.NONE;
            networkId = -1;
            if(creature == null) return false;


            NetworkLocalPlayer nlp = creature.GetComponent<NetworkLocalPlayer>();
            if(nlp != null) {
                networkId = ModManager.clientInstance.netclient.ClientId;
                holderType = ItemHolderType.PLAYER;
                return true;
            } else {
                NetworkCreature networkCreature = creature.GetComponent<NetworkCreature>();
                if(networkCreature != null && networkCreature.creatureNetworkData != null) {
                    networkId = networkCreature.creatureNetworkData.networkedId;
                    holderType = ItemHolderType.CREATURE;
                    return true;
                }
            }

            return false;
        }

        internal static Creature GetCreature(ItemHolderType holderType, int networkId, bool includePlayer = false) {
            switch(holderType) {
                case ItemHolderType.PLAYER:
                    if(ModManager.clientSync.syncData.players.ContainsKey(networkId)) {
                        return ModManager.clientSync.syncData.players[networkId].creature;
                    }
                    if(includePlayer && networkId == ModManager.clientSync.syncData.myPlayerData.clientId) {
                        return Player.currentCreature;
                    }
                    break;
                case ItemHolderType.CREATURE:
                    if(ModManager.clientSync.syncData.creatures.ContainsKey(networkId)) {
                        return ModManager.clientSync.syncData.creatures[networkId].creature;
                    }
                    break;

                default: break;
            }
            return null;
        }

    }
}
