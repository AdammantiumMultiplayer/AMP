using AMP.Network.Data.Sync;
using Netamite.Server.Data;
using System;

namespace AMP.Events {
    public class ServerEvents {
        public static Action<ClientInformation> OnPlayerJoin;
        public static Action<ClientInformation> OnPlayerQuit;
        public static Action<PlayerNetworkData, ClientInformation> OnPlayerKilled;

        public static Action<ItemNetworkData, ClientInformation> OnItemSpawned;
        public static Action<ItemNetworkData, ClientInformation> OnItemDespawned;
        public static Action<ItemNetworkData, ClientInformation, ClientInformation> OnItemOwnerChanged;

        public static Action<CreatureNetworkData, ClientInformation> OnCreatureSpawned;
        public static Action<CreatureNetworkData, ClientInformation> OnCreatureDespawned;
        public static Action<CreatureNetworkData, ClientInformation> OnCreatureKilled;
        public static Action<CreatureNetworkData, ClientInformation, ClientInformation> OnCreatureOwnerChanged;
    }
}
