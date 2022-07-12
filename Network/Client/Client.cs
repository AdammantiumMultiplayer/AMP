using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using System;
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
            if(tcp != null) {
                tcp.SendPacket(PacketWriter.Disconnect(0, "Connection closed"));
                tcp.Disconnect();
            }
            if(udp != null) udp.Disconnect();
            Log.Info("[Client] Disconnected.");
        }

        internal void Connect() {
            Log.Info($"[Client] Connecting to {ip}:{port}...");
            tcp = new TcpSocket(ip, port);
            tcp.onPacket += OnPacket;
            udp = new UdpSocket(ip, port);
            udp.onPacket += OnPacket;

            isConnected = tcp.client.Connected;
            if(!isConnected) {
                Log.Err("[Client] Connection failed. Check ip address and ports.");
                Disconnect();
            }
        }

        void OnPacket(Packet p) {
            Packet.Type type = p.ReadType();

            //Debug.Log("[Client] Packet " + type);

            switch(type) {
                case Packet.Type.welcome:
                    myClientId = p.ReadInt();

                    udp.Connect(((IPEndPoint) tcp.client.Client.LocalEndPoint).Port);

                    Log.Debug("[Client] Assigned id " + myClientId);
                    
                    // Send some udp packets, one should reach the host if ports are free
                    Thread udpLinkThread = new Thread(() => {
                        for(int i = 0; i < 20; i++) {
                            udp.SendPacket(PacketWriter.Welcome(myClientId));
                            Thread.Sleep(100);
                        }
                    });
                    udpLinkThread.Start();
                    break;

                case Packet.Type.disconnect:
                    int playerId = p.ReadInt();

                    if(myClientId == playerId) {
                        ModManager.StopClient();
                        Log.Info("[Client] Disconnected: " + p.ReadString());
                    } else {
                        if(ModManager.clientSync.syncData.players.ContainsKey(playerId)) {
                            PlayerSync ps = ModManager.clientSync.syncData.players[playerId];
                            ModManager.clientSync.LeavePlayer(ps);
                            Log.Info($"[Client] {ps.name} disconnected: " + p.ReadString());
                        }
                    }
                    break;

                case Packet.Type.message:
                    Log.Debug("[Client] Message: " + p.ReadString());
                    break;

                case Packet.Type.error:
                    Log.Err("[Client] Error: " + p.ReadString());
                    break;

                case Packet.Type.playerData:
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

                case Packet.Type.playerPos:
                    playerSync = new PlayerSync();
                    playerSync.ApplyPosPacket(p);

                    if(playerSync.clientId == myClientId) {
                        #if DEBUG_SELF
                        playerSync.playerPos += Vector3.right * 2;
                        playerSync.handLeftPos += Vector3.right * 2;
                        playerSync.handRightPos += Vector3.right * 2;
                        playerSync.headPos += Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    ModManager.clientSync.MovePlayer(playerSync.clientId, playerSync);
                    break;

                case Packet.Type.playerEquip:
                    int clientId = p.ReadInt();

                    #if !DEBUG_SELF
                    if(clientId == myClientId) return;
                    #endif

                    playerSync = ModManager.clientSync.syncData.players[clientId];
                    playerSync.ApplyEquipmentPacket(p);
                    ModManager.clientSync.UpdateEquipment(playerSync);

                    break;

                case Packet.Type.itemSpawn:
                    ItemSync itemSync = new ItemSync();
                    itemSync.ApplySpawnPacket(p);

                    bool already_exists = false;
                    if(itemSync.clientsideId < 0) {
                        already_exists = true;
                        itemSync.clientsideId = Mathf.Abs(itemSync.clientsideId);
                    }

                    if(ModManager.clientSync.syncData.items.ContainsKey(-itemSync.clientsideId)) { // Item has been spawned by player
                        ItemSync exisitingSync = ModManager.clientSync.syncData.items[-itemSync.clientsideId];
                        exisitingSync.networkedId = itemSync.networkedId;

                        ModManager.clientSync.syncData.items.Remove(-itemSync.clientsideId);

                        if(ModManager.clientSync.syncData.items.ContainsKey(itemSync.networkedId)) { // Item has already been spawned by server before we sent it, so we can just despawn it
                            if(ModManager.clientSync.syncData.items[itemSync.networkedId] != exisitingSync) {
                                if(exisitingSync.clientsideItem != null) exisitingSync.clientsideItem.Despawn();
                            } else {
                                exisitingSync.ApplyPositionToItem();
                            }
                            return;
                        } else { // Assign item to its network Id
                            ModManager.clientSync.syncData.items.Add(itemSync.networkedId, exisitingSync);
                        }

                        if(already_exists) { // Server told us he already knows about the item, so we unset the clientsideId to make sure we dont send unnessasary position updates
                            Log.Debug($"[Client] Server knew about item {itemSync.dataId} (Local: {exisitingSync.clientsideId} - Server: {itemSync.networkedId}) already (Probably map default item).");
                            exisitingSync.clientsideId = 0; // Server had the item already known, so reset that its been spawned by the player
                        }
                        EventHandler.AddEventsToItem(exisitingSync);
                        exisitingSync.ApplyPositionToItem();
                    } else { // Item has been spawned by other player or already existed in session
                        if(ModManager.clientSync.syncData.items.ContainsKey(itemSync.networkedId)) {
                            itemSync.ApplyPositionToItem();
                            return;
                        }


                        Item item_found = SyncFunc.DoesItemAlreadyExist(itemSync, Item.allActive);
                        
                        if(item_found == null) {
                            ItemData itemData = Catalog.GetData<ItemData>(itemSync.dataId);
                            if(itemData == null) { // If the client doesnt have the item, just spawn a sword (happens when mod is not installed)
                                Log.Err($"[Client] Couldn't spawn {itemSync.dataId}, please check you mods. Instead a SwordShortCommon is used now.");
                                itemData = Catalog.GetData<ItemData>("SwordShortCommon");
                            }
                            if(itemData != null) {
                                itemData.SpawnAsync((item) => {
                                    if(ModManager.clientSync.syncData.items.ContainsKey(itemSync.networkedId) && ModManager.clientSync.syncData.items[itemSync.networkedId].clientsideItem != item) {
                                        item.Despawn();
                                        return;
                                    }

                                    itemSync.clientsideItem = item;

                                    item.disallowDespawn = true;

                                    ModManager.clientSync.syncData.items.Add(itemSync.networkedId, itemSync);
                                    Log.Debug($"[Client] Item {itemSync.dataId} ({itemSync.networkedId}) spawned from server.");

                                    EventHandler.AddEventsToItem(itemSync);
                                }, itemSync.position, Quaternion.Euler(itemSync.rotation));
                            } else {
                                Log.Err($"[Client] Couldn't spawn {itemSync.dataId}. #SNHE002");
                            }
                        } else {
                            itemSync.clientsideItem = item_found;
                            item_found.disallowDespawn = true;

                            Log.Debug($"[Client] Item {itemSync.dataId} ({itemSync.networkedId}) matched with server.");

                            EventHandler.AddEventsToItem(itemSync);
                        }
                    }
                    break;

                case Packet.Type.itemDespawn:
                    int to_despawn = p.ReadInt();

                    if(ModManager.clientSync.syncData.items.ContainsKey(to_despawn)) {
                        itemSync = ModManager.clientSync.syncData.items[to_despawn];

                        if(itemSync.clientsideItem != null) {
                            itemSync.clientsideItem.Despawn();
                        }
                        ModManager.clientSync.syncData.items.Remove(to_despawn);
                    }
                    break;

                case Packet.Type.itemPos:
                    int to_update = p.ReadInt();

                    if(ModManager.clientSync.syncData.items.ContainsKey(to_update)) {
                        itemSync = ModManager.clientSync.syncData.items[to_update];

                        itemSync.ApplyPosPacket(p);
                        itemSync.ApplyPositionToItem();
                    }
                    break;

                case Packet.Type.itemOwn:
                    int networkId = p.ReadInt();
                    bool owner = p.ReadBool();

                    if(ModManager.clientSync.syncData.items.ContainsKey(networkId)) {
                        ModManager.clientSync.syncData.items[networkId].SetOwnership(owner);
                    }
                    break;

                case Packet.Type.itemSnap:
                    networkId = p.ReadInt();

                    if(ModManager.clientSync.syncData.items.ContainsKey(networkId)) {
                        itemSync = ModManager.clientSync.syncData.items[networkId];

                        itemSync.creatureNetworkId = p.ReadInt();
                        itemSync.drawSlot = (Holder.DrawSlot) p.ReadByte();
                        itemSync.holdingSide = (Side) p.ReadByte();
                        itemSync.holderIsPlayer = p.ReadBool();

                        itemSync.UpdateHoldState();
                    }
                    break;

                case Packet.Type.itemUnSnap:
                    networkId = p.ReadInt();

                    if(ModManager.clientSync.syncData.items.ContainsKey(networkId)) {
                        itemSync = ModManager.clientSync.syncData.items[networkId];

                        itemSync.drawSlot = Holder.DrawSlot.None;
                        itemSync.creatureNetworkId = 0;
                        itemSync.holderIsPlayer = false;

                        itemSync.UpdateHoldState();
                        Log.Debug($"[Client] Unsnapped item {itemSync.dataId}.");
                    }
                    break;

                case Packet.Type.loadLevel:
                    string level = p.ReadString();
                    string mode = p.ReadString();

                    ModManager.clientSync.syncData.serverlevel = level;
                    ModManager.clientSync.syncData.servermode = mode;

                    string currentLevel = "";
                    string currentMode = "";
                    if(Level.current != null && Level.current.data != null && Level.current.data.id != null && Level.current.data.id.Length > 0) {
                        currentLevel = Level.current.data.id;
                        currentMode = Level.current.mode.name;
                    }

                    if(!(currentLevel.Equals(level, StringComparison.OrdinalIgnoreCase) && currentMode.Equals(mode, StringComparison.OrdinalIgnoreCase))) {
                        LevelData ld = Catalog.GetData<LevelData>(level);
                        if(ld != null) {
                            LevelData.Mode ldm = ld.GetMode(mode);
                            if(ldm != null) { 
                                Log.Info($"[Client] Changing to level {level} with mode {mode}.");

                                GameManager.LoadLevel(ld, ldm);
                            } else {
                                Log.Err($"[Client] Couldn't switch to level {level}. Mode {mode} not found, please check you mods.");
                            }
                        } else {
                            Log.Err($"[Client] Level {level} not found, please check you mods.");
                        }
                    }
                    break;

                case Packet.Type.creatureSpawn:
                    CreatureSync creatureSync = new CreatureSync();
                    creatureSync.ApplySpawnPacket(p);

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(-creatureSync.clientsideId)) { // Creature has been spawned by player
                        CreatureSync exisitingSync = ModManager.clientSync.syncData.creatures[-creatureSync.clientsideId];
                        exisitingSync.networkedId = creatureSync.networkedId;

                        ModManager.clientSync.syncData.creatures.Remove(-creatureSync.clientsideId);

                        ModManager.clientSync.syncData.creatures.Add(creatureSync.networkedId, exisitingSync);

                        EventHandler.AddEventsToCreature(exisitingSync);
                    } else {
                        Log.Info($"[Client] Server has summoned {creatureSync.creatureId} ({creatureSync.networkedId})");
                        ModManager.clientSync.syncData.creatures.Add(creatureSync.networkedId, creatureSync);
                        ModManager.clientSync.SpawnCreature(creatureSync);
                    }
                    break;

                case Packet.Type.creaturePos:
                    to_update = p.ReadInt();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(to_update)) {
                        creatureSync = ModManager.clientSync.syncData.creatures[to_update];
                        creatureSync.ApplyPosPacket(p);
                        creatureSync.ApplyPositionToCreature();
                    }
                    break;

                case Packet.Type.creatureHealth:
                    to_update = p.ReadInt();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(to_update)) {
                        creatureSync = ModManager.clientSync.syncData.creatures[to_update];
                        creatureSync.ApplyHealthPacket(p);
                        creatureSync.ApplyHealthToCreature();
                    }
                    break;

                case Packet.Type.creatureDespawn:
                    to_despawn = p.ReadInt();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(to_despawn)) {
                        creatureSync = ModManager.clientSync.syncData.creatures[to_despawn];

                        if(creatureSync.clientsideCreature != null) {
                            creatureSync.clientsideCreature.Despawn();
                        }
                        ModManager.clientSync.syncData.creatures.Remove(to_despawn);
                    }
                    break;

                case Packet.Type.creatureAnimation:
                    networkId = p.ReadInt();
                    int stateHash = p.ReadInt();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(networkId)) {
                        CreatureSync cs = ModManager.clientSync.syncData.creatures[networkId];
                        if(cs.clientsideCreature == null) return;

                        cs.clientsideCreature.animator.Play(stateHash, 0);
                    }
                    break;

                default: break;
            }
        }
    }
}
