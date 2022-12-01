using AMP.Network.Data;
using AMP.Network.Data.Sync;
using System;

namespace AMP.Events {
    public class ServerEvents {
        public static Action<ClientData> OnPlayerJoin;
        public static Action<ClientData> OnPlayerQuit;
        public static Action<PlayerNetworkData, ClientData> OnPlayerKilled;

        public static Action<ItemNetworkData, ClientData> OnItemSpawned;
        public static Action<ItemNetworkData, ClientData> OnItemDespawned;
        public static Action<ItemNetworkData, ClientData, ClientData> OnItemOwnerChanged;

        public static Action<CreatureNetworkData, ClientData> OnCreatureSpawned;
        public static Action<CreatureNetworkData, ClientData> OnCreatureDespawned;
        public static Action<CreatureNetworkData, ClientData> OnCreatureKilled;
        public static Action<CreatureNetworkData, ClientData, ClientData> OnCreatureOwnerChanged;
    }
}
