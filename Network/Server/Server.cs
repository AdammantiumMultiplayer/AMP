using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Network.Server {
    public class Server {
        internal bool isRunning = false;

        private int maxClients = 4;
        private int port = 13698;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        private Dictionary<int, ClientData> clients = new Dictionary<int, ClientData>();
        private Dictionary<string, int> endPointMapping = new Dictionary<string, int>();

        private int currentItemId = 1;
        private Dictionary<int, ItemSync> items = new Dictionary<int, ItemSync>();

        public int connectedClients {
            get { return clients.Count; }
        }
        public int spawnedItems {
            get { return items.Count; }
        }

        public Server(int maxClients, int port) {
            this.maxClients = maxClients;
            this.port = port;
        }

        internal void Stop() {
            Debug.Log("[Server] Stopping server...");

            tcpListener.Stop();
            udpListener.Dispose();

            tcpListener = null;
            udpListener = null;

            Debug.Log("[Server] Server stopped.");
        }

        internal void Start() {
            Debug.Log("[Server] Starting server...");

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

            udpListener = new UdpClient(port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            isRunning = true;
            Debug.Log("[Server] Server started.");
        }

        private int playerId = 1;

        private void TCPConnectCallback(IAsyncResult _result) {
            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(_result);
            tcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);
            Debug.Log($"[Server] Incoming connection from {tcpClient.Client.RemoteEndPoint}...");

            TcpSocket socket = new TcpSocket(tcpClient);

            if(connectedClients >= maxClients) {
                Debug.Log("[Server] Client tried to join full server.");
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

            foreach(ClientData other_client in clients.Values) {
                if(other_client.playerSync == null) continue;
                cd.tcp.SendPacket(other_client.playerSync.CreateConfigPacket());
            }

            clients.Add(cd.playerId, cd);

            Debug.Log("[Server] Welcoming player " + cd.playerId);
        }

        private void UDPReceiveCallback(IAsyncResult _result) {
            try {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(_result, ref clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if(data.Length < 8) return;

                // Check if its a welcome package and if the user is not linked up
                using(Packet packet = new Packet(data)) {
                    packet.ReadInt(); // Flush length away
                    int packetType = packet.ReadInt();
                    if(packetType == (int) Packet.Type.welcome) {
                        int clientId = packet.ReadInt();

                        // If no udp is connected, then link up
                        if(clients[clientId].udp == null) {
                            clients[clientId].udp = new UdpSocket(clientEndPoint);
                            endPointMapping.Add(clientEndPoint.ToString(), clientId);
                            clients[clientId].udp.onPacket += (p) => {
                                OnPacket(clients[clientId], p);
                            };
                            Debug.Log("[Server] Linked UDP for " + clientId);
                            return;
                        }
                    }
                }

                // Determine client id by EndPoint
                if(endPointMapping.ContainsKey(clientEndPoint.ToString())) {
                    int clientId = endPointMapping[clientEndPoint.ToString()];
                    if(!clients.ContainsKey(clientId)) {
                        Debug.Log("[Server] This should not happen... #SNHE002"); // SNHE = Should not happen error
                    } else {
                        if(clients[clientId].udp.endPoint.ToString() == clientEndPoint.ToString()) {
                            clients[clientId].udp.HandleData(new Packet(data));
                        }
                    }
                } else {
                    Debug.Log("[Server] Invalid UDP client tried to connect " + clientEndPoint.ToString());
                }
            } catch(Exception e) {
                Debug.Log($"[Server] Error receiving UDP data: {e}");
            }
        }

        void OnPacket(ClientData client, Packet p) {
            int type = p.ReadInt();

            //Debug.Log("[Server] Packet " + type + " from " + client.playerId);

            switch(type) {
                case (int) Packet.Type.message:
                    Debug.Log($"[Server] Message from {client.name}: {p.ReadString()}");
                    break;

                case (int) Packet.Type.disconnect:
                    endPointMapping.Remove(client.udp.endPoint.ToString());
                    clients.Remove(client.playerId);
                    client.Disconnect();
                    Debug.Log($"[Server] {client.name} disconnected.");
                    break;

                case (int) Packet.Type.playerData:
                    if(client.playerSync == null) client.playerSync = new PlayerSync() { clientId = client.playerId };
                    client.playerSync.ApplyConfigPacket(p);

                    client.playerSync.clientId = client.playerId;
                    Debug.Log("[Server] Received player data for " + client.playerId);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendUnreliableToAllExcept(client.playerSync.CreateConfigPacket());//, client.playerId);
                    #else
                    SendUnreliableToAllExcept(client.playerSync.CreateConfigPacket(), client.playerId);
                    #endif
                    break;

                case (int) Packet.Type.playerPos:
                    client.playerSync.ApplyPosPacket(p);
                    client.playerSync.clientId = client.playerId;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    SendUnreliableToAllExcept(client.playerSync.CreatePosPacket());//, client.playerId);
                    #else
                    SendUnreliableToAllExcept(client.playerSync.CreatePosPacket(), client.playerId);
                    #endif
                    break;

                case (int) Packet.Type.itemSpawn:
                    ItemSync itemSync = new ItemSync();
                    itemSync.RestoreSpawnPacket(p);

                    


                    itemSync.networkedId = currentItemId++;

                    items.Add(itemSync.networkedId, itemSync);

                    client.tcp.SendPacket(itemSync.CreateSpawnPacket());

                    itemSync.clientsideId = 0;

                    Debug.Log("[Server] " + client.name + " has spawned " + itemSync.dataId);
                    
                    SendReliableToAllExcept(itemSync.CreateSpawnPacket(), client.playerId);
                    break;

                case (int) Packet.Type.itemDespawn:
                    int to_despawn = p.ReadInt();

                    if(items.ContainsKey(to_despawn)) {
                        itemSync = items[to_despawn];
                        
                        SendReliableToAllExcept(itemSync.DespawnPacket(), client.playerId);

                        items.Remove(to_despawn);
                    }

                    break;

                default: break;
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
                    }
                } catch(Exception e) {
                    Debug.Log($"Error sending data to {client.Value.udp.endPoint} via UDP: {e}");
                }
            }
        }
    }
}
