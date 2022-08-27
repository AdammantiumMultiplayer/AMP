using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using ThunderRoad;

namespace AMP.Network.Server {
    public class Server {
        internal bool isRunning = false;

        private uint maxClients = 4;
        private int port = 13698;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public string currentLevel = null;
        public string currentMode = null;
        public Dictionary<string, string> currentOptions = new Dictionary<string, string>();

        public Dictionary<long, ClientData> clients = new Dictionary<long, ClientData>();
        private Dictionary<string, long> endPointMapping = new Dictionary<string, long>();

        private int currentItemId = 1;
        public Dictionary<long, ItemNetworkData> items = new Dictionary<long, ItemNetworkData>();
        public Dictionary<long, long> item_owner = new Dictionary<long, long>();
        private int currentCreatureId = 1;
        public Dictionary<long, Data.Sync.CreatureNetworkData> creatures = new Dictionary<long, Data.Sync.CreatureNetworkData>();

        public int connectedClients {
            get { return clients.Count; }
        }
        public int spawnedItems {
            get { return items.Count; }
        }
        public int spawnedCreatures {
            get { return creatures.Count; }
        }

        public Server(uint maxClients, int port) {
            this.maxClients = maxClients;
            this.port = port;
        }

        internal void Stop() {
            Log.Info("[Server] Stopping server...");

            foreach(ClientData clientData in clients.Values) {
                SendReliableTo(clientData.playerId, PacketWriter.Disconnect(clientData.playerId, "Server closed"));
            }

            tcpListener.Stop();
            udpListener.Dispose();

            tcpListener = null;
            udpListener = null;

            Log.Info("[Server] Server stopped.");
        }

        internal void Start() {
            Log.Info("[Server] Starting server...");

            if(port > 0) {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                tcpListener.BeginAcceptTcpClient(TCPRequestCallback, null);

                udpListener = new UdpClient(port);
                udpListener.BeginReceive(UDPRequestCallback, null);
            }

            if(Level.current != null && Level.current.data != null && Level.current.data.id != null && Level.current.data.id.Length > 0) {
                currentLevel = Level.current.data.id;
                currentMode = Level.current.mode.name;


                Dictionary<string, string> options = new Dictionary<string, string>();
                foreach(KeyValuePair<string, string> entry in Level.current.options) {
                    options.Add(entry.Key, entry.Value);
                }

                if(Level.current.dungeon != null && !options.ContainsKey(LevelOption.DungeonSeed.ToString())) {
                    options.Add(LevelOption.DungeonSeed.ToString(), Level.current.dungeon.seed.ToString());
                }
                currentOptions = options;
            }

            if(currentLevel == null || currentLevel.Equals("CharacterSelection")) {
                currentLevel = "Home";
                currentMode = "Default";
            }

            isRunning = true;
            Log.Info($"[Server] Server started.\nLevel: {currentLevel} / Mode: {currentMode}\nMax-Players: {maxClients} / Port: {port}");
        }

        public int packetsSent = 0;
        public int packetsReceived = 0;
        private int udpPacketSent = 0;
        public void UpdatePacketCount() {
            packetsSent = udpPacketSent;
            packetsReceived = 0;
            foreach(ClientData cd in clients.Values) {
                packetsSent += (cd.tcp != null ? cd.tcp.GetPacketsSent() : 0)
                                + (cd.udp != null ? cd.udp.GetPacketsSent() : 0);
                packetsReceived += (cd.tcp != null ? cd.tcp.GetPacketsReceived() : 0)
                                    + (cd.udp != null ? cd.udp.GetPacketsReceived() : 0);
            }
            udpPacketSent = 0;
        }

        private int playerId = 1;

        private void TCPRequestCallback(IAsyncResult _result) {
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPRequestCallback, null);
            Log.Debug($"[Server] Incoming connection from {tcpClient.Client.RemoteEndPoint}...");

            TcpSocket socket = new TcpSocket(tcpClient);

            if(connectedClients >= maxClients) {
                Log.Warn("[Server] Client tried to join full server.");
                socket.SendPacket(PacketWriter.Error("server is full"));
                socket.Disconnect();
                return;
            }

            ClientData cd = new ClientData(playerId++);
            cd.tcp = socket;
            cd.tcp.onPacket += (packet) => {
                OnPacket(cd, packet);
            };
            cd.name = "Player " + cd.playerId;

            GreetPlayer(cd);
        }

        public void GreetPlayer(ClientData cd) {
            clients.Add(cd.playerId, cd);

            SendReliableTo(cd.playerId, PacketWriter.Welcome(cd.playerId));

            // Send all player data to the new client
            foreach(ClientData other_client in clients.Values) {
                if(other_client.playerSync == null) continue;
                SendReliableTo(cd.playerId, other_client.playerSync.CreateConfigPacket());
                SendReliableTo(cd.playerId, other_client.playerSync.CreateEquipmentPacket());
            }

            // Send all spawned creatures to the client
            foreach(KeyValuePair<long, Data.Sync.CreatureNetworkData> entry in creatures) {
                SendReliableTo(cd.playerId, entry.Value.CreateSpawnPacket());
            }

            // Send all spawned items to the client
            foreach(KeyValuePair<long, ItemNetworkData> entry in items) {
                SendReliableTo(cd.playerId, entry.Value.CreateSpawnPacket());
                if(entry.Value.creatureNetworkId > 0) {
                    SendReliableTo(cd.playerId, entry.Value.SnapItemPacket());
                }
            }

            if(currentLevel.Length > 0) {
                SendReliableTo(cd.playerId, PacketWriter.LoadLevel(currentLevel, currentMode, currentOptions));
            }

            Log.Debug("[Server] Welcoming player " + cd.playerId);

            SendReliableTo(cd.playerId, PacketWriter.Welcome(-1));
        }

        private void UDPRequestCallback(IAsyncResult _result) {
            try {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(_result, ref clientEndPoint);
                udpListener.BeginReceive(UDPRequestCallback, null);

                if(data.Length < 8) return;

                // Check if its a welcome package and if the user is not linked up
                using(Packet packet = new Packet(data)) {
                    packet.ReadInt(); // Flush length away
                    Packet.Type packetType = packet.ReadType();
                    if(packetType == Packet.Type.welcome) {
                        long clientId = packet.ReadLong();

                        // If no udp is connected, then link up
                        if(clients[clientId].udp == null) {
                            clients[clientId].udp = new UdpSocket(clientEndPoint);
                            endPointMapping.Add(clientEndPoint.ToString(), clientId);
                            clients[clientId].udp.onPacket += (p) => {
                                OnPacket(clients[clientId], p);
                            };

                            Log.Debug("[Server] Linked UDP for " + clientId);
                            return;
                        }
                    }
                }

                // Determine client id by EndPoint
                if(endPointMapping.ContainsKey(clientEndPoint.ToString())) {
                    long clientId = endPointMapping[clientEndPoint.ToString()];
                    if(!clients.ContainsKey(clientId)) {
                        Log.Err("[Server] This should not happen... #SNHE001"); // SNHE = Should not happen error
                    } else {
                        if(clients[clientId].udp.endPoint.ToString() == clientEndPoint.ToString()) {
                            clients[clientId].udp.HandleData(new Packet(data));
                        }
                    }
                } else {
                    Log.Err("[Server] Invalid UDP client tried to connect " + clientEndPoint.ToString());
                }
            } catch(Exception e) {
                Log.Err($"[Server] Error receiving UDP data: {e}");
            }
        }

        public void OnPacket(ClientData client, Packet p) {
            Packet.Type type = p.ReadType();

            //Debug.Log("[Server] Packet " + type + " from " + client.playerId);

            switch(type) {
                case Packet.Type.welcome:
                    // Other user is sending multiple messages, one should reach the server
                    // Debug.Log($"[Server] UDP {client.name}...");
                    break;

                case Packet.Type.message:
                    Log.Debug($"[Server] Message from {client.name}: {p.ReadString()}");
                    break;

                case Packet.Type.disconnect:
                    LeavePlayer(clients[client.playerId]);
                    break;

                case Packet.Type.playerData:
                    if(client.playerSync == null) {
                        Log.Info($"[Server] Player {client.name} ({client.playerId}) joined the server.");

                        client.playerSync = new PlayerNetworkData() { clientId = client.playerId };
                    }
                    client.playerSync.ApplyConfigPacket(p);

                    client.playerSync.name = Regex.Replace(client.playerSync.name, @"[^\u0000-\u007F]+", string.Empty);

                    client.playerSync.clientId = client.playerId;
                    client.name = client.playerSync.name;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(client.playerSync.CreateConfigPacket());//, client.playerId);
                    #else
                    SendReliableToAllExcept(client.playerSync.CreateConfigPacket(), client.playerId);
                    #endif
                    break;

                case Packet.Type.playerPos:
                    if(client.playerSync == null) break;

                    client.playerSync.ApplyPosPacket(p);
                    client.playerSync.clientId = client.playerId;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendUnreliableToAll(client.playerSync.CreatePosPacket());//, client.playerId);
                    #else
                    SendUnreliableToAllExcept(client.playerSync.CreatePosPacket(), client.playerId);
                    #endif
                    break;

                case Packet.Type.playerEquip:
                    p.ReadLong(); // Flush the id

                    client.playerSync.ApplyEquipmentPacket(p);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(client.playerSync.CreateEquipmentPacket());
                    #else
                    SendReliableToAllExcept(client.playerSync.CreateEquipmentPacket(), client.playerId);
                    #endif
                    break;

                case Packet.Type.playerHealthChange:
                    if(!Config.ENABLE_PVP) break;

                    long playerId = p.ReadLong();
                    float change = p.ReadFloat();

                    if(clients.ContainsKey(playerId)) {
                        //Log.Warn(client.name + " / " + playerId + " / " + change);

                        SendReliableTo(playerId, clients[playerId].playerSync.CreateHealthChangePacket(change));
                    }
                    break;

                case Packet.Type.itemSpawn:
                    ItemNetworkData itemSync = new ItemNetworkData();
                    itemSync.ApplySpawnPacket(p);

                    itemSync.networkedId = SyncFunc.DoesItemAlreadyExist(itemSync, items.Values.ToList());
                    bool was_duplicate = false;
                    if(itemSync.networkedId <= 0) {
                        itemSync.networkedId = currentItemId++;
                        items.Add(itemSync.networkedId, itemSync);
                        UpdateItemOwner(itemSync, client.playerId);
                        Log.Debug($"[Server] {client.name} has spawned item {itemSync.dataId} ({itemSync.networkedId})" );
                    } else {
                        itemSync.clientsideId = -itemSync.clientsideId;
                        Log.Debug($"[Server] {client.name} has duplicate of {itemSync.dataId} ({itemSync.networkedId})");
                        was_duplicate = true;
                    }

                    SendReliableTo(client.playerId, itemSync.CreateSpawnPacket());

                    if(was_duplicate) return; // If it was a duplicate, dont send it to other players

                    itemSync.clientsideId = 0;
                    
                    SendReliableToAllExcept(itemSync.CreateSpawnPacket(), client.playerId);
                    break;

                case Packet.Type.itemDespawn:
                    long to_despawn = p.ReadLong();

                    if(items.ContainsKey(to_despawn)) {
                        itemSync = items[to_despawn];

                        Log.Debug($"[Server] {client.name} has despawned item {itemSync.dataId} ({itemSync.networkedId})");

                        SendReliableToAllExcept(itemSync.DespawnPacket(), client.playerId);

                        items.Remove(to_despawn);
                        if(item_owner.ContainsKey(to_despawn)) item_owner.Remove(to_despawn);
                    }

                    break;

                case Packet.Type.itemPos:
                    long to_update = p.ReadLong();

                    if(items.ContainsKey(to_update)) {
                        itemSync = items[to_update];

                        itemSync.ApplyPosPacket(p);

                        SendUnreliableToAllExcept(itemSync.CreatePosPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.itemOwn:
                    long networkId = p.ReadLong();

                    if(networkId > 0 && items.ContainsKey(networkId)) {
                        UpdateItemOwner(items[networkId], client.playerId);

                        SendReliableTo(client.playerId, PacketWriter.SetItemOwnership(networkId, true));
                        SendReliableToAllExcept(PacketWriter.SetItemOwnership(networkId, false), client.playerId);
                    }
                    break;

                case Packet.Type.itemSnap:
                    networkId = p.ReadLong();

                    if(networkId > 0 && items.ContainsKey(networkId)) {
                        itemSync = items[networkId];
                        itemSync.creatureNetworkId = p.ReadLong();
                        itemSync.drawSlot = (Holder.DrawSlot) p.ReadByte();
                        itemSync.holdingSide = (Side) p.ReadByte();
                        itemSync.holderIsPlayer = p.ReadBool();

                        Log.Debug($"[Server] Snapped item {itemSync.dataId} to {itemSync.creatureNetworkId} to { (itemSync.drawSlot == Holder.DrawSlot.None ? "hand " + itemSync.holdingSide : "slot " + itemSync.drawSlot) }.");
                        SendReliableToAllExcept(itemSync.SnapItemPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.itemUnSnap:
                    networkId = p.ReadLong();

                    if(networkId > 0 && items.ContainsKey(networkId)) {
                        itemSync = items[networkId];
                        Log.Debug($"[Server] Unsnapped item {itemSync.dataId} from {itemSync.creatureNetworkId}.");

                        itemSync.creatureNetworkId = 0;
                        itemSync.drawSlot = Holder.DrawSlot.None;
                        itemSync.holderIsPlayer = false;

                        SendReliableToAllExcept(itemSync.UnSnapItemPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.loadLevel:
                    string level = p.ReadString();
                    string mode = p.ReadString();

                    if(level == null) return;
                    if(mode == null) return;

                    if(level.Equals("characterselection", StringComparison.OrdinalIgnoreCase)) return;

                    if(!(level.Equals(currentLevel, StringComparison.OrdinalIgnoreCase) && mode.Equals(currentMode, StringComparison.OrdinalIgnoreCase))) {
                        currentLevel = level;
                        currentMode = mode;

                        currentOptions.Clear();
                        int count = p.ReadInt();
                        while(count > 0) {
                            currentOptions.Add(p.ReadString(), p.ReadString());
                            count--;
                        }

                        Log.Info($"[Server] Client { client.playerId } loaded level { level } with mode {mode}.");
                        SendReliableToAllExcept(PacketWriter.LoadLevel(currentLevel, currentMode, currentOptions), client.playerId);
                    }
                    break;

                case Packet.Type.creatureSpawn:
                    Data.Sync.CreatureNetworkData creatureSync = new Data.Sync.CreatureNetworkData();
                    creatureSync.ApplySpawnPacket(p);

                    if(creatureSync.networkedId > 0) return;

                    creatureSync.networkedId = currentCreatureId++;

                    creatures.Add(creatureSync.networkedId, creatureSync);
                    Log.Debug($"[Server] {client.name} has summoned {creatureSync.creatureId} ({creatureSync.networkedId})");

                    SendReliableTo(client.playerId, creatureSync.CreateSpawnPacket());

                    creatureSync.clientsideId = 0;

                    SendReliableToAllExcept(creatureSync.CreateSpawnPacket(), client.playerId);
                    break;


                case Packet.Type.creaturePos:
                    to_update = p.ReadLong();

                    if(creatures.ContainsKey(to_update)) {
                        creatureSync = creatures[to_update];
                        creatureSync.ApplyPosPacket(p);

                        SendUnreliableToAllExcept(creatureSync.CreatePosPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.creatureHealth:
                    to_update = p.ReadLong();

                    if(creatures.ContainsKey(to_update)) {
                        creatureSync = creatures[to_update];
                        creatureSync.ApplyHealthPacket(p);

                        //Log.Debug(client.name + " / " + creatureSync.networkedId + " / " + creatureSync.health);

                        SendReliableToAllExcept(creatureSync.CreateHealthPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.creatureHealthChange:
                    to_update = p.ReadLong();

                    if(creatures.ContainsKey(to_update)) {
                        creatureSync = creatures[to_update];
                        change = p.ReadFloat();
                        creatureSync.ApplyHealthChange(change);

                        //Log.Warn(client.name + " / " + creatureSync.networkedId + " / " + change);

                        SendReliableToAllExcept(creatureSync.CreateHealthChangePacket(change), client.playerId);
                    }
                    break;

                case Packet.Type.creatureDespawn:
                    to_despawn = p.ReadLong();

                    if(creatures.ContainsKey(to_despawn)) {
                        creatureSync = creatures[to_despawn];

                        Log.Debug($"[Server] {client.name} has despawned creature {creatureSync.creatureId} ({creatureSync.networkedId})");
                        SendReliableToAllExcept(creatureSync.CreateDespawnPacket(), client.playerId);

                        creatures.Remove(to_despawn);
                    }
                    break;

                case Packet.Type.creatureAnimation:
                    networkId = p.ReadLong();
                    int stateHash = p.ReadInt();
                    string clipName = p.ReadString();

                    if(creatures.ContainsKey(networkId)) {
                        SendReliableToAllExcept(PacketWriter.CreatureAnimation(networkId, stateHash, clipName), client.playerId);
                    }
                    break;

                default: break;
            }
        }

        public void UpdateItemOwner(ItemNetworkData itemSync, long playerId) {
            if(item_owner.ContainsKey(itemSync.networkedId)) {
                item_owner[itemSync.networkedId] = playerId;
            } else {
                item_owner.Add(itemSync.networkedId, playerId);
            }
        }


        public void LeavePlayer(ClientData client) {
            if(client == null) return;

            if(clients.Count <= 1) {
                items.Clear();
                item_owner.Clear();
                Log.Info($"[Server] Clearing all items, because last player disconnected.");
            }

            try {
                ClientData migrateUser = clients.First(entry => entry.Value.playerId != client.playerId).Value;
                try {
                    IEnumerable<KeyValuePair<long, long>> entries = item_owner.Where(entry => entry.Value == client.playerId);

                    foreach(KeyValuePair<long, long> entry in entries) {
                        if(items.ContainsKey(entry.Key)) {
                            SendReliableTo(migrateUser.playerId, PacketWriter.SetItemOwnership(entry.Key, true));
                        }
                    }
                    Log.Info($"[Server] Migrated items from { client.name } to { migrateUser.name }.");
                } catch(Exception e) {
                    Log.Err($"[Server] Couldn't migrate items from {client.name} to { migrateUser.name }. {e}");
                }
            } catch(Exception e) {
                Log.Err($"[Server] Couldn't migrate items from { client.name } to other client. {e}");
            }

            if(client.udp != null && endPointMapping.ContainsKey(client.udp.endPoint.ToString())) endPointMapping.Remove(client.udp.endPoint.ToString());
            clients.Remove(client.playerId);
            client.Disconnect();

            SendReliableToAll(PacketWriter.Disconnect(client.playerId, "Player disconnected"));

            Log.Info($"[Server] {client.name} disconnected.");
        }

        // TCP
        public void SendReliableToAll(Packet p) {
            SendReliableToAllExcept(p);
        }

        public void SendReliableTo(long clientId, Packet p) {
            if(!clients.ContainsKey(clientId)) return;

            if(ModManager.discordNetworking) {
                DiscordNetworking.DiscordNetworking.instance.SendReliable(p, clientId, true);
            } else {
                clients[clientId].tcp.SendPacket(p);
            }
        }

        public void SendReliableToAllExcept(Packet p, params long[] exceptions) {
            foreach(KeyValuePair<long, ClientData> client in clients) {
                if(exceptions.Contains(client.Key)) continue;

                if(ModManager.discordNetworking) {
                    DiscordNetworking.DiscordNetworking.instance.SendReliable(p, client.Key, true);
                } else {
                    client.Value.tcp.SendPacket(p);
                }
            }
        }

        // UDP
        public void SendUnreliableToAll(Packet p) {
            SendUnreliableToAllExcept(p);
        }

        public void SendUnreliableTo(long clientId, Packet p) {
            if(!clients.ContainsKey(clientId)) return;

            if(ModManager.discordNetworking) {
                DiscordNetworking.DiscordNetworking.instance.SendReliable(p, clientId, true);
            } else {
                try {
                    if(clients[clientId].udp.endPoint != null) {
                        udpListener.Send(p.ToArray(), p.Length(), clients[clientId].udp.endPoint);
                        udpPacketSent++;
                    }
                } catch(Exception e) {
                    Log.Err($"Error sending data to {clients[clientId].udp.endPoint} via UDP: {e}");
                }
            }
        }

        public void SendUnreliableToAllExcept(Packet p, params long[] exceptions) {
            //p.WriteLength();
            foreach(KeyValuePair<long, ClientData> client in clients) {
                if(exceptions.Contains(client.Key)) continue;

                if(ModManager.discordNetworking) {
                    DiscordNetworking.DiscordNetworking.instance.SendUnreliable(p, client.Key, true);
                } else {
                    try {
                        if(client.Value.udp.endPoint != null) {
                            udpListener.Send(p.ToArray(), p.Length(), client.Value.udp.endPoint);
                            udpPacketSent++;
                        }
                    } catch(Exception e) {
                        Log.Err($"Error sending data to {client.Value.udp.endPoint} via UDP: {e}");
                    }
                }
            }
        }
    }
}
