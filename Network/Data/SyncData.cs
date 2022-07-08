using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace AMP.Network.Data {
    public class SyncData {
        public int currentClientItemId = 1;
        public int currentClientCreatureId = 1;

        public List<Item> serverItems = new List<Item>();
        public List<Item> clientItems = new List<Item>();

        public Dictionary<int, ItemSync> items = new Dictionary<int, ItemSync>();

        public Dictionary<int, PlayerSync> players = new Dictionary<int, PlayerSync>();

        public Dictionary<int, CreatureSync> creatures = new Dictionary<int, CreatureSync>();

        public PlayerSync myPlayerData = new PlayerSync();
    }
}
