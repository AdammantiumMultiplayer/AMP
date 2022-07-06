using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using System.Net;
using System.Threading;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    public class Client {
        internal bool isConnected = false;
        private string ip;
        private int port;

        public int myClientId;
        public TcpSocket tcp;
        public UdpSocket udp;

        public Client(string address, int port) {
            this.ip = NetworkUtil.GetIP(address);
            this.port = port;
        }

        internal void Disconnect() {
            isConnected = false;
            if(tcp != null) tcp.Disconnect();
            if(udp != null) udp.Disconnect();
            Debug.Log("[Client] Disconnected.");
        }

        internal void Connect() {
            Debug.Log($"[Client] Connecting to {ip}:{port}...");
            tcp = new TcpSocket(ip, port);
            tcp.onPacket += OnPacket;
            udp = new UdpSocket(ip, port);
            udp.onPacket += OnPacket;

            isConnected = tcp.client.Connected;
            if(!isConnected) {
                Debug.Log("[Client] Connection failed. Check ip address and ports.");
                Disconnect();
            }
        }

        void OnPacket(Packet p) {
            int type = p.ReadInt();

            //Debug.Log("[Client] Packet " + type);

            switch(type) {
                case (int) Packet.Type.welcome:
                    myClientId = p.ReadInt();

                    udp.Connect(((IPEndPoint) tcp.client.Client.LocalEndPoint).Port);

                    Debug.Log("[Client] Assigned id " + myClientId);
                    udp.SendPacket(PacketWriter.Welcome(myClientId));

                    // TODO: Remove, just a test if packets are send
                    //Thread test = new Thread(() => {
                    //    for(int i = 0; i < 10; i++) {
                    //        Thread.Sleep(1000);
                    //        tcp.SendPacket(PacketWriter.Message("MEEP"));
                    //    }
                    //});
                    //test.Start();


                    break;

                case (int) Packet.Type.message:
                    Debug.Log("[Client] Message: " + p.ReadString());
                    break;

                case (int) Packet.Type.error:
                    Debug.Log("[Client] Error: " + p.ReadString());
                    break;

                case (int) Packet.Type.itemSpawn:
                    ItemSync itemSync = new ItemSync();
                    itemSync.RestoreSpawnPacket(p);

                    if(ModManager.clientSync.syncData.itemDataMapping.ContainsKey(-itemSync.clientsideId)) { // Item has been spawned by player
                        ItemSync exisitingSync = ModManager.clientSync.syncData.itemDataMapping[-itemSync.clientsideId];
                        exisitingSync.networkedId = itemSync.networkedId;

                        ModManager.clientSync.syncData.itemDataMapping.Add(itemSync.networkedId, exisitingSync);
                        ModManager.clientSync.syncData.itemDataMapping.Remove(-itemSync.clientsideId);
                    } else { // Item has been spawned by other player
                        ThunderRoad.ItemData itemData = Catalog.GetData<ThunderRoad.ItemData>(itemSync.dataId);
                        if(itemData != null) {
                            itemData.SpawnAsync((item) => {
                                itemSync.clientsideItem = item;

                                ModManager.clientSync.syncData.serverItems.Add(item);
                                ModManager.clientSync.syncData.itemDataMapping.Add(itemSync.networkedId, itemSync);
                            }, itemSync.position, Quaternion.Euler(itemSync.rotation));
                        }
                    }
                    break;

                case (int) Packet.Type.playerData:
                    PlayerSync playerSync = new PlayerSync();
                    playerSync.ApplyConfigPacket(p);

                    if(playerSync.clientId <= 0 || playerSync.clientId == myClientId) return;

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerSync.clientId)) {
                        playerSync = ModManager.clientSync.syncData.players[playerSync.clientId];
                    } else {
                        ModManager.clientSync.syncData.players.Add(playerSync.clientId, playerSync);
                    }

                    if(playerSync.creature == null) {
                        ModManager.clientSync.SpawnPlayer(playerSync.clientId);
                    } else {
                        // Maybe modify? Dont know if needed, its just when height and gender are changed while connected
                    }
                    break;

                case (int) Packet.Type.playerPos:
                    playerSync = new PlayerSync();
                    playerSync.ApplyPosPacket(p);

                    ModManager.clientSync.MovePlayer(playerSync.clientId, p);
                    break;

                default: break;
            }
        }
    }
}
