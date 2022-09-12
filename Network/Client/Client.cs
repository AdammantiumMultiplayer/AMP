using AMP.Extension;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Handler;
using AMP.Network.Helper;
using AMP.SupportFunctions;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    public class Client {
        public long myClientId;
        public bool readyForTransmitting = false;
        
        public NetworkHandler nw;

        public Client(NetworkHandler nw) {
            this.nw = nw;

            ModManager.discordNetworking = (nw is DiscordNetworking.DiscordNetworking);

            if(ModManager.discordNetworking)
                nw.onPacketReceived += OnPacket;
            else
                nw.onPacketReceived += OnPacketMainThread;
        }

        public void OnPacket(Packet p) {
            Dispatcher.Enqueue(() => {
                OnPacketMainThread(p);
            });
        }

        private void OnPacketMainThread(Packet p) {
            Packet.Type type = p.ReadType();

            switch(type) {
                #region Connection handling and stuff
                case Packet.Type.welcome:
                    long id = p.ReadLong();

                    if(id > 0) { // Server send the player a client id
                        myClientId = id;

                        Log.Debug("[Client] Assigned id " + myClientId);

                        if(!ModManager.discordNetworking) {
                            SocketHandler sh = (SocketHandler) nw;
                            sh.udp.Connect(((IPEndPoint) sh.tcp.client.Client.LocalEndPoint).Port);
                        
                            // Send some udp packets, one should reach the host if ports are free
                            Thread udpLinkThread = new Thread(() => {
                                for(int i = 0; i < 20; i++) {
                                    sh.udp.SendPacket(PacketWriter.Welcome(myClientId));
                                    Thread.Sleep(100);
                                }
                            });
                            udpLinkThread.Start();
                        }
                    }else if(id == -1) { // Server is done with all the data sending, client is allowed to transmit now
                        readyForTransmitting = true;
                    }
                    break;

                case Packet.Type.disconnect:
                    long playerId = p.ReadLong();

                    if(myClientId == playerId) {
                        ModManager.StopClient();
                        Log.Info("[Client] Disconnected: " + p.ReadString());
                    } else {
                        if(ModManager.clientSync.syncData.players.ContainsKey(playerId)) {
                            PlayerNetworkData ps = ModManager.clientSync.syncData.players[playerId];
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
                #endregion

                #region Player Packets
                case Packet.Type.playerData:
                    PlayerNetworkData playerSync = new PlayerNetworkData();
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
                        ClientSync.SpawnPlayer(playerSync.clientId);
                    } else {
                        // Maybe allow modify? Dont know if needed, its just when height and gender are changed while connected, so no?
                    }
                    break;

                case Packet.Type.playerPos:
                    playerSync = new PlayerNetworkData();
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
                    long clientId = p.ReadLong();

                    #if !DEBUG_SELF
                    if(clientId == myClientId) return;
                    #endif

                    playerSync = ModManager.clientSync.syncData.players[clientId];
                    playerSync.ApplyEquipmentPacket(p);

                    if(playerSync.isSpawning) return;
                    ClientSync.UpdateEquipment(playerSync);

                    break;

                case Packet.Type.playerHealthChange:
                    clientId = p.ReadLong();
                    float change = p.ReadFloat();

                    if(clientId == myClientId) {
                        Player.currentCreature.currentHealth += change;

                        try {
                            if(Player.currentCreature.currentHealth <= 0)
                                Player.currentCreature.Kill();
                        } catch(NullReferenceException) { }
                    }
                    break;
                #endregion

                #region Item Packets
                case Packet.Type.itemSpawn:
                    ItemNetworkData itemSync = new ItemNetworkData();
                    itemSync.ApplySpawnPacket(p);

                    bool already_existed_on_server = false;
                    if(itemSync.clientsideId < 0) {
                        already_existed_on_server = true;
                        itemSync.clientsideId = Math.Abs(itemSync.clientsideId);
                    }

                    if(ModManager.clientSync.syncData.items.ContainsKey(-itemSync.clientsideId)) { // Item has been spawned by player
                        ItemNetworkData exisitingSync = ModManager.clientSync.syncData.items[-itemSync.clientsideId];
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

                        if(already_existed_on_server) { // Server told us he already knows about the item, so we unset the clientsideId to make sure we dont send unnessasary position updates
                            Log.Debug($"[Client] Server knew about item {itemSync.dataId} (Local: {exisitingSync.clientsideId} - Server: {itemSync.networkedId}) already (Probably map default item).");
                            exisitingSync.clientsideId = 0; // Server had the item already known, so reset that its been spawned by the player
                        }

                        exisitingSync.StartNetworking();

                        exisitingSync.ApplyPositionToItem();
                    } else { // Item has been spawned by other player or already existed in session
                        if(ModManager.clientSync.syncData.items.ContainsKey(itemSync.networkedId)) {
                            itemSync.ApplyPositionToItem();
                            return;
                        }

                        Item item_found = SyncFunc.DoesItemAlreadyExist(itemSync, Item.allActive);
                        
                        if(item_found == null) {
                            ModManager.clientSync.syncData.items.Add(itemSync.networkedId, itemSync);

                            ClientSync.SpawnItem(itemSync);
                        } else {
                            itemSync.clientsideItem = item_found;
                            item_found.disallowDespawn = true;

                            Log.Debug($"[Client] Item {itemSync.dataId} ({itemSync.networkedId}) matched with server.");

                            itemSync.StartNetworking();
                        }
                    }
                    break;

                case Packet.Type.itemDespawn:
                    long to_despawn = p.ReadLong();

                    if(ModManager.clientSync.syncData.items.ContainsKey(to_despawn)) {
                        itemSync = ModManager.clientSync.syncData.items[to_despawn];

                        if(itemSync.clientsideItem != null) {
                            itemSync.clientsideItem.Despawn();
                        }
                        ModManager.clientSync.syncData.items.Remove(to_despawn);
                    }
                    break;

                case Packet.Type.itemPos:
                    long to_update = p.ReadLong();
                    
                    if(ModManager.clientSync.syncData.items.ContainsKey(to_update)) {
                        itemSync = ModManager.clientSync.syncData.items[to_update];

                        itemSync.ApplyPosPacket(p);

                        itemSync.ApplyPositionToItem();
                    }
                    break;

                case Packet.Type.itemOwn:
                    long networkId = p.ReadLong();
                    bool owner = p.ReadBool();

                    if(ModManager.clientSync.syncData.items.ContainsKey(networkId)) {
                        ModManager.clientSync.syncData.items[networkId].SetOwnership(owner);
                    }
                    break;

                case Packet.Type.itemSnap:
                    networkId = p.ReadLong();

                    if(ModManager.clientSync.syncData.items.ContainsKey(networkId)) {
                        itemSync = ModManager.clientSync.syncData.items[networkId];

                        itemSync.creatureNetworkId = p.ReadLong();
                        itemSync.drawSlot = (Holder.DrawSlot) p.ReadByte();
                        itemSync.holdingSide = (Side) p.ReadByte();
                        itemSync.holderIsPlayer = p.ReadBool();

                        itemSync.UpdateHoldState();
                    }
                    break;

                case Packet.Type.itemUnSnap:
                    networkId = p.ReadLong();

                    if(ModManager.clientSync.syncData.items.ContainsKey(networkId)) {
                        itemSync = ModManager.clientSync.syncData.items[networkId];

                        itemSync.drawSlot = Holder.DrawSlot.None;
                        itemSync.creatureNetworkId = 0;
                        itemSync.holderIsPlayer = false;

                        itemSync.UpdateHoldState();
                        Log.Debug($"[Client] Unsnapped item {itemSync.dataId}.");
                    }
                    break;
                #endregion

                #region Imbues
                case Packet.Type.itemImbue:
                    to_update = p.ReadLong();

                    if(ModManager.clientSync.syncData.items.ContainsKey(to_update)) {
                        ModManager.clientSync.syncData.items[to_update].ApplyImbuePacket(p);
                    }
                    break;
                #endregion

                #region Level Changing
                case Packet.Type.loadLevel:
                    string level = p.ReadString();
                    string mode = p.ReadString();

                    // Writeback data to client cache
                    ModManager.clientSync.syncData.serverlevel = level;
                    ModManager.clientSync.syncData.servermode = mode;

                    Dictionary<string, string> options = new Dictionary<string, string>();
                    int count = p.ReadInt();
                    while(count > 0) {
                        options.Add(p.ReadString(), p.ReadString());
                        count--;
                    }
                    ModManager.clientSync.syncData.serveroptions = options;


                    string currentLevel = "";
                    string currentMode = "";
                    Dictionary<string, string> currentOptions = new Dictionary<string, string>();
                    LevelInfo.ReadLevelInfo(ref currentLevel, ref currentMode, ref currentOptions);

                    if(!(currentLevel.Equals(level, StringComparison.OrdinalIgnoreCase))) {
                        LevelInfo.TryLoadLevel(level, mode, options);
                    } else {
                        if(!readyForTransmitting) {
                            PacketWriter.LoadLevel("", "", null).SendToServerReliable();
                        }
                    }
                    break;
                #endregion

                #region Creature Packets
                case Packet.Type.creatureSpawn:
                    Data.Sync.CreatureNetworkData creatureSync = new Data.Sync.CreatureNetworkData();
                    creatureSync.ApplySpawnPacket(p);

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(-creatureSync.clientsideId)) { // Creature has been spawned by player
                        CreatureNetworkData exisitingSync = ModManager.clientSync.syncData.creatures[-creatureSync.clientsideId];
                        exisitingSync.networkedId = creatureSync.networkedId;

                        ModManager.clientSync.syncData.creatures.Remove(-creatureSync.clientsideId);

                        ModManager.clientSync.syncData.creatures.Add(creatureSync.networkedId, exisitingSync);

                        exisitingSync.StartNetworking();
                    } else {
                        if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureSync.networkedId)) return; // If creature is already there, just ignore

                        Log.Info($"[Client] Server has summoned {creatureSync.creatureId} ({creatureSync.networkedId})");
                        ModManager.clientSync.syncData.creatures.Add(creatureSync.networkedId, creatureSync);
                        ModManager.clientSync.SpawnCreature(creatureSync);
                    }
                    break;

                case Packet.Type.creaturePos:
                    to_update = p.ReadLong();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(to_update)) {
                        creatureSync = ModManager.clientSync.syncData.creatures[to_update];
                        creatureSync.ApplyPosPacket(p);
                        creatureSync.ApplyPositionToCreature();
                    }
                    break;

                case Packet.Type.creatureHealth:
                    to_update = p.ReadLong();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(to_update)) {
                        creatureSync = ModManager.clientSync.syncData.creatures[to_update];
                        creatureSync.ApplyHealthPacket(p);
                        creatureSync.ApplyHealthToCreature();
                    }
                    break;

                case Packet.Type.creatureHealthChange:
                    to_update = p.ReadLong();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(to_update)) {
                        creatureSync = ModManager.clientSync.syncData.creatures[to_update];
                        change = p.ReadFloat();
                        creatureSync.ApplyHealthChange(change);
                        creatureSync.ApplyHealthToCreature();
                    }
                    break;

                case Packet.Type.creatureDespawn:
                    to_despawn = p.ReadLong();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(to_despawn)) {
                        creatureSync = ModManager.clientSync.syncData.creatures[to_despawn];

                        if(creatureSync.clientsideCreature != null) {
                            creatureSync.clientsideCreature.Despawn();
                        }
                        ModManager.clientSync.syncData.creatures.Remove(to_despawn);
                    }
                    break;

                case Packet.Type.creatureAnimation:
                    networkId = p.ReadLong();
                    int stateHash = p.ReadInt();
                    string clipName = p.ReadString();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(networkId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[networkId];
                        if(cnd.clientsideCreature == null) return;

                        //cs.clientsideCreature.SetAnimatorBusy(true);
                        //cs.clientsideCreature.isPlayingDynamicAnimation = true;
                        
                        cnd.clientsideCreature.PlayAttackAnimation(clipName);

                        //cs.clientsideCreature.animator.Play(stateHash, 6);

                        //Debug.Log($"Trying to play " + clipName + " animation.");
                    }
                    break;

                case Packet.Type.creatureRagdoll:
                    networkId = p.ReadLong();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(networkId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[networkId];
                        cnd.ApplyRagdollPacket(p);
                    }
                    break;

                case Packet.Type.creatureSlice:
                    networkId = p.ReadLong();

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(networkId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[networkId];

                        RagdollPart.Type ragdollPartType = (RagdollPart.Type) p.ReadInt();

                        RagdollPart rp = cnd.clientsideCreature.ragdoll.GetPart(ragdollPartType);
                        if(rp != null) {
                            cnd.clientsideCreature.ragdoll.TrySlice(rp);
                        } else {
                            Log.Err($"Couldn't slice off {ragdollPartType} from {networkId}.");
                        }
                    }
                    break;
                #endregion

                default: break;
            }
        }

        internal void Disconnect() {
            if(nw != null) nw.Disconnect();
        }
    }
}
