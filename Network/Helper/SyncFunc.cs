using AMP.Extension;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Helper {
    internal class SyncFunc {

        // Assume the item is the same if they are the same type and like 1mm apart
        private const float ITEM_CLONE_MAX_DISTANCE = 0.0001f;


        // Min distance a item needs to move before its position is updated
        private const float REQUIRED_MOVE_DISTANCE = 0.0001f;

        // Min distance a item needs to move before its position is updated
        private const float REQUIRED_ROTATION_DISTANCE = 1f;


        public static int DoesItemAlreadyExist(ItemSync new_item) {
            foreach(KeyValuePair<int, ItemSync> entry in ModManager.serverInstance.items) {
                ItemSync item = entry.Value;

                if(item.position.Distance(new_item.position) < ITEM_CLONE_MAX_DISTANCE) {
                    if(item.dataId.Equals(new_item.dataId)) {
                        return entry.Key;
                    }
                }

            }

            return 0;
        }

        public static bool hasItemMoved(ItemSync item) {
            if(item.clientsideItem == null) return false;

            if(!item.position.Approximately(item.clientsideItem.transform.position, REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(item.rotation.Approximately(item.clientsideItem.transform.eulerAngles, REQUIRED_ROTATION_DISTANCE)) {
                return false;
            }

            return false;
        }

        public static bool hasPlayerMoved() {
            if(Player.currentCreature == null) return false;

            PlayerSync playerSync = ModManager.clientSync.syncData.myPlayerData;

            // TODO: More checks when hand movement is synched correctly
            //playerSync.handLeftPos = Player.currentCreature.handLeft.transform.position;
            //playerSync.handLeftRot = Player.currentCreature.handLeft.transform.eulerAngles;
            //
            //playerSync.handRightPos = Player.currentCreature.handRight.transform.position;
            //playerSync.handRightRot = Player.currentCreature.handRight.transform.eulerAngles;
            //
            //playerSync.headRot = Player.currentCreature.ragdoll.headPart.transform.eulerAngles;

            if(!Player.local.transform.position.Approximately(playerSync.playerPos, REQUIRED_MOVE_DISTANCE)) return true;
            if(Mathf.Abs(Player.local.transform.eulerAngles.y - playerSync.playerRot) > REQUIRED_ROTATION_DISTANCE) return true;

            return false;
        }

    }
}
