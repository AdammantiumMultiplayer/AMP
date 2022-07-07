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

                case (int) Packet.Type.playerData:
                    PlayerSync playerSync = new PlayerSync();
                    playerSync.ApplyConfigPacket(p);

                    if(playerSync.clientId <= 0) return;
                    if(playerSync.clientId == myClientId) {
                        #if DEBUG_SELF
                        playerSync.playerPos += Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerSync.clientId)) {
                        playerSync = ModManager.clientSync.syncData.players[playerSync.clientId];
                    } else {
                        ModManager.clientSync.syncData.players.Add(playerSync.clientId, playerSync);
                    }

                    if(playerSync.creature == null) {
                        ModManager.clientSync.SpawnPlayer(playerSync.clientId);
                    } else {
                        // Maybe allow modify? Dont know if needed, its just when height and gender are changed while connected, so no?
                    }
                    break;

                case (int) Packet.Type.playerPos:
                    playerSync = new PlayerSync();
                    playerSync.ApplyPosPacket(p);

                    if(playerSync.clientId == myClientId) {
                        #if DEBUG_SELF
                        playerSync.playerPos += Vector3.right * 2;
                        playerSync.handLeftPos += Vector3.right * 2;
                        playerSync.handRightPos += Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    ModManager.clientSync.MovePlayer(playerSync.clientId, playerSync);
                    break;

                case (int) Packet.Type.itemSpawn:
                    ItemSync itemSync = new ItemSync();
                    itemSync.ApplySpawnPacket(p);

                    if(ModManager.clientSync.syncData.itemDataMapping.ContainsKey(-itemSync.clientsideId)) { // Item has been spawned by player
                        ItemSync exisitingSync = ModManager.clientSync.syncData.itemDataMapping[-itemSync.clientsideId];
                        exisitingSync.networkedId = itemSync.networkedId;

                        if(ModManager.clientSync.syncData.itemDataMapping.ContainsKey(itemSync.networkedId))
                            ModManager.clientSync.syncData.itemDataMapping[itemSync.networkedId] = exisitingSync;
                        else
                            ModManager.clientSync.syncData.itemDataMapping.Add(itemSync.networkedId, exisitingSync);

                        ModManager.clientSync.syncData.itemDataMapping.Remove(-itemSync.clientsideId);
                    } else { // Item has been spawned by other player or already existed in session
                        if(ModManager.clientSync.syncData.itemDataMapping.ContainsKey(itemSync.networkedId)) {
                            return;
                        }

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

                case (int) Packet.Type.itemDespawn:
                    int id = p.ReadInt();

                    if(ModManager.clientSync.syncData.itemDataMapping.ContainsKey(id)) {
                        itemSync = ModManager.clientSync.syncData.itemDataMapping[id];

                        if(itemSync.clientsideItem != null) {
                            ModManager.clientSync.syncData.serverItems.Remove(itemSync.clientsideItem);
                            itemSync.clientsideItem.Despawn();
                        }
                        ModManager.clientSync.syncData.itemDataMapping.Remove(id);
                    }
                    break;

                case (int) Packet.Type.itemPos:
                    ItemSync itemPosData = new ItemSync();
                    itemPosData.ApplyPosPacket(p);

                    if(ModManager.clientSync.syncData.itemDataMapping.ContainsKey(itemPosData.networkedId)) {
                        itemSync = ModManager.clientSync.syncData.itemDataMapping[itemPosData.networkedId];

                        itemSync.position = itemPosData.position;
                        itemSync.rotation = itemPosData.rotation;
                        itemSync.velocity = itemPosData.velocity;
                        itemSync.angularVelocity = itemPosData.angularVelocity;

                        itemSync.ApplyPositionToItem();
                    }
                    break;

                default: break;
            }
        }
    }
}
