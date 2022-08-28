using AMP.Network.Data.Sync;
using System.Collections.Generic;

namespace AMP.Network.Data {
    public class SyncData {
        public int currentClientItemId = 1;
        public int currentClientCreatureId = 1;

        public Dictionary<long, ItemNetworkData> items = new Dictionary<long, ItemNetworkData>();

        public Dictionary<long, PlayerNetworkData> players = new Dictionary<long, PlayerNetworkData>();

        public Dictionary<long, CreatureNetworkData> creatures = new Dictionary<long, CreatureNetworkData>();

        public PlayerNetworkData myPlayerData = new PlayerNetworkData();
        
        public string serverlevel = "";
        public string servermode = "";
        public Dictionary<string, string> serveroptions;
    }
}
