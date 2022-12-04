using AMP.Network.Data.Sync;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AMP.Network.Data {
    internal class SyncData {
        internal int currentClientItemId = 1;
        internal int currentClientCreatureId = 1;

        internal ConcurrentDictionary<long, ItemNetworkData> items = new ConcurrentDictionary<long, ItemNetworkData>();

        internal ConcurrentDictionary<long, PlayerNetworkData> players = new ConcurrentDictionary<long, PlayerNetworkData>();

        internal ConcurrentDictionary<long, CreatureNetworkData> creatures = new ConcurrentDictionary<long, CreatureNetworkData>();

        internal PlayerNetworkData myPlayerData = new PlayerNetworkData();
        
        internal string serverlevel = "";
        internal string servermode = "";
        internal Dictionary<string, string> serveroptions;
    }
}
