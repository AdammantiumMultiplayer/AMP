using AMP.Data;
using AMP.Events;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using Netamite.Network.Packet;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AMP.Network.Server {
    public class Server {
        public NetamiteServer netamiteServer;

        public string currentLevel = null;
        public string currentMode = null;
        public Dictionary<string, string> currentOptions = new Dictionary<string, string>();

        private int currentItemId = 1;
        public int CurrentItemId {
            get { return currentItemId; }
        }
        public int NextItemId {
            get { return Interlocked.Increment(ref currentItemId); }
        }
        public ConcurrentDictionary<int, ItemNetworkData> items = new ConcurrentDictionary<int, ItemNetworkData>();
        internal ConcurrentDictionary<int, int> item_owner = new ConcurrentDictionary<int, int>();

        private int currentCreatureId = 1;
        public int CurrentCreatureId {
            get { return currentCreatureId; }
        }
        public int NextCreatureId {
            get { return Interlocked.Increment(ref currentCreatureId); }
        }
        public ConcurrentDictionary<int, CreatureNetworkData> creatures = new ConcurrentDictionary<int, CreatureNetworkData>();
        internal ConcurrentDictionary<int, int> creature_owner = new ConcurrentDictionary<int, int>();

        private int currentEntityId = 1;
        public int CurrentEntityId {
            get { return currentEntityId; }
        }
        public int NextEntityId {
            get { return Interlocked.Increment(ref currentEntityId); }
        }
        public ConcurrentDictionary<int, EntityNetworkData> entities = new ConcurrentDictionary<int, EntityNetworkData>();
        internal ConcurrentDictionary<int, int> entity_owner = new ConcurrentDictionary<int, int>();


        public static string DEFAULT_MAP = "Home";
        public static string DEFAULT_MODE = "Default";

        public ClientData[] Clients {
            get { 
                return Array.ConvertAll(netamiteServer.Clients, item => (ClientData) item);
            }
        }

        /// <summary>
        /// Amount of players connected to the server
        /// </summary>
        public int connectedClients {
            get { return netamiteServer.Clients.Length; }
        }
        /// <summary>
        /// Amount of players connected to the server and not currently loading the level
        /// </summary>
        public int loadedClients {
            get { return ModManager.serverInstance.Clients.Where(c => c.LoadedLevel).ToArray().Length; }
        }
        public int spawnedItems {
            get { return items.Count; }
        }
        public int spawnedCreatures {
            get { return creatures.Count; }
        }
        public Dictionary<int, string> connectedClientList {
            get {
                Dictionary<int, string> list = new Dictionary<int, string>();
                foreach (var item in netamiteServer._clients) {
                    list.Add(item.Key, item.Value.ClientName);
                }
                return list;
            }
        }

        internal void Stop() {
            Log.Info(Defines.SERVER, $"Stopping server...");

            netamiteServer.Stop();

            Log.Info(Defines.SERVER, $"Server stopped.");
        }

        internal void Start(NetamiteServer netamiteServer) {
            this.netamiteServer = netamiteServer;
            Log.Info(Defines.SERVER, $"Starting server...");

            long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            netamiteServer.BeforeStart += (time) => {
                Dictionary<string, string> options;
                bool levelInfoSuccess = LevelInfo.ReadLevelInfo(out currentLevel, out currentMode, out options);

                if(!levelInfoSuccess || currentLevel.Equals("CharacterSelection")) {
                    currentLevel = DEFAULT_MAP;
                    currentMode = DEFAULT_MODE;
                }

                Log.Info(Defines.SERVER,
                         $"Server started after {DateTimeOffset.Now.ToUnixTimeMilliseconds() - ms}ms.\n" +
                         $"\t Level: {currentLevel} / Mode: {currentMode}\n" +
                         $"\t Options:\n\t{string.Join("\n\t", options.Select(p => p.Key + " = " + p.Value))}\n" +
                         $"\t Max-Players: {netamiteServer.MaxClients}\n" +
                         $"\t Has password: {(netamiteServer.HasConnectToken ? "Yes" : "No")}"
                         );

                netamiteServer.OnDisconnect += OnClientDisconnect;
                netamiteServer.OnDataReceived += ProcessPacket;
                netamiteServer.OnConnect += GreetPlayer;
            };

            netamiteServer.Start();
        }
        private void OnClientDisconnect(ClientInformation client, string reason) {
            LeavePlayer((ClientData) client, reason);
        }

        internal void GreetPlayer(ClientInformation client) {
            Task.Delay(1000).ContinueWith(t => GreetPlayer((ClientData) client, false));
        }

        internal void GreetPlayer(ClientData client, bool loadedLevel = false) {
            if(client.greeted) return;

            if(!loadedLevel) {
                netamiteServer.SendTo(client, new ServerInfoPacket(Defines.MOD_VERSION, netamiteServer.MaxClients, ModManager.safeFile.hostingSettings.allowVoiceChat, ModManager.safeFile.hostingSettings.baseTickRate, ModManager.safeFile.hostingSettings.playerTickRate));

                if(currentLevel.Length > 0) {
                    Log.Debug(Defines.SERVER, $"Waiting for player {client.ClientName} to load into the level.");
                    netamiteServer.SendTo(client, new LevelChangePacket(currentLevel, currentMode, currentOptions));
                    return;
                }
            }

            // Send all player data to the new client
            foreach(ClientData other_client in netamiteServer._clients.Values) {
                if(other_client._player == null) continue;
                netamiteServer.SendTo(client, new PlayerDataPacket(other_client.player));
                netamiteServer.SendTo(client, new PlayerEquipmentPacket(other_client.player));
            }

            SendItemsAndCreatures(client);

            Log.Info(Defines.SERVER, $"Player {client.ClientName} ({client.ClientId} / {client.player.uniqueId}) joined the server.");
            Log.Info(Defines.SERVER, $"Players currently connected: { string.Join(", ", netamiteServer._clients.Select(x => x.Value.ClientName).ToArray()) }");

            ServerEvents.InvokeOnPlayerJoin(client);
            
            ModManager.serverInstance.netamiteServer.InitializeTimeSync(client);

            client.greeted = true;
            client.LoadedLevel = true;
        }

        internal void SendItemsAndCreatures(ClientInformation client) {
            if(items.Count > 0 || creatures.Count > 0) {
                // Clear all already present stuff first
                netamiteServer.SendTo(client, new ClearPacket(true, true));
            }

            // Send all spawned creatures to the client
            foreach(KeyValuePair<int, CreatureNetworkData> entry in creatures) {
                netamiteServer.SendTo(client, new CreatureSpawnPacket(entry.Value));
            }

            // Send all spawned items to the client
            foreach(KeyValuePair<int, ItemNetworkData> entry in items.ToArray()) {
                netamiteServer.SendTo(client, new ItemSpawnPacket(entry.Value));
                if(entry.Value.holdingStates != null && entry.Value.holdingStates.Length > 0) {
                    netamiteServer.SendTo(client, new ItemSnapPacket(entry.Value));
                }
            }

            netamiteServer.SendTo(client, new AllowTransmissionPacket(true));
        }

        private void ProcessPacket(ClientInformation client, NetPacket p) {
            if(p == null) return;
            if(client == null) return;

            byte type = p.getPacketType();

            //Log.Warn("SERVER", type);

            switch(type) {
                default: break;
            }
        }

        public void UpdateItemOwner(ItemNetworkData itemNetworkData, ClientData newOwner) {
            int oldOwnerId = 0;
            if(item_owner.ContainsKey(itemNetworkData.networkedId)) {
                try {
                    oldOwnerId = item_owner[itemNetworkData.networkedId];
                } catch(Exception) { }
                item_owner[itemNetworkData.networkedId] = newOwner.ClientId;
            } else {
                item_owner.TryAdd(itemNetworkData.networkedId, newOwner.ClientId);
            }

            netamiteServer.SendTo(newOwner, new ItemOwnerPacket(itemNetworkData.networkedId, true));
            netamiteServer.SendToAllExcept(new ItemOwnerPacket(itemNetworkData.networkedId, false), newOwner.ClientId);

            if(oldOwnerId != newOwner.ClientId) {
                if(oldOwnerId > 0)
                    Log.Debug(Defines.SERVER, $"{newOwner.ClientName} has taken ownership of item {itemNetworkData.dataId} ({itemNetworkData.networkedId})");

                ClientData oldOwner = null;
                if(oldOwnerId > 0) {
                    oldOwner = (ClientData) netamiteServer.GetClientById(oldOwnerId);
                }
                ServerEvents.InvokeOnItemOwnerChanged(itemNetworkData, oldOwner, newOwner);
            }
        }

        public void UpdateCreatureOwner(CreatureNetworkData creatureNetworkData, ClientData newOwner) {
            int oldOwnerId = 0;
            if(creature_owner.ContainsKey(creatureNetworkData.networkedId)) {
                try {
                    oldOwnerId = creature_owner[creatureNetworkData.networkedId];
                } catch(Exception) { }
                if(creature_owner[creatureNetworkData.networkedId] != newOwner.ClientId) {
                    creature_owner[creatureNetworkData.networkedId] = newOwner.ClientId;
                }
            } else {
                creature_owner.TryAdd(creatureNetworkData.networkedId, newOwner.ClientId);
            }

            netamiteServer.SendTo(newOwner, new CreatureOwnerPacket(creatureNetworkData.networkedId, true));
            netamiteServer.SendToAllExcept(new CreatureOwnerPacket(creatureNetworkData.networkedId, false), newOwner.ClientId);

            List<ItemNetworkData> holdingItems = items.Values.Where(ind => ind.holdingStates != null && ind.holdingStates.Where(state => state.holderNetworkId == creatureNetworkData.networkedId).Count() > 0).ToList();
            foreach(ItemNetworkData item in holdingItems) {
                //if(item.holderType == ItemHolderType.PLAYER) continue; // Don't transfer items that are held by a player // TODO: Check if needed
                UpdateItemOwner(item, newOwner);
            }

            if(oldOwnerId != newOwner.ClientId) {
                if(oldOwnerId > 0)
                    Log.Debug(Defines.SERVER, $"{newOwner.ClientName} has taken ownership of creature {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId})");

                ClientData oldOwner = null;
                if(oldOwnerId > 0) {
                    oldOwner = (ClientData) netamiteServer.GetClientById(oldOwnerId);
                }
                ServerEvents.InvokeOnCreatureOwnerChanged(creatureNetworkData, oldOwner, newOwner);
            }
        }

        internal void ClearItemsAndCreatures() {
            lock(creatures) { creatures.Clear(); }
            lock(creature_owner) { creature_owner.Clear(); }
            lock(items) { items.Clear(); }
            lock(item_owner) { item_owner.Clear(); }
        }

        internal void LeavePlayer(ClientData client, string reason = "Player disconnected") {
            Log.Debug(Defines.SERVER, $"{client.ClientName} initialized a disconnect. {reason}");

            if(netamiteServer.Clients.Length <= 0) {
                ClearItemsAndCreatures();
                Log.Info(Defines.SERVER, $"Clearing server because last player disconnected.");
            } else {
                try {
                    ClientInformation migrateUser = netamiteServer.Clients.First(entry => entry.ClientId != client.ClientId);
                    try {
                        KeyValuePair<int, int>[] entries = item_owner.Where(entry => entry.Value == client.ClientId).ToArray();

                        if(entries.Length > 0) {
                            foreach(KeyValuePair<int, int> entry in entries) {
                                if(items.ContainsKey(entry.Key)) {
                                    item_owner[entry.Key] = migrateUser.ClientId;
                                    netamiteServer.SendTo(migrateUser, new ItemOwnerPacket(entry.Key, true));
                                }
                            }
                            Log.Info(Defines.SERVER, $"Migrated items from { client.ClientName } to { migrateUser.ClientName }.");
                        }
                    } catch(Exception e) {
                        Log.Err(Defines.SERVER, $"Couldn't migrate items from {client.ClientName} to { migrateUser.ClientName}.\n{e}");
                    }

                    try {
                        KeyValuePair<int, int>[] entries = creature_owner.Where(entry => entry.Value == client.ClientId).ToArray();

                        if(entries.Length > 0) {
                            foreach(KeyValuePair<int, int> entry in entries) {
                                if(creatures.ContainsKey(entry.Key)) {
                                    creature_owner[entry.Key] = migrateUser.ClientId;
                                    netamiteServer.SendTo(migrateUser, new CreatureOwnerPacket(entry.Key, true));
                                }
                            }
                            Log.Info(Defines.SERVER, $"Migrated creatures from { client.ClientName } to { migrateUser.ClientName }.");
                        }
                    } catch(Exception e) {
                        Log.Err(Defines.SERVER, $"Couldn't migrate creatures from { client.ClientName } to { migrateUser.ClientName }.\n{e}");
                    }
                } catch(Exception e) {
                    Log.Err(Defines.SERVER, $"Couldn't migrate stuff from { client.ClientName } to other client.\n{e}");
                }
            }

            ServerEvents.InvokeOnPlayerQuit(client);

            Log.Info(Defines.SERVER, $"{client.ClientName} disconnected. {reason}");
        }

        public ClientData GetClientById(int id) {
            return (ClientData) netamiteServer.GetClientById(id);
        }

        public ClientData GetClientByName(string name) {
            int id = -1;
            try {
                id = int.Parse(name);
            } catch(Exception) {
                foreach(var client in ModManager.serverInstance.Clients) {
                    if(client.ClientName.ToLower().Contains(name.ToLower())) {
                        id = client.ClientId;
                        break;
                    }
                }
            }

            return ModManager.serverInstance.GetClientById(id);
        }
    }
}
