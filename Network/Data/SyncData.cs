using AMP.Network.Data.Sync;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AMP.Network.Data {
    internal class SyncData {
        internal int currentClientItemId = 1;
        internal int currentClientCreatureId = 1;

        internal ConcurrentDictionary<int, ItemNetworkData> items = new ConcurrentDictionary<int, ItemNetworkData>();

        internal ConcurrentDictionary<int, PlayerNetworkData> players = new ConcurrentDictionary<int, PlayerNetworkData>();

        internal ConcurrentDictionary<int, CreatureNetworkData> creatures = new ConcurrentDictionary<int, CreatureNetworkData>();

        internal PlayerNetworkData myPlayerData = new PlayerNetworkData();
        
        internal string serverlevel = "";
        internal string servermode = "";
        internal Dictionary<string, string> serveroptions;
    }
}
