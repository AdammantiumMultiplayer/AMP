using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AMP.Network.Data {
    internal class SyncData {
        internal int currentClientItemId = 1;
        internal int currentClientCreatureId = 1;
        internal int currentClientEntityId = 1;

        internal ConcurrentDictionary<int, ItemNetworkData> items = new ConcurrentDictionary<int, ItemNetworkData>();

        internal ConcurrentDictionary<int, PlayerNetworkData> players = new ConcurrentDictionary<int, PlayerNetworkData>();

        internal ConcurrentDictionary<int, CreatureNetworkData> creatures = new ConcurrentDictionary<int, CreatureNetworkData>();

        internal ConcurrentDictionary<int, EntityNetworkData> entities = new ConcurrentDictionary<int, EntityNetworkData>();

        internal PlayerNetworkData myPlayerData = new PlayerNetworkData();

        internal List<int> owningCreatures = new List<int>();
        internal List<int> owningItems = new List<int>();

        internal string serverlevel = "";
        internal string servermode = "";
        internal Dictionary<string, string> serveroptions;

        internal bool enable_item_book = true;
        internal bool enable_spawn_book = true;

        internal ServerInfoPacket server_config;
    }
}
