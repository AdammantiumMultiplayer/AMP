using AMP.Data;
using AMP.Events;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Connection;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Network.Packets;
using AMP.Network.Packets.Implementation;
using AMP.Security;
using AMP.SupportFunctions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Server {
    public class Server {
        internal bool isRunning = false;

        public uint maxClients = 4;
        public int port = 13698;
        private string password = "";

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public string currentLevel = null;
        public string currentMode = null;
        internal Dictionary<string, string> currentOptions = new Dictionary<string, string>();

        private long currentPlayerId = 1;
        internal Dictionary<long, ClientData> clients = new Dictionary<long, ClientData>();

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

        public Server(uint maxClients, int port, string password = "") {
            this.maxClients = maxClients;
            this.port = port;
            this.password = Encryption.SHA256(password);
        }

        private Thread timeoutThread;

        internal void Stop() {
            Log.Info(Defines.SERVER, $"Stopping server...");

            foreach(ClientData clientData in clients.Values) {
                SendReliableTo(clientData.playerId, new DisconnectPacket(clientData.playerId, "Server closed"));
            }

            tcpListener.Stop();
            udpListener.Dispose();

            tcpListener = null;
            udpListener = null;

            if(timeoutThread != null) {
                try {
                    timeoutThread.Abort();
                } catch { }
            }

            Log.Info(Defines.SERVER, $"Server stopped.");
        }

        internal void Start() {
            long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Log.Info(Defines.SERVER, $"Starting server...");

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

            timeoutThread = new Thread(() => {
                while(isRunning) {
                    long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    foreach(ClientData cd in clients.Values.ToArray()) {
                        if(cd.last_time < now - 30000) { // 30 Sekunden
                            try {
                                LeavePlayer(cd, "Played timed out");
                            } catch { }
                        }
                    }
                    Thread.Sleep(5000);
                }
            });
            timeoutThread.Name = "TimeoutThead";
            timeoutThread.Start();
            
            Log.Info(Defines.SERVER,
                     $"Server started after {DateTimeOffset.Now.ToUnixTimeMilliseconds() - ms}ms.\n" +
                     $"\t Level: {currentLevel} / Mode: {currentMode}\n" +
                     $"\t Options:\n\t{string.Join("\n\t", options.Select(p => p.Key + " = " + p.Value))}\n" +
                     $"\t Max-Players: {maxClients} / Port: {port}\n" +
                     $"\t Has password: {(password != null && password.Length > 0 ? "Yes" : "No")}"
                     );
        }

        internal void GreetPlayer(ClientData cd, bool loadedLevel = false) {
            if(cd.greeted) return;

            if(!clients.ContainsKey(cd.playerId)) {
                clients.Add(cd.playerId, cd);
                SendReliableTo(cd.playerId, new WelcomePacket(cd.playerId));
            }

            if(currentLevel.Length > 0 && !loadedLevel) {
                Log.Debug(Defines.SERVER, $"Waiting for player {cd.name} to load into the level.");
                cd.last_time = DateTimeOffset.Now.ToUnixTimeMilliseconds() + 90000; // Give players 2 minutes to connect.
                SendReliableTo(cd.playerId, new LevelChangePacket(currentLevel, currentMode, currentOptions));
                return;
            }

            SendReliableTo(cd.playerId, new ServerInfoPacket((int) maxClients));

            // Send all player data to the new client
            foreach(ClientData other_client in clients.Values) {
                if(other_client.playerSync == null) continue;
                SendReliableTo(cd.playerId, new PlayerDataPacket(other_client.playerSync));
                SendReliableTo(cd.playerId, new PlayerEquipmentPacket(other_client.playerSync));
            }

            SendItemsAndCreatures(cd);

            Log.Info(Defines.SERVER, $"Player {cd.name} ({cd.playerId}) joined the server.");

            try { if(ServerEvents.OnPlayerJoin != null) ServerEvents.OnPlayerJoin.Invoke(cd); } catch (Exception e) { Log.Err(e); }

            cd.greeted = true;
        }

        private void SendItemsAndCreatures(ClientData cd) {
            // Send all spawned creatures to the client
            foreach(KeyValuePair<long, CreatureNetworkData> entry in creatures) {
                SendReliableTo(cd.playerId, new CreatureSpawnPacket(entry.Value));
            }

            // Send all spawned items to the client
            foreach(KeyValuePair<long, ItemNetworkData> entry in items) {
                SendReliableTo(cd.playerId, new ItemSpawnPacket(entry.Value));
                if(entry.Value.creatureNetworkId > 0) {
                    SendReliableTo(cd.playerId, new ItemSnapPacket(entry.Value));
                }
            }

            SendReliableTo(cd.playerId, new AllowTransmissionPacket(true));
        }

        #region TCP/IP Callbacks
        private void TCPRequestCallback(IAsyncResult _result) {
            if(tcpListener == null) return;
            TcpClient tcpClient;
            try {
                tcpClient = tcpListener.EndAcceptTcpClient(_result);
            }catch(ObjectDisposedException) { return; } // Happens when closing a already disposed socket, so we can just ignore it
            tcpListener.BeginAcceptTcpClient(TCPRequestCallback, null);
            Log.Debug($"[Server] Incoming connection from {tcpClient.Client.RemoteEndPoint}...");

            TcpSocket socket = new TcpSocket(tcpClient);

            socket.onPacket += (packet) => {
                WaitForConnection(socket, packet);
            };
        }

        private void WaitForConnection(TcpSocket socket, NetPacket p) {
            if(p is ServerPingPacket) {
                ServerPingPacket serverPingPacket = new ServerPingPacket();

                socket.QueuePacket(serverPingPacket);
                socket.Disconnect();
            } else if(p is EstablishConnectionPacket) {
                EstablishConnectionPacket ecp = (EstablishConnectionPacket)p;

                string error = CheckEstablishConnection(ecp);
                if(error == null) {
                    EstablishConnection(currentPlayerId++, ecp.name, socket);
                } else {
                    socket.QueuePacket(new ErrorPacket(error));
                    socket.Disconnect();
                    return;
                }
            }
        }

        private void UDPRequestCallback(IAsyncResult _result) {
            if(udpListener == null) return;
            try {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(_result, ref clientEndPoint);
                udpListener.BeginReceive(UDPRequestCallback, null);

                if(data.Length <= 1) return;

                // Check if its a welcome package and if the user is not linked up
                using(NetPacket packet = NetPacket.ReadPacket(data, true)) {
                    if(packet is WelcomePacket) {
                        long clientId = ((WelcomePacket) packet).playerId;

                        // If no udp is connected, then link up
                        if(clients[clientId].udp == null) {
                            clients[clientId].udp = new UdpSocket(clientEndPoint);
                            clients[clientId].udp.onPacket += (p) => {
                                if(clients.ContainsKey(clientId)) OnPacket(clients[clientId], p);
                            };

                            Log.Debug("[Server] Linked UDP for " + clients[clientId].name + " (" + clientEndPoint + ")");
                            return;
                        }
                    }
                }

                // Determine client id by EndPoint
                bool found = false;
                foreach(ClientData c in clients.Values) {
                    if(c.udp.endPoint.Equals(clientEndPoint)) {
                        c.udp.HandleData(NetPacket.ReadPacket(data, true));
                        found = true;
                        break;
                    }
                }
                if(!found) {
                    Log.Err(Defines.SERVER, $"Invalid UDP client tried to connect {clientEndPoint}");
                }
            } catch(Exception e) {
                Log.Err(Defines.SERVER, $"Error receiving UDP data: {e}");
            }
        }
        #endregion

        #region Establish Connection Handler
        internal string CheckEstablishConnection(EstablishConnectionPacket ecp) {
            if(!ecp.version.Equals(Defines.MOD_VERSION)) {
                Log.Warn(Defines.SERVER, $"Client {ecp.name} tried to join with version {ecp.version} but server is on { Defines.MOD_VERSION }.");
                return $"Version Mismatch. Client {ecp.version} / Server: { Defines.MOD_VERSION }";
            }

            if(password != null && password.Length > 0) {
                if(!password.Equals(ecp.password)) { // Passwords are hashed with SHA256
                    Log.Warn(Defines.SERVER, $"Client {ecp.name} tried to join with wrong password.");
                    return $"Wrong password.";
                }
            }

            if(connectedClients >= maxClients) {
                Log.Warn(Defines.SERVER, $"Client {ecp.name} tried to join full server.");
                return "Server is already full.";
            }

            return null;
        }

        internal void EstablishConnection(long playerId, string name = "Unnamed", TcpSocket tcpSocket = null) {
            if(playerId <= 0) playerId = currentPlayerId++;

            ClientData cd = new ClientData(playerId);
            cd.tcp = tcpSocket;
            cd.tcp.onPacket = (packet) => {
                OnPacket(cd, packet);
            };
            cd.name = name;

            GreetPlayer(cd);
        }
        #endregion

        private class PacketQueueData { public ClientData clientData; public NetPacket packet; public PacketQueueData(ClientData clientData, NetPacket packet) { this.clientData = clientData; this.packet = packet; } }
        private ConcurrentQueue<PacketQueueData> packetQueue = new ConcurrentQueue<PacketQueueData>();
        public void OnPacket(ClientData client, NetPacket p) {
            packetQueue.Enqueue(new PacketQueueData(client, p));

            ProcessPacketQueue();
        }

        private void ProcessPacketQueue() {
            PacketQueueData data;
            lock(packetQueue) {
                while(packetQueue.TryDequeue(out data)) {
                    ProcessPacket(data.clientData, data.packet);
                }
            }
        }

        private void ProcessPacket(ClientData client, NetPacket p) {
            if(p == null) return;
            if(client == null) return;

            client.last_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            PacketType type = (PacketType) p.getPacketType();

            switch(type) {
                #region Connection handling and stuff
                case PacketType.WELCOME:
                    WelcomePacket welcomePacket = (WelcomePacket) p;

                    // Other user is sending multiple messages, one should reach the server
                    // Debug.Log($"[Server] UDP {client.name}...");
                    break;

                case PacketType.MESSAGE:
                    MessagePacket messagePacket = (MessagePacket) p;

                    Log.Debug(Defines.SERVER, $"Message from {client.name}: { messagePacket.message }");
                    break;

                case PacketType.DISCONNECT:
                    DisconnectPacket disconnectPacket = (DisconnectPacket) p;

                    LeavePlayer(clients[client.playerId], disconnectPacket.reason);
                    break;

                case PacketType.PING:
                    PingPacket pingPacket = (PingPacket)p;

                    SendReliableTo(client.playerId, pingPacket);
                    break;
                #endregion

                #region Player Packets
                case PacketType.PLAYER_DATA:
                    PlayerDataPacket playerDataPacket = (PlayerDataPacket) p;

                    if(client.playerSync == null) {
                        client.playerSync = new PlayerNetworkData() { clientId = client.playerId };
                    }
                    client.playerSync.Apply(playerDataPacket);

                    client.playerSync.name = Regex.Replace(client.playerSync.name, @"[^\u0000-\u007F]+", string.Empty);

                    client.playerSync.clientId = client.playerId;
                    client.name = client.playerSync.name;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(new PlayerDataPacket(client.playerSync));//, client.playerId);
                    #else
                    SendReliableToAllExcept(new PlayerDataPacket(client.playerSync), client.playerId);
                    #endif
                    break;

                case PacketType.PLAYER_POSITION:
                    PlayerPositionPacket playerPositionPacket = (PlayerPositionPacket) p;

                    if(client.playerSync == null) break;

                    client.playerSync.Apply(playerPositionPacket);
                    client.playerSync.clientId = client.playerId;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendUnreliableToAll(new PlayerPositionPacket(client.playerSync));//, client.playerId);
                    #else
                    SendUnreliableToAllExcept(new PlayerPositionPacket(client.playerSync), client.playerId);
                    #endif
                    break;

                case PacketType.PLAYER_EQUIPMENT:
                    PlayerEquipmentPacket playerEquipmentPacket = (PlayerEquipmentPacket) p;

                    client.playerSync.Apply(playerEquipmentPacket);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(new PlayerEquipmentPacket(client.playerSync));
                    #else
                    SendReliableToAllExcept(new PlayerEquipmentPacket(client.playerSync), client.playerId);
                    #endif
                    break;

                case PacketType.PLAYER_RAGDOLL:
                    PlayerRagdollPacket playerRagdollPacket = (PlayerRagdollPacket) p;

                    if(client.playerSync == null) break;
                    if(client.playerId != playerRagdollPacket.playerId) return;

                    client.playerSync.Apply(playerRagdollPacket);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(playerRagdollPacket);
                    #else
                    SendReliableToAllExcept(playerRagdollPacket, client.playerId);
                    #endif
                    break;

                case PacketType.PLAYER_HEALTH_SET:
                    PlayerHealthSetPacket playerHealthSetPacket = (PlayerHealthSetPacket) p;

                    if(client.playerSync.Apply(playerHealthSetPacket)) {
                        try { if(ServerEvents.OnPlayerKilled != null) ServerEvents.OnPlayerKilled.Invoke(client.playerSync, client); } catch(Exception e) { Log.Err(e); }
                    }

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(new PlayerHealthSetPacket(client.playerSync));
                    #else
                    SendReliableToAllExcept(new PlayerHealthSetPacket(client.playerSync), client.playerId);
                    #endif
                    break;

                case PacketType.PLAYER_HEALTH_CHANGE:
                    PlayerHealthChangePacket playerHealthChangePacket = (PlayerHealthChangePacket) p;
                    
                    if(!ModManager.safeFile.hostingSettings.pvpEnable) break;
                    if(ModManager.safeFile.hostingSettings.pvpDamageMultiplier <= 0) break;

                    if(clients.ContainsKey(playerHealthChangePacket.playerId)) {
                        playerHealthChangePacket.change *= ModManager.safeFile.hostingSettings.pvpDamageMultiplier;

                        SendReliableTo(playerHealthChangePacket.playerId, playerHealthChangePacket);
                    }
                    break;
                #endregion

                #region Item Packets
                case PacketType.ITEM_SPAWN:
                    ItemSpawnPacket itemSpawnPacket = (ItemSpawnPacket) p;

                    ItemNetworkData ind = new ItemNetworkData();
                    ind.Apply(itemSpawnPacket);

                    ind.networkedId = SyncFunc.DoesItemAlreadyExist(ind, items.Values.ToList());
                    bool was_duplicate = false;
                    if(ind.networkedId <= 0) {
                        ind.networkedId = currentItemId++;
                        items.Add(ind.networkedId, ind);
                        UpdateItemOwner(ind, client);
                        Log.Debug(Defines.SERVER, $"{client.name} has spawned item {ind.dataId} ({ind.networkedId})" );
                    } else {
                        ind.clientsideId = -ind.clientsideId;
                        Log.Debug(Defines.SERVER, $"{client.name} has duplicate of {ind.dataId} ({ind.networkedId})");
                        was_duplicate = true;
                    }

                    SendReliableTo(client.playerId, new ItemSpawnPacket(ind));

                    if(was_duplicate) return; // If it was a duplicate, dont send it to other players

                    ind.clientsideId = 0;
                    
                    SendReliableToAllExcept(new ItemSpawnPacket(ind), client.playerId);

                    try { if(ServerEvents.OnItemSpawned != null) ServerEvents.OnItemSpawned.Invoke(ind, client); } catch(Exception e) { Log.Err(e); }
                    break;

                case PacketType.ITEM_DESPAWN:
                    ItemDespawnPacket itemDespawnPacket = (ItemDespawnPacket) p;

                    if(items.ContainsKey(itemDespawnPacket.itemId)) {
                        ind = items[itemDespawnPacket.itemId];

                        Log.Debug(Defines.SERVER, $"{client.name} has despawned item {ind.dataId} ({ind.networkedId})");

                        SendReliableToAllExcept(itemDespawnPacket, client.playerId);

                        items.Remove(itemDespawnPacket.itemId);
                        if(item_owner.ContainsKey(itemDespawnPacket.itemId)) item_owner.Remove(itemDespawnPacket.itemId);

                        try { if(ServerEvents.OnItemDespawned != null) ServerEvents.OnItemDespawned.Invoke(ind, client); } catch(Exception e) { Log.Err(e); }
                    }

                    break;

                case PacketType.ITEM_POSITION:
                    ItemPositionPacket itemPositionPacket = (ItemPositionPacket) p;

                    if(items.ContainsKey(itemPositionPacket.itemId)) {
                        ind = items[itemPositionPacket.itemId];

                        ind.Apply(itemPositionPacket);

                        SendUnreliableToAllExcept(itemPositionPacket, client.playerId);
                    }
                    break;

                case PacketType.ITEM_OWNER:
                    ItemOwnerPacket itemOwnerPacket = (ItemOwnerPacket) p;

                    if(itemOwnerPacket.itemId > 0 && items.ContainsKey(itemOwnerPacket.itemId)) {
                        UpdateItemOwner(items[itemOwnerPacket.itemId], client);

                        SendReliableTo(client.playerId, new ItemOwnerPacket(itemOwnerPacket.itemId, true));
                        SendReliableToAllExcept(new ItemOwnerPacket(itemOwnerPacket.itemId, false), client.playerId);
                    }
                    break;

                case PacketType.ITEM_SNAPPING_SNAP:
                    ItemSnapPacket itemSnapPacket = (ItemSnapPacket) p;

                    if(itemSnapPacket.itemId > 0 && items.ContainsKey(itemSnapPacket.itemId)) {
                        ind = items[itemSnapPacket.itemId];

                        ind.Apply(itemSnapPacket);

                        Log.Debug(Defines.SERVER, $"Snapped item {ind.dataId} to {ind.creatureNetworkId} to { (ind.drawSlot == Holder.DrawSlot.None ? "hand " + ind.holdingSide : "slot " + ind.drawSlot) }.");
                        SendReliableToAllExcept(itemSnapPacket, client.playerId);
                    }
                    break;

                case PacketType.ITEM_SNAPPING_UNSNAP:
                    ItemUnsnapPacket itemUnsnapPacket = (ItemUnsnapPacket) p;

                    if(itemUnsnapPacket.itemId > 0 && items.ContainsKey(itemUnsnapPacket.itemId)) {
                        ind = items[itemUnsnapPacket.itemId];
                        Log.Debug(Defines.SERVER, $"Unsnapped item {ind.dataId} from {ind.creatureNetworkId}.");

                        ind.Apply(itemUnsnapPacket);

                        SendReliableToAllExcept(itemUnsnapPacket, client.playerId);
                    }
                    break;
                #endregion

                #region Imbues
                case PacketType.ITEM_IMBUE:
                    ItemImbuePacket itemImbuePacket = (ItemImbuePacket) p;

                    SendReliableToAllExcept(p, client.playerId); // Just forward them atm
                    break;
                #endregion

                #region Level Changing
                case PacketType.DO_LEVEL_CHANGE:
                    LevelChangePacket levelChangePacket = (LevelChangePacket) p;

                    if(!client.greeted) {
                        GreetPlayer(client, true);
                        return;
                    }

                    if(levelChangePacket.level == null) return;
                    if(levelChangePacket.mode  == null) return;

                    if(levelChangePacket.level.Equals("characterselection", StringComparison.OrdinalIgnoreCase)) return;

                    if(!(levelChangePacket.level.Equals(currentLevel, StringComparison.OrdinalIgnoreCase) && levelChangePacket.mode.Equals(currentMode, StringComparison.OrdinalIgnoreCase))) { // Player is the first to join that level
                        if(!ModManager.safeFile.hostingSettings.allowMapChange) {
                            Log.Err(Defines.SERVER, $"Player { client.name } tried changing level.");
                            SendReliableTo(client.playerId, new DisconnectPacket(client.playerId, "Map changing is not allowed by the server!"));
                            LeavePlayer(client);
                            return;
                        }

                        if(levelChangePacket.eventTime == EventTime.OnStart) {
                            Log.Info(Defines.SERVER, $"Player {client.name} started to load level {levelChangePacket.level} with mode {levelChangePacket.mode}.");
                            ClearItemsAndCreatures();
                            SendReliableToAllExcept(new PrepareLevelChangePacket(client.name, levelChangePacket.level, levelChangePacket.mode), client.playerId);

                            ModManager.serverInstance.SendReliableToAllExcept(
                                  new DisplayTextPacket("level_change", $"Player {client.name} loading into {levelChangePacket.level}.\nPlease stay in your level.", Color.yellow, Vector3.forward * 2, true, true, 60)
                                , client.playerId
                            );
                        } else {
                            currentLevel = levelChangePacket.level;
                            currentMode = levelChangePacket.mode;

                            currentOptions = levelChangePacket.option_dict;

                            ClearItemsAndCreatures();
                            Log.Info(Defines.SERVER, $"Player { client.name } loaded level {currentLevel} with mode {currentMode}.");
                            SendReliableToAllExcept(levelChangePacket, client.playerId);
                        }
                    }
                    if(levelChangePacket.eventTime == EventTime.OnEnd) {
                        SendItemsAndCreatures(client); // If its the first player changing the level, this will send nothing other than the permission to start sending stuff
                    }
                    break;
                #endregion

                #region Creature Packets
                case PacketType.CREATURE_SPAWN:
                    CreatureSpawnPacket creatureSpawnPacket = (CreatureSpawnPacket) p;

                    CreatureNetworkData cnd = new CreatureNetworkData();
                    cnd.Apply(creatureSpawnPacket);

                    cnd.networkedId = currentCreatureId++;
                    creatureSpawnPacket.creatureId = cnd.networkedId;

                    UpdateCreatureOwner(cnd, client);
                    creatures.Add(cnd.networkedId, cnd);
                    Log.Debug(Defines.SERVER, $"{client.name} has summoned {cnd.creatureType} ({cnd.networkedId})");

                    SendReliableTo(client.playerId, creatureSpawnPacket);

                    creatureSpawnPacket.clientsideId = 0;

                    SendReliableToAllExcept(creatureSpawnPacket, client.playerId);

                    try { if(ServerEvents.OnCreatureSpawned != null) ServerEvents.OnCreatureSpawned.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                    break;


                case PacketType.CREATURE_POSITION:
                    CreaturePositionPacket creaturePositionPacket = (CreaturePositionPacket) p;

                    if(creatures.ContainsKey(creaturePositionPacket.creatureId)) {
                        if(creature_owner[creaturePositionPacket.creatureId] != client.playerId) return;

                        cnd = creatures[creaturePositionPacket.creatureId];
                        cnd.Apply(creaturePositionPacket);

                        SendUnreliableToAllExcept(creaturePositionPacket, client.playerId);
                    }
                    break;

                case PacketType.CREATURE_HEALTH_SET:
                    CreatureHealthSetPacket creatureHealthSetPacket = (CreatureHealthSetPacket) p;

                    if(creatures.ContainsKey(creatureHealthSetPacket.creatureId)) {
                        cnd = creatures[creatureHealthSetPacket.creatureId];
                        if(cnd.Apply(creatureHealthSetPacket)) {
                            try { if(ServerEvents.OnCreatureKilled != null) ServerEvents.OnCreatureKilled.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                        }

                        SendReliableToAllExcept(creatureHealthSetPacket, client.playerId);
                    }
                    break;

                case PacketType.CREATURE_HEALTH_CHANGE:
                    CreatureHealthChangePacket creatureHealthChangePacket = (CreatureHealthChangePacket) p;

                    if(creatures.ContainsKey(creatureHealthChangePacket.creatureId)) {
                        cnd = creatures[creatureHealthChangePacket.creatureId];
                        if(cnd.Apply(creatureHealthChangePacket)) {
                            try { if(ServerEvents.OnCreatureKilled != null) ServerEvents.OnCreatureKilled.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                        }

                        SendReliableToAllExcept(creatureHealthChangePacket, client.playerId);

                        // If the damage the player did is more than 30% of the already dealt damage,
                        // then change the npc to that players authority
                        if(creatureHealthChangePacket.change / (cnd.maxHealth - cnd.health) > 0.3) {
                            UpdateCreatureOwner(cnd, client);
                        }
                    }
                    break;

                case PacketType.CREATURE_DESPAWN:
                    CreatureDepawnPacket creatureDepawnPacket = (CreatureDepawnPacket) p;

                    if(creatures.ContainsKey(creatureDepawnPacket.creatureId)) {
                        cnd = creatures[creatureDepawnPacket.creatureId];

                        Log.Debug(Defines.SERVER, $"{client.name} has despawned creature {cnd.creatureType} ({cnd.networkedId})");
                        SendReliableToAllExcept(creatureDepawnPacket, client.playerId);

                        creatures.Remove(creatureDepawnPacket.creatureId);

                        try { if(ServerEvents.OnCreatureDespawned != null) ServerEvents.OnCreatureDespawned.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                    }
                    break;

                case PacketType.CREATURE_PLAY_ANIMATION:
                    CreatureAnimationPacket creatureAnimationPacket = (CreatureAnimationPacket) p;

                    if(creatures.ContainsKey(creatureAnimationPacket.creatureId)) {
                        if(creature_owner[creatureAnimationPacket.creatureId] != client.playerId) return;

                        SendReliableToAllExcept(creatureAnimationPacket, client.playerId);
                    }
                    break;

                case PacketType.CREATURE_RAGDOLL:
                    CreatureRagdollPacket creatureRagdollPacket = (CreatureRagdollPacket) p;

                    if(creatures.ContainsKey(creatureRagdollPacket.creatureId)) {
                        if(creature_owner[creatureRagdollPacket.creatureId] != client.playerId) return;

                        cnd = creatures[creatureRagdollPacket.creatureId];
                        cnd.Apply(creatureRagdollPacket);

                        SendUnreliableToAllExcept(creatureRagdollPacket, client.playerId);
                    }
                    break;

                case PacketType.CREATURE_SLICE:
                    CreatureSlicePacket creatureSlicePacket = (CreatureSlicePacket) p;

                    if(creatures.ContainsKey(creatureSlicePacket.creatureId)) {
                        SendReliableToAllExcept(creatureSlicePacket, client.playerId);
                    }
                    break;

                case PacketType.CREATURE_OWNER:
                    CreatureOwnerPacket creatureOwnerPacket = (CreatureOwnerPacket) p;

                    if(creatureOwnerPacket.creatureId > 0 && creatures.ContainsKey(creatureOwnerPacket.creatureId)) {
                        UpdateCreatureOwner(creatures[creatureOwnerPacket.creatureId], client);
                    }
                    break;
                #endregion

                #region Other Stuff
                case PacketType.DISPLAY_TEXT:
                    DisplayTextPacket displayTextPacket = (DisplayTextPacket) p;

                    break;
                #endregion
                
                default: break;
            }
        }

        internal void UpdateItemOwner(ItemNetworkData itemNetworkData, ClientData newOwner) {
            ClientData oldOwner = null;
            if(item_owner.ContainsKey(itemNetworkData.networkedId)) {
                try {
                    oldOwner = ModManager.serverInstance.clients[item_owner[itemNetworkData.networkedId]];
                }catch(Exception) { }
                item_owner[itemNetworkData.networkedId] = newOwner.playerId;
            } else {
                item_owner.Add(itemNetworkData.networkedId, newOwner.playerId);
            }

            if(oldOwner != newOwner) {
                try { if(ServerEvents.OnItemOwnerChanged != null) ServerEvents.OnItemOwnerChanged.Invoke(itemNetworkData, oldOwner, newOwner); } catch(Exception e) { Log.Err(e); }
            }
        }

        internal void UpdateCreatureOwner(CreatureNetworkData creatureNetworkData, ClientData newOwner) {
            ClientData oldOwner = null;
            if(creature_owner.ContainsKey(creatureNetworkData.networkedId)) {
                try {
                    oldOwner = ModManager.serverInstance.clients[item_owner[creatureNetworkData.networkedId]];
                } catch(Exception) { }
                if(creature_owner[creatureNetworkData.networkedId] != newOwner.playerId) {
                    creature_owner[creatureNetworkData.networkedId] = newOwner.playerId;

                    SendReliableTo(newOwner.playerId, new CreatureOwnerPacket(creatureNetworkData.networkedId, true));
                    SendReliableToAllExcept(new CreatureOwnerPacket(creatureNetworkData.networkedId, false), newOwner.playerId);

                    Log.Debug(Defines.SERVER, $"{newOwner.name} has taken ownership of creature {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId})");
                }
            } else {
                creature_owner.Add(creatureNetworkData.networkedId, newOwner.playerId);
            }

            if(oldOwner != newOwner) {
                try { if(ServerEvents.OnItemOwnerChanged != null) ServerEvents.OnCreatureOwnerChanged.Invoke(creatureNetworkData, oldOwner, newOwner); } catch(Exception e) { Log.Err(e); }
            }
        }

        internal void ClearItemsAndCreatures() {
            creatures.Clear();
            creature_owner.Clear();
            items.Clear();
            item_owner.Clear();

            SendReliableToAll(new ClearPacket(true, true));
        }

        internal void LeavePlayer(ClientData client, string reason = "Player disconnected") {
            if(client == null) return;
            if(client.disconnectThread != null && client.disconnectThread.IsAlive) return;

            client.disconnectThread = new Thread(() => LeavePlayerThread(client, reason));
            client.disconnectThread.Name = $"LeavePlayer {client.name}: {reason}";
            client.disconnectThread.Start();
        }

        private void LeavePlayerThread(ClientData client, string reason) {
            if(clients.Count <= 1) {
                ClearItemsAndCreatures();
                Log.Info(Defines.SERVER, $"Clearing server because last player disconnected.");
            } else {
                try {
                    ClientData migrateUser = clients.First(entry => entry.Value.playerId != client.playerId).Value;
                    try {
                        KeyValuePair<long, long>[] entries = item_owner.Where(entry => entry.Value == client.playerId).ToArray();

                        if(entries.Length > 0) {
                            foreach(KeyValuePair<long, long> entry in entries) {
                                if(items.ContainsKey(entry.Key)) {
                                    item_owner[entry.Key] = migrateUser.playerId;
                                    SendReliableTo(migrateUser.playerId, new ItemOwnerPacket(entry.Key, true));
                                }
                            }
                            Log.Info(Defines.SERVER, $"Migrated items from { client.name } to { migrateUser.name }.");
                        }
                    } catch(Exception e) {
                        Log.Err(Defines.SERVER, $"Couldn't migrate items from {client.name} to { migrateUser.name }.\n{e}");
                    }

                    try {
                        KeyValuePair<long, long>[] entries = creature_owner.Where(entry => entry.Value == client.playerId).ToArray();

                        if(entries.Length > 0) {
                            foreach(KeyValuePair<long, long> entry in entries) {
                                if(creatures.ContainsKey(entry.Key)) {
                                    creature_owner[entry.Key] = migrateUser.playerId;
                                    SendReliableTo(migrateUser.playerId, new CreatureOwnerPacket(entry.Key, true));
                                }
                            }
                            Log.Info(Defines.SERVER, $"Migrated creatures from {client.name} to {migrateUser.name}.");
                        }
                    } catch(Exception e) {
                        Log.Err(Defines.SERVER, $"Couldn't migrate creatures from {client.name} to {migrateUser.name}.\n{e}");
                    }
                } catch(Exception e) {
                    Log.Err(Defines.SERVER, $"Couldn't migrate stuff from { client.name } to other client.\n{e}");
                }
            }

            try {
                try {
                    try {
                        SendReliableTo(client.playerId, new DisconnectPacket(client.playerId, reason));
                    } catch { }
                    client.Disconnect();
                } catch { }
            } catch(Exception e) {
                Log.Err($"Unable to properly disconnect {client.name}: {e}");
            }
            try {
                clients.Remove(client.playerId);
            } catch(Exception e) {
                Log.Err($"Unable to remove client from list {client.name}: {e}");
            }

            SendReliableToAllExcept(new DisconnectPacket(client.playerId, reason), client.playerId);

            try { if(ServerEvents.OnPlayerQuit != null) ServerEvents.OnPlayerQuit.Invoke(client); } catch(Exception e) { Log.Err(e); }

            Log.Info(Defines.SERVER, $"{client.name} disconnected. {reason}");
        }

        // TCP
        public void SendReliableToAll(NetPacket p) {
            SendReliableToAllExcept(p);
        }

        public void SendReliableTo(long clientId, NetPacket p) {
            if(!clients.ContainsKey(clientId)) return;
            
            clients[clientId].tcp.QueuePacket(p);
        }

        public void SendReliableToAllExcept(NetPacket p, params long[] exceptions) {
            foreach(KeyValuePair<long, ClientData> client in clients.ToArray()) {
                if(exceptions.Contains(client.Key)) continue;

                SendReliableTo(client.Key, p);
            }
        }

        // UDP
        public void SendUnreliableToAll(NetPacket p) {
            SendUnreliableToAllExcept(p);
        }

        public void SendUnreliableTo(long clientId, NetPacket p) {
            if(!clients.ContainsKey(clientId)) return;

            try {
                UdpSocket udp = clients[clientId].udp;
                if(udp != null && udp.endPoint != null) {
                    byte[] data = p.GetData(true);
                    udpListener.Send(data, data.Length, clients[clientId].udp.endPoint);
                }
            } catch(Exception e) {
                Log.Err(Defines.SERVER, $"Error sending data to {clients[clientId].udp.endPoint} via UDP: {e}");
            }
        }

        public void SendUnreliableToAllExcept(NetPacket p, params long[] exceptions) {
            //p.WriteLength();
            foreach(KeyValuePair<long, ClientData> client in clients.ToArray()) {
                if(exceptions.Contains(client.Key)) continue;

                SendUnreliableTo(client.Key, p);
            }
        }
    }
}
