using AMP.Network.Data.Sync;
using System.Collections.Generic;

namespace AMP.Network.Data {
    internal class SyncData {
        internal int currentClientItemId = 1;
        internal int currentClientCreatureId = 1;

        internal Dictionary<long, ItemNetworkData> items = new Dictionary<long, ItemNetworkData>();

        internal Dictionary<long, PlayerNetworkData> players = new Dictionary<long, PlayerNetworkData>();

        internal Dictionary<long, CreatureNetworkData> creatures = new Dictionary<long, CreatureNetworkData>();

        internal PlayerNetworkData myPlayerData = new PlayerNetworkData();
        
        internal string serverlevel = "";
        internal string servermode = "";
        internal Dictionary<string, string> serveroptions;
    }
}
