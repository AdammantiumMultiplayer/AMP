using AMP.Data;
using AMP.Extension;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;

namespace AMP.Network.Helper {
    internal class SyncFunc {

        public static int DoesItemAlreadyExist(ItemSync new_item, List<ItemSync> items) {
            foreach(ItemSync item in items) {
                if(item.position.Distance(new_item.position) < Config.ITEM_CLONE_MAX_DISTANCE) {
                    if(item.dataId.Equals(new_item.dataId)) {
                        return item.networkedId;
                    }
                }
            }

            return 0;
        }

        public static Item DoesItemAlreadyExist(ItemSync new_item, List<Item> items) {
            foreach(Item item in items) {
                if(item.transform.position.Distance(new_item.position) < Config.ITEM_CLONE_MAX_DISTANCE) {
                    if(item.itemId.Equals(new_item.dataId)) {
                        return item;
                    }
                }
            }

            return null;
        }

        public static bool hasItemMoved(ItemSync item) {
            if(item.clientsideItem == null) return false;
            if(!item.clientsideItem.isPhysicsOn) return false;
            if(item.clientsideItem.holder != null) return false;
            if(item.clientsideItem.mainHandler != null) return false;

            if(!item.position.Approximately(item.clientsideItem.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(item.rotation.Approximately(item.clientsideItem.transform.eulerAngles, Config.REQUIRED_ROTATION_DISTANCE)) {
                return false;
            }

            return false;
        }

        public static bool hasCreatureMoved(CreatureSync creature) {
            if(creature.clientsideCreature == null) return false;

            if(creature.clientsideCreature.isKilled) return false;

            if(!creature.position.Approximately(creature.clientsideCreature.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(creature.rotation.Approximately(creature.clientsideCreature.transform.eulerAngles, Config.REQUIRED_ROTATION_DISTANCE)) {
                return false;
            }

            return false;
        }

        public static bool hasPlayerMoved() {
            if(Player.currentCreature == null) return false;

            PlayerSync playerSync = ModManager.clientSync.syncData.myPlayerData;

            if(!Player.currentCreature.transform.position.Approximately(playerSync.playerPos, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
            //if(Mathf.Abs(Player.local.transform.eulerAngles.y - playerSync.playerRot) > REQUIRED_ROTATION_DISTANCE) return true;
            if(!Player.currentCreature.ragdoll.ik.handLeftTarget.position.Approximately(playerSync.handLeftPos, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
            if(!Player.currentCreature.ragdoll.ik.handRightTarget.position.Approximately(playerSync.handRightPos, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
            //if(Mathf.Abs(Player.currentCreature.ragdoll.headPart.transform.eulerAngles.y - playerSync.playerRot) > Config.REQUIRED_ROTATION_DISTANCE) { return true; }

            return false;
        }

        public static bool GetCreature(Creature creature, out bool isPlayer, out int networkId) {
            isPlayer = false;
            networkId = -1;
            if(creature == null) return false;

            if(creature == Player.currentCreature) {
                networkId = ModManager.clientInstance.myClientId;
                isPlayer = true;
                return true;
            } else {
                try {
                    KeyValuePair<int, CreatureSync> entry = ModManager.clientSync.syncData.creatures.First(value => creature.Equals(value.Value.clientsideCreature));
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
