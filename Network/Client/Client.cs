using AMP.Data;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.GameInteraction.Components;
using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Network.Handler;
using AMP.Network.Helper;
using AMP.Network.Packets;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using ThunderRoad;
#if DEBUG_SELF
using UnityEngine;
#endif

namespace AMP.Network.Client {
    internal class Client {
        internal long myPlayerId;
        internal bool allowTransmission = false;

        internal NetworkHandler nw;

        internal Client(NetworkHandler nw) {
            this.nw = nw;

            nw.onPacketReceived += OnPacket;
        }

        internal void OnPacket(NetPacket p) {
            Dispatcher.Enqueue(() => {
                OnPacketMainThread(p);
            });
        }

        private void OnPacketMainThread(NetPacket p) {
            PacketType type = (PacketType) p.getPacketType();

            //Log.Warn("CLIENT", type);

            switch(type) {
                #region Connection handling and stuff
                case PacketType.WELCOME:
                    WelcomePacket welcomePacket = (WelcomePacket) p;

                    if(welcomePacket.playerId > 0) { // Server send the player a client id
                        myPlayerId = welcomePacket.playerId;

                        Log.Debug(Defines.CLIENT, $"Assigned id " + myPlayerId);

                        if(nw is SocketHandler) {
                            SocketHandler sh = (SocketHandler) nw;
                            sh.udp.Connect(((IPEndPoint) sh.tcp.client.Client.LocalEndPoint).Port);
                        
                            // Send some udp packets, one should reach the host if ports are free
                            Thread udpLinkThread = new Thread(() => {
                                for(int i = 0; i < 20; i++) {
                                    sh.udp.QueuePacket(new WelcomePacket(myPlayerId));
                                    Thread.Sleep(100);
                                }
                            });
                            udpLinkThread.Name = "UdpLinker";
                            udpLinkThread.Start();
                        }
                    }
                    break;

                case PacketType.DISCONNECT:
                    DisconnectPacket disconnectPacket = (DisconnectPacket) p;

                    if(myPlayerId == disconnectPacket.playerId) {
                        Log.Info(Defines.CLIENT, $"Disconnected: " + disconnectPacket.reason);
                        ModManager.StopClient();
                    } else {
                        if(ModManager.clientSync.syncData.players.ContainsKey(disconnectPacket.playerId)) {
                            PlayerNetworkData ps = ModManager.clientSync.syncData.players[disconnectPacket.playerId];
                            ModManager.clientSync.LeavePlayer(ps);
                            Log.Info(Defines.CLIENT, $"{ps.name} disconnected: " + disconnectPacket.reason);
                        }
                    }
                    break;

                case PacketType.MESSAGE:
                    MessagePacket messagePacket = (MessagePacket) p;
                    Log.Debug(Defines.CLIENT, $"Message: " + messagePacket.message);
                    break;

                case PacketType.ERROR:
                    ErrorPacket errorPacket = (ErrorPacket) p;
                    Log.Err(Defines.CLIENT, $"Error: " + errorPacket.message);
                    ModManager.StopClient();
                    break;

                case PacketType.ALLOW_TRANSMISSION:
                    AllowTransmissionPacket allowTransmissionPacket = (AllowTransmissionPacket) p;
                    allowTransmission = allowTransmissionPacket.allow;
                    //Log.Debug(Defines.CLIENT, $"Transmission is now { (allowTransmission ? "allowed" : "disabled") }");
                    break;
                #endregion

                #region Player Packets
                case PacketType.PLAYER_DATA:
                    PlayerDataPacket playerDataPacket = (PlayerDataPacket) p;

                    if(playerDataPacket.playerId <= 0) return;
                    if(playerDataPacket.playerId == myPlayerId) {
                        #if DEBUG_SELF
                        playerDataPacket.playerPos += Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    PlayerNetworkData pnd;
                    if(ModManager.clientSync.syncData.players.ContainsKey(playerDataPacket.playerId)) {
                        pnd = ModManager.clientSync.syncData.players[playerDataPacket.playerId];
                    } else {
                        pnd = new PlayerNetworkData();
                        pnd.Apply(playerDataPacket);
                        ModManager.clientSync.syncData.players.Add(playerDataPacket.playerId, pnd);
                    }

                    Spawner.TrySpawnPlayer(pnd);
                    break;

                case PacketType.PLAYER_POSITION:
                    PlayerPositionPacket playerPositionPacket = (PlayerPositionPacket) p;

                    if(playerPositionPacket.playerId == myPlayerId) {
                        #if DEBUG_SELF
                        playerPositionPacket.position     += Vector3.right * 2;
                        playerPositionPacket.handLeftPos  += Vector3.right * 2;
                        playerPositionPacket.handRightPos += Vector3.right * 2;
                        playerPositionPacket.headPos      += Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerPositionPacket.playerId)) {
                        pnd = ModManager.clientSync.syncData.players[playerPositionPacket.playerId];
                        pnd.Apply(playerPositionPacket);
                        ModManager.clientSync.MovePlayer(pnd);
                    }
                    break;

                case PacketType.PLAYER_EQUIPMENT:
                    PlayerEquipmentPacket playerEquipmentPacket = (PlayerEquipmentPacket) p;

                    #if !DEBUG_SELF
                    if(playerEquipmentPacket.playerId == myPlayerId) return;
                    #endif

                    pnd = ModManager.clientSync.syncData.players[playerEquipmentPacket.playerId];
                    pnd.Apply(playerEquipmentPacket);

                    if(pnd.isSpawning) return;
                    CreatureEquipment.Apply(pnd);
                    break;

                case PacketType.PLAYER_RAGDOLL:
                    PlayerRagdollPacket playerRagdollPacket = (PlayerRagdollPacket) p;

                    if(playerRagdollPacket.playerId == myPlayerId) {
                        #if DEBUG_SELF
                        playerRagdollPacket.position += Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerRagdollPacket.playerId)) {
                        pnd = ModManager.clientSync.syncData.players[playerRagdollPacket.playerId];
                        pnd.Apply(playerRagdollPacket);
                        pnd.PositionChanged();
                        ModManager.clientSync.MovePlayer(pnd);
                    }
                    break;

                case PacketType.PLAYER_HEALTH_SET:
                    PlayerHealthSetPacket playerHealthSetPacket = (PlayerHealthSetPacket) p;

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerHealthSetPacket.playerId)) {
                        ModManager.clientSync.syncData.players[playerHealthSetPacket.playerId].Apply(playerHealthSetPacket);
                    }
                    break;

                case PacketType.PLAYER_HEALTH_CHANGE:
                    PlayerHealthChangePacket playerHealthChangePacket = (PlayerHealthChangePacket) p;

                    if(playerHealthChangePacket.playerId == myPlayerId) {
                        Player.currentCreature.currentHealth += playerHealthChangePacket.change;

                        try {
                            if(Player.currentCreature.currentHealth <= 0 && !Player.invincibility)
                                Player.currentCreature.Kill();
                        } catch(NullReferenceException) { }
                    }
                    break;
                #endregion

                #region Item Packets
                case PacketType.ITEM_SPAWN:
                    ItemSpawnPacket itemSpawnPacket = (ItemSpawnPacket) p;

                    bool already_existed_on_server = false;
                    if(itemSpawnPacket.clientsideId < 0) {
                        already_existed_on_server = true;
                        itemSpawnPacket.clientsideId = Math.Abs(itemSpawnPacket.clientsideId);
                    }

                    if(ModManager.clientSync.syncData.items.ContainsKey(-itemSpawnPacket.clientsideId)) { // Item has been spawned by player
                        ItemNetworkData exisitingSync = ModManager.clientSync.syncData.items[-itemSpawnPacket.clientsideId];
                        exisitingSync.networkedId = itemSpawnPacket.itemId;

                        ModManager.clientSync.syncData.items.Remove(-itemSpawnPacket.clientsideId);

                        if(ModManager.clientSync.syncData.items.ContainsKey(itemSpawnPacket.itemId)) { // Item has already been spawned by server before we sent it, so we can just despawn it
                            if(ModManager.clientSync.syncData.items[itemSpawnPacket.itemId] != exisitingSync) {
                                if(exisitingSync.clientsideItem != null) exisitingSync.clientsideItem.Despawn();
                            } else {
                                exisitingSync.ApplyPositionToItem();
                            }
                            return;
                        } else { // Assign item to its network Id
                            ModManager.clientSync.syncData.items.Add(itemSpawnPacket.itemId, exisitingSync);
                        }

                        if(already_existed_on_server) { // Server told us he already knows about the item, so we unset the clientsideId to make sure we dont send unnessasary position updates
                            Log.Debug(Defines.CLIENT, $"Server knew about item {itemSpawnPacket.type} (Local: {exisitingSync.clientsideId} - Server: {itemSpawnPacket.itemId}) already (Probably map default item).");
                            exisitingSync.clientsideId = 0; // Server had the item already known, so reset that its been spawned by the player
                        }

                        exisitingSync.StartNetworking();
                    } else { // Item has been spawned by other player or already existed in session
                        if(ModManager.clientSync.syncData.items.ContainsKey(itemSpawnPacket.itemId)) {
                            Spawner.TrySpawnItem(ModManager.clientSync.syncData.items[itemSpawnPacket.itemId]);
                            return;
                        }

                        ItemNetworkData ind = new ItemNetworkData();
                        ind.Apply(itemSpawnPacket);

                        Item item_found = SyncFunc.DoesItemAlreadyExist(ind, Item.allActive);
                        
                        if(item_found == null) {
                            Spawner.TrySpawnItem(ind);
                        } else {
                            ind.clientsideItem = item_found;
                            item_found.disallowDespawn = true;

                            Log.Debug(Defines.CLIENT, $"Item {ind.dataId} ({ind.networkedId}) matched with server.");

                            ind.StartNetworking();
                        }
                        ModManager.clientSync.syncData.items.Add(ind.networkedId, ind);
                    }
                    break;

                case PacketType.ITEM_DESPAWN:
                    ItemDespawnPacket itemDespawnPacket = (ItemDespawnPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemDespawnPacket.itemId)) {
                        ItemNetworkData ind = ModManager.clientSync.syncData.items[itemDespawnPacket.itemId];

                        if(ind.clientsideItem != null) {
                            ind.clientsideItem.Despawn();
                        }
                        ModManager.clientSync.syncData.items.Remove(itemDespawnPacket.itemId);
                    }
                    break;

                case PacketType.ITEM_POSITION:
                    ItemPositionPacket itemPositionPacket = (ItemPositionPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemPositionPacket.itemId)) {
                        ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemPositionPacket.itemId];

                        itemNetworkData.Apply(itemPositionPacket);
                        itemNetworkData.PositionChanged();

                        itemNetworkData.ApplyPositionToItem();
                    }
                    break;

                case PacketType.ITEM_OWNER:
                    ItemOwnerPacket itemOwnerPacket = (ItemOwnerPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemOwnerPacket.itemId)) {
                        ModManager.clientSync.syncData.items[itemOwnerPacket.itemId].SetOwnership(itemOwnerPacket.owning);
                    }
                    break;

                case PacketType.ITEM_SNAPPING_SNAP:
                    ItemSnapPacket itemSnapPacket = (ItemSnapPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemSnapPacket.itemId)) {
                        ItemNetworkData ind = ModManager.clientSync.syncData.items[itemSnapPacket.itemId];

                        ind.Apply(itemSnapPacket);
                        ind.UpdateHoldState();
                    }
                    break;

                case PacketType.ITEM_SNAPPING_UNSNAP:
                    ItemUnsnapPacket itemUnsnapPacket = (ItemUnsnapPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemUnsnapPacket.itemId)) {
                        ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemUnsnapPacket.itemId];

                        itemNetworkData.Apply(itemUnsnapPacket);
                        itemNetworkData.UpdateHoldState();
                    }
                    break;
                #endregion

                #region Imbues
                case PacketType.ITEM_IMBUE:
                    ItemImbuePacket itemImbuePacket = (ItemImbuePacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemImbuePacket.itemId)) {
                        ModManager.clientSync.syncData.items[itemImbuePacket.itemId].Apply(itemImbuePacket);
                    }
                    break;
                #endregion

                #region Level Changing
                case PacketType.LEVEL_CHANGE:
                    LevelChangePacket levelChangePacket = (LevelChangePacket) p;

                    // Writeback data to client cache
                    ModManager.clientSync.syncData.serverlevel   = levelChangePacket.level;
                    ModManager.clientSync.syncData.servermode    = levelChangePacket.mode;
                    ModManager.clientSync.syncData.serveroptions = levelChangePacket.option_dict;


                    string currentLevel = "";
                    string currentMode = "";
                    Dictionary<string, string> currentOptions = new Dictionary<string, string>();
                    LevelInfo.ReadLevelInfo(ref currentLevel, ref currentMode, ref currentOptions);

                    if(!(currentLevel.Equals(ModManager.clientSync.syncData.serverlevel, StringComparison.OrdinalIgnoreCase))) {
                        LevelInfo.TryLoadLevel(ModManager.clientSync.syncData.serverlevel, ModManager.clientSync.syncData.servermode, ModManager.clientSync.syncData.serveroptions);
                    } else {
                        levelChangePacket.SendToServerReliable();
                    }
                    break;
                #endregion

                #region Creature Packets
                case PacketType.CREATURE_SPAWN:
                    CreatureSpawnPacket creatureSpawnPacket = (CreatureSpawnPacket) p;
                    
                    if(creatureSpawnPacket.clientsideId > 0 && ModManager.clientSync.syncData.creatures.ContainsKey(-creatureSpawnPacket.clientsideId)) { // Creature has been spawned by player
                        CreatureNetworkData exisitingSync = ModManager.clientSync.syncData.creatures[-creatureSpawnPacket.clientsideId];
                        exisitingSync.networkedId = creatureSpawnPacket.creatureId;

                        ModManager.clientSync.syncData.creatures.Remove(-creatureSpawnPacket.clientsideId);

                        ModManager.clientSync.syncData.creatures.Add(creatureSpawnPacket.creatureId, exisitingSync);

                        exisitingSync.StartNetworking();
                    } else {
                        CreatureNetworkData cnd;
                        if(!ModManager.clientSync.syncData.creatures.ContainsKey(creatureSpawnPacket.creatureId)) { // If creature is not already there
                            cnd = new CreatureNetworkData();
                            cnd.Apply(creatureSpawnPacket);

                            Log.Info(Defines.CLIENT, $"Server has summoned {cnd.creatureType} ({cnd.networkedId})");
                            ModManager.clientSync.syncData.creatures.Add(cnd.networkedId, cnd);
                        } else {
                            cnd = ModManager.clientSync.syncData.creatures[creatureSpawnPacket.creatureId];
                        }
                        Spawner.TrySpawnCreature(cnd);
                    }
                    break;

                case PacketType.CREATURE_POSITION:
                    CreaturePositionPacket creaturePositionPacket = (CreaturePositionPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creaturePositionPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creaturePositionPacket.creatureId];
                        if(cnd.isSpawning) break;

                        cnd.Apply(creaturePositionPacket);
                        cnd.ApplyPositionToCreature();
                    }
                    break;

                case PacketType.CREATURE_HEALTH_SET:
                    CreatureHealthSetPacket creatureHealthSetPacket = (CreatureHealthSetPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureHealthSetPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureHealthSetPacket.creatureId];
                        cnd.Apply(creatureHealthSetPacket);
                        cnd.ApplyHealthToCreature();
                    }
                    break;

                case PacketType.CREATURE_HEALTH_CHANGE:
                    CreatureHealthChangePacket creatureHealthChangePacket = (CreatureHealthChangePacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureHealthChangePacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureHealthChangePacket.creatureId];
                        cnd.Apply(creatureHealthChangePacket);
                        cnd.ApplyHealthToCreature();
                    }
                    break;

                case PacketType.CREATURE_DESPAWN:
                    CreatureDepawnPacket creatureDepawnPacket = (CreatureDepawnPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureDepawnPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureDepawnPacket.creatureId];

                        if(cnd.creature != null) {
                            cnd.creature.Despawn();
                        }
                        ModManager.clientSync.syncData.creatures.Remove(creatureDepawnPacket.creatureId);
                    }
                    break;

                case PacketType.CREATURE_PLAY_ANIMATION:
                    CreatureAnimationPacket creatureAnimationPacket = (CreatureAnimationPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureAnimationPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureAnimationPacket.creatureId];
                        if(cnd.creature == null) return;

                        cnd.creature.PlayAttackAnimation(creatureAnimationPacket.animationClip);
                    }
                    break;

                case PacketType.CREATURE_RAGDOLL:
                    CreatureRagdollPacket creatureRagdollPacket = (CreatureRagdollPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureRagdollPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureRagdollPacket.creatureId];
                        if(cnd.isSpawning) break;

                        cnd.Apply(creatureRagdollPacket);
                        cnd.ApplyPositionToCreature();
                    }
                    break;

                case PacketType.CREATURE_SLICE:
                    CreatureSlicePacket creatureSlicePacket = (CreatureSlicePacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureSlicePacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureSlicePacket.creatureId];

                        RagdollPart.Type ragdollPartType = (RagdollPart.Type) creatureSlicePacket.slicedPart;

                        RagdollPart rp = cnd.creature.ragdoll.GetPart(ragdollPartType);
                        if(rp != null) {
                            cnd.creature.ragdoll.TrySlice(rp);
                        } else {
                            Log.Err(Defines.CLIENT, $"Couldn't slice off {ragdollPartType} from {creatureSlicePacket.creatureId}.");
                        }
                    }
                    break;

                case PacketType.CREATURE_OWNER:
                    CreatureOwnerPacket creatureOwnerPacket = (CreatureOwnerPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureOwnerPacket.creatureId)) {
                        ModManager.clientSync.syncData.creatures[creatureOwnerPacket.creatureId].SetOwnership(creatureOwnerPacket.owning);
                    }
                    break;
                #endregion

                #region Other Stuff
                case PacketType.CLEAR_DATA:
                    ClearPacket clearPacket = (ClearPacket) p;

                    if(ModManager.clientSync == null) return;
                    if(ModManager.clientSync.syncData == null) return;

                    if(clearPacket.clearCreatures) {
                        foreach(CreatureNetworkData cnd in ModManager.clientSync.syncData.creatures.Values.ToList()) {
                            if(cnd == null) continue;
                            if(cnd.creature == null) continue;

                            try {
                                cnd.creature?.Despawn();
                            } catch(Exception e) {
                                Log.Err(e);
                            }
                        }
                        ModManager.clientSync.syncData.creatures.Clear();
                    }
                    if(clearPacket.clearItems) {
                        foreach(ItemNetworkData ind in ModManager.clientSync.syncData.items.Values.ToList()) {
                            if(ind == null) continue;
                            if(ind.clientsideItem == null) continue;

                            try {
                                ind.clientsideItem?.Despawn();
                            } catch(Exception e) {
                                Log.Err(e);
                            }
                        }
                        ModManager.clientSync.syncData.items.Clear();
                    }

                    break;

                case PacketType.DISPLAY_TEXT:
                    DisplayTextPacket displayTextPacket = (DisplayTextPacket) p;

                    TextDisplay.ShowTextDisplay(displayTextPacket);
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
