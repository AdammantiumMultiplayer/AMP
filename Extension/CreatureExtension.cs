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

    }
}
