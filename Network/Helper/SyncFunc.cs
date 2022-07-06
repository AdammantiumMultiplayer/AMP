using AMP.Extension;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Network.Helper {
    internal class SyncFunc {

        // Assume the item is the same if they are the same type and like 1mm apart
        private const float ITEM_MAX_DISTANCE = 0.0001f;


        // Min distance a item needs to move before its position is updated
        private const float ITEM_REQUIRED_MOVE_DISTANCE = 0.0001f;

        // Min distance a item needs to move before its position is updated
        private const float ITEM_REQUIRED_ROTATION_DISTANCE = 1f;


        public static int DoesItemAlreadyExist(ItemSync new_item) {
            foreach(KeyValuePair<int, ItemSync> entry in ModManager.serverInstance.items) {
                ItemSync item = entry.Value;

                if(item.position.Distance(new_item.position) < ITEM_MAX_DISTANCE) {
                    if(item.dataId.Equals(new_item.dataId)) {
                        return entry.Key;
                    }
                }

            }

            return 0;
        }

        public static bool hasItemMoved(ItemSync item) {
            if(item.clientsideItem == null) return false;

            if(!item.position.Approximately(item.clientsideItem.transform.position, ITEM_REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(item.rotation.Approximately(item.clientsideItem.transform.eulerAngles, ITEM_REQUIRED_ROTATION_DISTANCE)) {
                return true;
            }

            return false;
        }

    }
}
