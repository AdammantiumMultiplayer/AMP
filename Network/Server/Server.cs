using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Server {
    public class Server {
        internal bool isRunning = false;

        private int maxClients = 4;
        private int port = 13698;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public string currentLevel = null;
        public string currentMode = null;

        private Dictionary<int, ClientData> clients = new Dictionary<int, ClientData>();
        private Dictionary<string, int> endPointMapping = new Dictionary<string, int>();

        private int currentItemId = 1;
        public Dictionary<int, ItemSync> items = new Dictionary<int, ItemSync>();
        public Dictionary<int, int> item_owner = new Dictionary<int, int>();
        private int currentCreatureId = 1;
        public Dictionary<int, CreatureSync> creatures = new Dictionary<int, CreatureSync>();

        public int connectedClients {
            get { return clients.Count; }
        }
        public int spawnedItems {
            get { return items.Count; }
        }
        public int spawnedCreatures {
            get { return creatures.Count; }
        }

        public Server(int maxClients, int port) {
            this.maxClients = maxClients;
            this.port = port;
        }

        internal void Stop() {
            Log.Info("[Server] Stopping server...");

            foreach(ClientData clientData in clients.Values) {
                clientData.tcp.SendPacket(PacketWriter.Disconnect(clientData.playerId, "Server closed"));
            }

            tcpListener.Stop();
            udpListener.Dispose();

            tcpListener = null;
            udpListener = null;

            Log.Info("[Server] Server stopped.");
        }

        internal void Start() {
            Log.Info("[Server] Starting server...");

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TCPRequestCallback, null);

            udpListener = new UdpClient(port);
            udpListener.BeginReceive(UDPRequestCallback, null);

            if(Level.current != null && Level.current.data != null && Level.current.data.id != null && Level.current.data.id.Length > 0) {
                currentLevel = Level.current.data.id;
                currentMode = Level.current.mode.name;
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

            cd.tcp.SendPacket(PacketWriter.Welcome(cd.playerId));

            // Send all player data to the new client
            foreach(ClientData other_client in clients.Values) {
                if(other_client.playerSync == null) continue;
                cd.tcp.SendPacket(other_client.playerSync.CreateConfigPacket());
                cd.tcp.SendPacket(other_client.playerSync.CreateEquipmentPacket());
            }

            // Send all spawned creatures to the client
            foreach(KeyValuePair<int, CreatureSync> entry in creatures) {
                cd.tcp.SendPacket(entry.Value.CreateSpawnPacket());
            }

            // Send all spawned items to the client
            foreach(KeyValuePair<int, ItemSync> entry in items) {
                cd.tcp.SendPacket(entry.Value.CreateSpawnPacket());
                if(entry.Value.creatureNetworkId > 0) {
                    cd.tcp.SendPacket(entry.Value.SnapItemPacket());
                }
            }

            clients.Add(cd.playerId, cd);

            Log.Debug("[Server] Welcoming player " + cd.playerId);
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
                        int clientId = packet.ReadInt();

                        // If no udp is connected, then link up
                        if(clients[clientId].udp == null) {
                            clients[clientId].udp = new UdpSocket(clientEndPoint);
                            endPointMapping.Add(clientEndPoint.ToString(), clientId);
                            clients[clientId].udp.onPacket += (p) => {
                                OnPacket(clients[clientId], p);
                            };

                            if(currentLevel.Length > 0) {
                                clients[clientId].tcp.SendPacket(PacketWriter.LoadLevel(currentLevel, currentMode));
                            }

                            Log.Debug("[Server] Linked UDP for " + clientId);
                            return;
                        }
                    }
                }

                // Determine client id by EndPoint
                if(endPointMapping.ContainsKey(clientEndPoint.ToString())) {
                    int clientId = endPointMapping[clientEndPoint.ToString()];
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

        void OnPacket(ClientData client, Packet p) {
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
                    endPointMapping.Remove(client.udp.endPoint.ToString());
                    clients.Remove(client.playerId);
                    client.Disconnect();
                    Log.Info($"[Server] {client.name} disconnected.");

                    SendReliableToAll(PacketWriter.Disconnect(client.playerId, "Player disconnected"));
                    break;

                case Packet.Type.playerData:
                    if(client.playerSync == null) client.playerSync = new PlayerSync() { clientId = client.playerId };
                    client.playerSync.ApplyConfigPacket(p);

                    client.playerSync.name = Regex.Replace(client.playerSync.name, @"[^\u0000-\u007F]+", string.Empty);

                    client.playerSync.clientId = client.playerId;
                    client.name = client.playerSync.name;
                    Log.Info($"[Server] Player {client.name} ({client.playerId}) joined the server.");

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(client.playerSync.CreateConfigPacket());//, client.playerId);
                    #else
                    SendReliableToAllExcept(client.playerSync.CreateConfigPacket(), client.playerId);
                    #endif
                    break;

                case Packet.Type.playerPos:
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
                    p.ReadInt(); // Flush the id

                    client.playerSync.ApplyEquipmentPacket(p);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendReliableToAll(client.playerSync.CreateEquipmentPacket());
                    #else
                    SendReliableToAllExcept(client.playerSync.CreateEquipmentPacket(), client.playerId);
                    #endif
                    break;

                case Packet.Type.itemSpawn:
                    ItemSync itemSync = new ItemSync();
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

                    client.tcp.SendPacket(itemSync.CreateSpawnPacket());

                    if(was_duplicate) return; // If it was a duplicate, dont send it to other players

                    itemSync.clientsideId = 0;
                    
                    SendReliableToAllExcept(itemSync.CreateSpawnPacket(), client.playerId);
                    break;

                case Packet.Type.itemDespawn:
                    int to_despawn = p.ReadInt();

                    if(items.ContainsKey(to_despawn)) {
                        itemSync = items[to_despawn];

                        Log.Debug($"[Server] {client.name} has despawned item {itemSync.dataId} ({itemSync.networkedId})");

                        SendReliableToAllExcept(itemSync.DespawnPacket(), client.playerId);

                        items.Remove(to_despawn);
                        if(item_owner.ContainsKey(to_despawn)) item_owner.Remove(to_despawn);
                    }

                    break;

                case Packet.Type.itemPos:
                    int to_update = p.ReadInt();

                    if(ModManager.clientSync.syncData.items.ContainsKey(to_update)) {
                        itemSync = ModManager.clientSync.syncData.items[to_update];

                        itemSync.ApplyPosPacket(p);

                        SendUnreliableToAllExcept(itemSync.CreatePosPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.itemOwn:
                    int networkId = p.ReadInt();

                    if(networkId > 0 && items.ContainsKey(networkId)) {
                        UpdateItemOwner(items[networkId], client.playerId);

                        client.tcp.SendPacket(PacketWriter.SetItemOwnership(networkId, true));
                        SendReliableToAllExcept(PacketWriter.SetItemOwnership(networkId, false), client.playerId);
                    }
                    break;

                case Packet.Type.itemSnap:
                    networkId = p.ReadInt();

                    if(networkId > 0 && items.ContainsKey(networkId)) {
                        itemSync = items[networkId];
                        itemSync.creatureNetworkId = p.ReadInt();
                        itemSync.drawSlot = (Holder.DrawSlot) p.ReadByte();
                        itemSync.holdingSide = (Side) p.ReadByte();
                        itemSync.holderIsPlayer = p.ReadBool();

                        Log.Debug($"[Server] Snapped item {itemSync.dataId} to {itemSync.creatureNetworkId} to { (itemSync.drawSlot == Holder.DrawSlot.None ? "hand " + itemSync.holdingSide : "slot " + itemSync.drawSlot) }.");
                        SendReliableToAllExcept(itemSync.SnapItemPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.itemUnSnap:
                    networkId = p.ReadInt();

                    if(networkId > 0 && items.ContainsKey(networkId)) {
                        itemSync = items[networkId];
                        Log.Debug($"[Server] Unsnapped item {itemSync.dataId} to {itemSync.creatureNetworkId} to {(itemSync.drawSlot == Holder.DrawSlot.None ? "hand " + itemSync.holdingSide : "slot " + itemSync.drawSlot)}.");

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

                        Log.Info($"[Server] Client { client.playerId } loaded level { level } with mode {mode}.");
                        SendReliableToAllExcept(PacketWriter.LoadLevel(currentLevel, currentMode), client.playerId);
                    }
                    break;

                case Packet.Type.creatureSpawn:
                    CreatureSync creatureSync = new CreatureSync();
                    creatureSync.ApplySpawnPacket(p);

                    if(creatureSync.networkedId > 0) return;

                    creatureSync.networkedId = currentCreatureId++;

                    creatures.Add(creatureSync.networkedId, creatureSync);
                    Log.Debug($"[Server] {client.name} has summoned {creatureSync.creatureId} ({creatureSync.networkedId})");

                    client.tcp.SendPacket(creatureSync.CreateSpawnPacket());

                    creatureSync.clientsideId = 0;

                    SendReliableToAllExcept(creatureSync.CreateSpawnPacket(), client.playerId);
                    break;


                case Packet.Type.creaturePos:
                    to_update = p.ReadInt();

                    if(creatures.ContainsKey(to_update)) {
                        creatureSync = creatures[to_update];
                        creatureSync.ApplyPosPacket(p);

                        SendUnreliableToAllExcept(creatureSync.CreatePosPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.creatureHealth:
                    to_update = p.ReadInt();

                    if(creatures.ContainsKey(to_update)) {
                        creatureSync = creatures[to_update];
                        creatureSync.ApplyHealthPacket(p);

                        SendReliableToAllExcept(creatureSync.CreateHealthPacket(), client.playerId);
                    }
                    break;

                case Packet.Type.creatureDespawn:
                    to_despawn = p.ReadInt();

                    if(creatures.ContainsKey(to_despawn)) {
                        creatureSync = creatures[to_despawn];

                        Log.Debug($"[Server] {client.name} has despawned creature {creatureSync.creatureId} ({creatureSync.networkedId})");
                        SendReliableToAllExcept(creatureSync.CreateDespawnPacket(), client.playerId);

                        creatures.Remove(to_despawn);
                    }
                    break;

                case Packet.Type.creatureAnimation:
                    networkId = p.ReadInt();
                    int stateHash = p.ReadInt();

                    if(creatures.ContainsKey(networkId)) {
                        SendReliableToAllExcept(PacketWriter.CreatureAnimation(networkId, stateHash), client.playerId);
                    }
                    break;

                default: break;
            }
        }

        public void UpdateItemOwner(ItemSync itemSync, int playerId) {
            if(item_owner.ContainsKey(itemSync.networkedId)) {
                item_owner[itemSync.networkedId] = playerId;
            } else {
                item_owner.Add(itemSync.networkedId, playerId);
            }
        }

        // TCP
        public void SendReliableToAll(Packet p) {
            SendReliableToAllExcept(p);
        }

        public void SendReliableToAllExcept(Packet p, params int[] exceptions) {
            foreach(KeyValuePair<int, ClientData> client in clients) {
                if(exceptions.Contains(client.Key)) continue;
                client.Value.tcp.SendPacket(p);
            }
        }

        // UDP
        public void SendUnreliableToAll(Packet p) {
            SendUnreliableToAllExcept(p);
        }

        public void SendUnreliableToAllExcept(Packet p, params int[] exceptions) {
            p.WriteLength();
            foreach(KeyValuePair<int, ClientData> client in clients) {
                if(exceptions.Contains(client.Key)) continue;

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
