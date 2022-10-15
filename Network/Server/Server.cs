using AMP.Data;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.SupportFunctions;
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

        public uint maxClients = 4;
        private int port = 13698;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        internal string currentLevel = null;
        internal string currentMode = null;
        internal Dictionary<string, string> currentOptions = new Dictionary<string, string>();

        private long currentPlayerId = 1;
        internal Dictionary<long, ClientData> clients = new Dictionary<long, ClientData>();
        private Dictionary<string, long> endPointMapping = new Dictionary<string, long>();

        private long currentItemId = 1;
        internal Dictionary<long, ItemNetworkData> items = new Dictionary<long, ItemNetworkData>();
        internal Dictionary<long, long> item_owner = new Dictionary<long, long>();
        internal long currentCreatureId = 1;
        internal Dictionary<long, long> creature_owner = new Dictionary<long, long>();
        internal Dictionary<long, CreatureNetworkData> creatures = new Dictionary<long, CreatureNetworkData>();

        public static string DEFAULT_MAP = "Home";
        public static string DEFAULT_MODE = "Default";

        public int connectedClients {
            get { return clients.Count; }
        }
        public int spawnedItems {
            get { return items.Count; }
        }
        public int spawnedCreatures {
            get { return creatures.Count; }
        }
        public Dictionary<long, string> connectedClientList {
            get {
                Dictionary<long, string> test = new Dictionary<long, string>();
                foreach (var item in clients) {
                    test.Add(item.Key, item.Value.name);
                }
                return test;
            }
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

            if(ModManager.discordNetworking) {
                DiscordNetworking.DiscordNetworking.instance.Disconnect();
            } else {
                tcpListener.Stop();
                udpListener.Dispose();

                tcpListener = null;
                udpListener = null;
            }

            Log.Info("[Server] Server stopped.");
        }

        internal void Start() {
            int ms = DateTime.UtcNow.Millisecond;
            Log.Info("[Server] Starting server...");

            if(port > 0) {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                tcpListener.BeginAcceptTcpClient(TCPRequestCallback, null);

                udpListener = new UdpClient(port);
                udpListener.BeginReceive(UDPRequestCallback, null);
            }

            Dictionary<string, string> options = new Dictionary<string, string>();
            bool levelInfoSuccess = LevelInfo.ReadLevelInfo(ref currentLevel, ref currentMode, ref options);

            if(!levelInfoSuccess || currentLevel.Equals("CharacterSelection")) {
                currentLevel = DEFAULT_MAP;
                currentMode = DEFAULT_MODE;
            }

            isRunning = true;
            
            Log.Info($"[Server] Server started after {DateTime.UtcNow.Millisecond - ms}ms.\n" +
                     $"\t Level: {currentLevel} / Mode: {currentMode}\n" +
                     $"\t Options:\n\t{string.Join("\n\t", options.Select(p => p.Key + " = " + p.Value))}\n" +
                     $"\t Max-Players: {maxClients} / Port: {port}"
                     );
        }

        internal int packetsSent = 0;
        internal int packetsReceived = 0;
        private int udpPacketSent = 0;
        internal void UpdatePacketCount() {
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

        internal void GreetPlayer(ClientData cd, bool loadedLevel = false) {
            if(!clients.ContainsKey(cd.playerId)) {
                clients.Add(cd.playerId, cd);

                SendReliableTo(cd.playerId, PacketWriter.Welcome(cd.playerId));
            }

            if(currentLevel.Length > 0 && !loadedLevel) {
                Log.Debug($"[Server] Waiting for player {cd.playerId} to load into the level.");
                SendReliableTo(cd.playerId, PacketWriter.LoadLevel(currentLevel, currentMode, currentOptions));
                return;
            }

            // Send all player data to the new client
            foreach(ClientData other_client in clients.Values) {
                if(other_client.playerSync == null) continue;
                SendReliableTo(cd.playerId, other_client.playerSync.CreateConfigPacket());
                SendReliableTo(cd.playerId, other_client.playerSync.CreateEquipmentPacket());
            }

            SendItemsAndCreatures(cd);

            Log.Debug("[Server] Welcoming player " + cd.playerId);

            cd.greeted = true;
        }

        private void SendItemsAndCreatures(ClientData cd) {
            // Send all spawned creatures to the client
            foreach(KeyValuePair<long, CreatureNetworkData> entry in creatures) {
                SendReliableTo(cd.playerId, entry.Value.CreateSpawnPacket());
            }

            // Send all spawned items to the client
            foreach(KeyValuePair<long, ItemNetworkData> entry in items) {
                SendReliableTo(cd.playerId, entry.Value.CreateSpawnPacket());
                if(entry.Value.creatureNetworkId > 0) {
                    SendReliableTo(cd.playerId, entry.Value.SnapItemPacket());
                }
            }

            SendReliableTo(cd.playerId, PacketWriter.Welcome(-1));
        }

        #region TCP/IP Callbacks
        private void TCPRequestCallback(IAsyncResult _result) {
            if(tcpListener == null) return;
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

            ClientData cd = new ClientData(currentPlayerId++);
            cd.tcp = socket;
            cd.tcp.onPacket += (packet) => {
                OnPacket(cd, packet);
            };
            cd.name = "Player " + cd.playerId;

            GreetPlayer(cd);
        }

        private void UDPRequestCallback(IAsyncResult _result) {
            if(udpListener == null) return;
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
        #endregion

        internal void OnPacket(ClientData client, Packet p) {
            p.ResetPos();
            Packet.Type type = p.ReadType();

            switch(type) {
                #region Connection handling and stuff
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
                #endregion

                #region Player Packets
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

                case Packet.Type.playerRagdoll:
                    if(client.playerSync == null) break;

                    client.playerSync.ApplyRagdollPacket(p);
                    client.playerSync.clientId = client.playerId;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(client.playerSync.CreateRagdollPacket());
                    #else
                    SendReliableToAllExcept(client.playerSync.CreateRagdollPacket(), client.playerId);
                    #endif
                    break;

                case Packet.Type.playerHealth:
                    p.ReadLong(); // Flush the id

                    client.playerSync.ApplyHealthPacket(p);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(client.playerSync.CreateHealthPacket());
                    #else
                    SendReliableToAllExcept(client.playerSync.CreateHealthPacket(), client.playerId);
                    #endif
                    break;

                case Packet.Type.playerHealthChange:
                    if(!ServerConfig.pvpEnable) break;
                    if(ServerConfig.pvpDamageMultiplier <= 0) break;

                    long playerId = p.ReadLong();
                    float change = p.ReadFloat();

                    if(clients.ContainsKey(playerId)) {
                        change *= ServerConfig.pvpDamageMultiplier;

                        SendReliableTo(playerId, clients[playerId].playerSync.CreateHealthChangePacket(change));
                    }
                    break;
                #endregion

                #region Item Packets
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
                #endregion

                #region Imbues
                case Packet.Type.itemImbue:
                    SendReliableToAllExcept(p, client.playerId); // Just forward them atm
                    break;
                #endregion

                #region Level Changing
                case Packet.Type.loadLevel:
                    string level = p.ReadString();
                    string mode = p.ReadString();

                    if(!client.greeted) {
                        GreetPlayer(client, true);
                        return;
                    }

                    if(level == null) return;
                    if(mode == null) return;

                    if(level.Equals("characterselection", StringComparison.OrdinalIgnoreCase)) return;

                    if(!(level.Equals(currentLevel, StringComparison.OrdinalIgnoreCase) && mode.Equals(currentMode, StringComparison.OrdinalIgnoreCase))) { // Player is the first to join that level
                        if(!ServerConfig.allowMapChange) {
                            Log.Warn("Player " + client.name + " tried changing level.");
                            SendReliableTo(client.playerId, PacketWriter.Disconnect(client.playerId, "Map changing is not allowed by the server!"));
                            LeavePlayer(client);
                            return;
                        }
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
                    } else { // Player joined after another is already in it, so we send all items and stuff
                        SendItemsAndCreatures(client);
                    }
                    break;
                #endregion

                #region Creature Packets
                case Packet.Type.creatureSpawn:
                    Data.Sync.CreatureNetworkData creatureSync = new Data.Sync.CreatureNetworkData();
                    creatureSync.ApplySpawnPacket(p);

                    if(creatureSync.networkedId > 0) return;

                    creatureSync.networkedId = currentCreatureId++;

                    UpdateCreatureOwner(creatureSync, client);
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
                        change = p.ReadFloatLP();
                        creatureSync.ApplyHealthChange(change);

                        SendReliableToAllExcept(creatureSync.CreateHealthChangePacket(change), client.playerId);

                        // If the damage the player did is more than 30% of the already dealt damage, then change the npc to that players authority
                        Log.Debug(change / (creatureSync.maxHealth - creatureSync.health));
                        if(change / (creatureSync.maxHealth - creatureSync.health) > 0.3) {
                            UpdateCreatureOwner(creatureSync, client);
                        }
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

                case Packet.Type.creatureRagdoll:
                    networkId = p.ReadLong();

                    if(creatures.ContainsKey(networkId)) {
                        CreatureNetworkData cnd = creatures[networkId];
                        cnd.ApplyRagdollPacket(p);

                        SendUnreliableToAllExcept(p, client.playerId);
                    }
                    break;

                case Packet.Type.creatureSlice:
                    networkId = p.ReadLong();

                    if(creatures.ContainsKey(networkId)) {
                        SendUnreliableToAllExcept(p, client.playerId);
                    }
                    break;

                case Packet.Type.creatureOwn:
                    networkId = p.ReadLong();

                    if(networkId > 0 && creatures.ContainsKey(networkId)) {
                        UpdateCreatureOwner(creatures[networkId], client);
                    }
                    break;
                #endregion

                default: break;
            }
        }

        internal void UpdateItemOwner(ItemNetworkData itemNetworkData, long playerId) {
            if(item_owner.ContainsKey(itemNetworkData.networkedId)) {
                item_owner[itemNetworkData.networkedId] = playerId;
            } else {
                item_owner.Add(itemNetworkData.networkedId, playerId);
            }
        }

        internal void UpdateCreatureOwner(CreatureNetworkData creatureNetworkData, ClientData client) {
            if(creature_owner.ContainsKey(creatureNetworkData.networkedId)) {
                if(creature_owner[creatureNetworkData.networkedId] != client.playerId) {
                    creature_owner[creatureNetworkData.networkedId] = client.playerId;

                    SendReliableTo(client.playerId, PacketWriter.SetCreatureOwnership(creatureNetworkData.networkedId, true));
                    SendReliableToAllExcept(PacketWriter.SetCreatureOwnership(creatureNetworkData.networkedId, false), client.playerId);

                    Log.Debug($"[Server] {client.name} has taken ownership of creature {creatureNetworkData.creatureId} ({creatureNetworkData.networkedId})");
                }
            } else {
                creature_owner.Add(creatureNetworkData.networkedId, client.playerId);
            }
        }


        internal void LeavePlayer(ClientData client) {
            if(client == null) return;

            if(clients.Count <= 1) {
                items.Clear();
                item_owner.Clear();
                Log.Info($"[Server] Clearing all items, because last player disconnected.");
            } else {
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
                        Log.Err($"[Server] Couldn't migrate items from {client.name} to { migrateUser.name }.\n{e}");
                    }
                } catch(Exception e) {
                    Log.Err($"[Server] Couldn't migrate items from { client.name } to other client.\n{e}");
                }
            }

            if(client.udp != null && endPointMapping.ContainsKey(client.udp.endPoint.ToString())) endPointMapping.Remove(client.udp.endPoint.ToString());
            clients.Remove(client.playerId);
            client.Disconnect();

            SendReliableToAll(PacketWriter.Disconnect(client.playerId, "Player disconnected"));

            Log.Info($"[Server] {client.name} disconnected.");
        }

        // TCP
        internal void SendReliableToAll(Packet p) {
            SendReliableToAllExcept(p);
        }

        internal void SendReliableTo(long clientId, Packet p) {
            if(!clients.ContainsKey(clientId)) return;

            if(ModManager.discordNetworking) {
                DiscordNetworking.DiscordNetworking.instance.SendReliable(p, clientId, true);
            } else {
                clients[clientId].tcp.SendPacket(p);
            }
        }

        internal void SendReliableToAllExcept(Packet p, params long[] exceptions) {
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
        internal void SendUnreliableToAll(Packet p) {
            SendUnreliableToAllExcept(p);
        }

        internal void SendUnreliableTo(long clientId, Packet p) {
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

        internal void SendUnreliableToAllExcept(Packet p, params long[] exceptions) {
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
