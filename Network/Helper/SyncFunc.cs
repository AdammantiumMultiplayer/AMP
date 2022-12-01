using AMP.Data;
using AMP.Extension;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Helper {
    internal class SyncFunc {

        internal static long DoesItemAlreadyExist(ItemNetworkData new_item, List<ItemNetworkData> items) {
            float dist = getCloneDistance(new_item.dataId);

            long found_item = 0;
            float distance = float.MaxValue;
            foreach(ItemNetworkData item in items) {
                if(item.position.Approximately(new_item.position, dist)) {
                    if(item.dataId.Equals(new_item.dataId)) {
                        float this_distance = item.position.SQ_DIST(new_item.position);
                        if(this_distance < distance) {
                            distance = this_distance;
                            found_item = item.networkedId;
                        }
                    }
                }
            }

            return found_item;
        }

        private static float getCloneDistance(string itemId) {
            float dist = Config.MEDIUM_ITEM_CLONE_MAX_DISTANCE;

            switch(itemId.ToLower()) {
                case "arrow":
                    dist = Config.SMALL_ITEM_CLONE_MAX_DISTANCE;
                    break;

                case "barrel1":
                case "barrel2":
                case "bucket1":
                case "wheelbarrowassembly_01":
                case "cart_01_forsaken":
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
                if(item.transform.position.Approximately(new_item.position, dist)) {
                    if(item.itemId.Equals(new_item.dataId)) {
                        float this_distance = item.transform.position.SQ_DIST(new_item.position);
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
            if(!item.clientsideItem.isPhysicsOn) return false;
            if(item.clientsideItem.holder != null) return false;
            if(item.clientsideItem.mainHandler != null) return false;

            if(!item.position.Approximately(item.clientsideItem.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(!item.rotation.Approximately(item.clientsideItem.transform.eulerAngles, Config.REQUIRED_ROTATION_DISTANCE)) {
                return true;
            } else if(!item.velocity.Approximately(item.clientsideItem.rb.velocity, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(!item.angularVelocity.Approximately(item.clientsideItem.rb.angularVelocity, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            }

            return false;
        }

        internal static bool hasCreatureMoved(CreatureNetworkData creature) {
            if(creature.creature == null) return false;

            if(creature.creature.IsRagdolled()) {
                Vector3[] ragdollParts = creature.creature.ReadRagdoll();

                if(creature.ragdollParts == null) return true;

                float distance = 0f;
                for(int i = 0; i < ragdollParts.Length; i += 2) {
                    distance += ragdollParts[i].SQ_DIST(creature.ragdollParts[i]);
                }
                return distance > Config.REQUIRED_RAGDOLL_MOVE_DISTANCE;
            } else {
                if(!creature.position.Approximately(creature.creature.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
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
                Vector3[] ragdollParts = playerSync.creature.ReadRagdoll();

                if(playerSync.ragdollParts == null) return true;

                float distance = 0f;
                for(int i = 0; i < ragdollParts.Length; i += 2) {
                    distance += ragdollParts[i].SQ_DIST(playerSync.ragdollParts[i]);
                }
                return distance > Config.REQUIRED_RAGDOLL_MOVE_DISTANCE;
            } else {
                if(!Player.currentCreature.transform.position.Approximately(playerSync.position, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
                //if(Mathf.Abs(Player.local.transform.eulerAngles.y - playerSync.playerRot) > REQUIRED_ROTATION_DISTANCE) return true;
                if(!Player.currentCreature.ragdoll.ik.handLeftTarget.position.Approximately(playerSync.handLeftPos + playerSync.position, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
                if(!Player.currentCreature.ragdoll.ik.handRightTarget.position.Approximately(playerSync.handRightPos + playerSync.position, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
                //if(Mathf.Abs(Player.currentCreature.ragdoll.headPart.transform.eulerAngles.y - playerSync.playerRot) > Config.REQUIRED_ROTATION_DISTANCE) { return true; }
            }
            return false;
        }

        internal static bool GetCreature(Creature creature, out bool isPlayer, out long networkId) {
            isPlayer = false;
            networkId = -1;
            if(creature == null) return false;

            if(creature == Player.currentCreature) {
                networkId = ModManager.clientInstance.myPlayerId;
                isPlayer = true;
                return true;
            } else {
                try {
                    KeyValuePair<long, CreatureNetworkData> entry = ModManager.clientSync.syncData.creatures.First(value => creature.Equals(value.Value.creature));
                    if(entry.Value.networkedId > 0) {
                        networkId = entry.Value.networkedId;
                        return true;
                    }
                } catch(InvalidOperationException) { }
            }

            return false;
        }

    }
}
