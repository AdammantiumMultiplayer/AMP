using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace AMP.Extension {
    public static class StartNetworkItem {

        public static NetworkItem StartNetworking(this ItemNetworkData itemNetworkData) {
            NetworkItem networkItem = itemNetworkData.clientsideItem.gameObject.GetComponent<NetworkItem>();
            if(networkItem == null) networkItem = itemNetworkData.clientsideItem.gameObject.AddComponent<NetworkItem>();
            networkItem.Init(itemNetworkData);


            return networkItem;
        }

        public static NetworkCreature StartNetworking(this CreatureNetworkData creatureNetworkData) {
            NetworkCreature networkCreature = creatureNetworkData.clientsideCreature.gameObject.GetComponent<NetworkCreature>();
            if(networkCreature == null) networkCreature = creatureNetworkData.clientsideCreature.gameObject.AddComponent<NetworkCreature>();
            networkCreature.Init(creatureNetworkData);


            return networkCreature;
        }

        public static NetworkPlayerCreature StartNetworking(this PlayerNetworkData playerNetworkData) {
            Creature creature = playerNetworkData.creature;

            NetworkPlayerCreature networkPlayerCreature = creature.gameObject.GetComponent<NetworkPlayerCreature>();
            if(networkPlayerCreature == null) networkPlayerCreature = creature.gameObject.AddComponent<NetworkPlayerCreature>();
            networkPlayerCreature.Init(playerNetworkData);


            return networkPlayerCreature;
        }

    }
}
