using AMP.Network.Data.Sync;
using System.Collections.Generic;

namespace AMP.Network.Data {
    public class SyncData {
        public int currentClientItemId = 1;
        public int currentClientCreatureId = 1;

        public Dictionary<long, ItemSync> items = new Dictionary<long, ItemSync>();

        public Dictionary<long, PlayerSync> players = new Dictionary<long, PlayerSync>();

        public Dictionary<long, CreatureSync> creatures = new Dictionary<long, CreatureSync>();

        public PlayerSync myPlayerData = new PlayerSync();
        
        public string serverlevel = "";
        public string servermode = "";
    }
}
