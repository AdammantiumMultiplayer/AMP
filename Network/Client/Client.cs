using AMP.Data;
using AMP.Discord;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.GameInteraction.Components;
using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;
using PacketType = AMP.Network.Packets.PacketType;
#if DEBUG_SELF
#endif

namespace AMP.Network.Client {
    internal class Client {
        internal bool allowTransmission = false;

        internal NetamiteClient netclient;

        public ServerInfoPacket serverInfo = new ServerInfoPacket();

        internal Client(NetamiteClient netclient) {
            this.netclient = netclient;

            netclient.OnDataReceived += OnPacket;
        }

        internal void Connect(string password) {
            netclient.Connect(password);
        }

        internal void OnPacket(NetPacket p) {
            Dispatcher.Enqueue(() => {
                OnPacketMainThread(p);
            });
        }

        private void OnPacketMainThread(NetPacket p) {
            if(p == null) return;

            byte type = p.getPacketType();

            //Log.Warn("CLIENT", type);

            switch(type) {
                #region Connection handling and stuff
                case (byte) Netamite.Network.Packet.PacketType.DISCONNECT:
                    DisconnectPacket disconnectPacket = (DisconnectPacket) p;

                    if(netclient.ClientId == disconnectPacket.ClientId) { // Should never really happen, and be handled by the netamite onDisconnect Event
                        Log.Info(Defines.CLIENT, $"Disconnected: " + disconnectPacket.Reason);
                        ModManager.StopClient();
                    } else {
                        if(ModManager.clientSync.syncData.players.ContainsKey(disconnectPacket.ClientId)) {
                            PlayerNetworkData ps = ModManager.clientSync.syncData.players[disconnectPacket.ClientId];
                            ModManager.clientSync.LeavePlayer(ps);
                            Log.Info(Defines.CLIENT, $"{ps.name} disconnected: " + disconnectPacket.Reason);
                        }
                    }
                    break;

                case (byte) PacketType.ALLOW_TRANSMISSION:
                    AllowTransmissionPacket allowTransmissionPacket = (AllowTransmissionPacket) p;
                    allowTransmission = allowTransmissionPacket.allow;
                    Log.Debug(Defines.CLIENT, $"Transmission is now { (allowTransmission ? "en" : "dis") }abled");
                    break;
                #endregion

                #region Player Packets
                case (byte) PacketType.PLAYER_DATA:
                    PlayerDataPacket playerDataPacket = (PlayerDataPacket) p;

                    if(playerDataPacket.clientId <= 0) return;
                    if(playerDataPacket.clientId == netclient.ClientId) {
                        #if DEBUG_SELF
                        playerDataPacket.playerPos += Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    PlayerNetworkData pnd;
                    if(ModManager.clientSync.syncData.players.ContainsKey(playerDataPacket.clientId)) {
                        pnd = ModManager.clientSync.syncData.players[playerDataPacket.clientId];
                    } else {
                        pnd = new PlayerNetworkData();
                        ModManager.clientSync.syncData.players.TryAdd(playerDataPacket.clientId, pnd);
                    }
                    pnd.Apply(playerDataPacket);

                    Spawner.TrySpawnPlayer(pnd);

                    DiscordIntegration.Instance.UpdateActivity();
                    break;

                case (byte) PacketType.PLAYER_POSITION:
                    PlayerPositionPacket playerPositionPacket = (PlayerPositionPacket) p;

                    if(playerPositionPacket.playerId == netclient.ClientId) {
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
                        pnd.PositionChanged();
                        ModManager.clientSync.MovePlayer(pnd);
                    }
                    break;

                case (byte) PacketType.PLAYER_EQUIPMENT:
                    PlayerEquipmentPacket playerEquipmentPacket = (PlayerEquipmentPacket) p;

                    #if !DEBUG_SELF
                    if(playerEquipmentPacket.clientId == netclient.ClientId) return;
                    #endif

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerEquipmentPacket.clientId)) {
                        pnd = ModManager.clientSync.syncData.players[playerEquipmentPacket.clientId];
                    } else {
                        pnd = new PlayerNetworkData();
                        ModManager.clientSync.syncData.players.TryAdd(playerEquipmentPacket.clientId, pnd);
                    }
                    pnd.Apply(playerEquipmentPacket);

                    if(pnd.isSpawning) return;
                    if(pnd.clientId <= 0) return; // No player data received yet
                    CreatureEquipment.Apply(pnd);
                    break;

                case (byte) PacketType.PLAYER_RAGDOLL:
                    PlayerRagdollPacket playerRagdollPacket = (PlayerRagdollPacket) p;

                    if(playerRagdollPacket.playerId == netclient.ClientId) {
                        #if DEBUG_SELF
                        playerRagdollPacket.position += -Vector3.right * 2;
                        #else
                        return;
                        #endif
                    }

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerRagdollPacket.playerId)) {
                        pnd = ModManager.clientSync.syncData.players[playerRagdollPacket.playerId];

                        // Do our prediction
                        float compensationFactor       = NetworkData.GetCompensationFactor(playerRagdollPacket.timestamp);

                        if(compensationFactor > 0.03f) {
                            Vector3[] estimatedRagdollPos = playerRagdollPacket.ragdollPositions;
                            Quaternion[] estimatedRagdollRotation = playerRagdollPacket.ragdollRotations;
                            Vector3 estimatedPlayerPos = playerRagdollPacket.position;
                            float estimatedPlayerRot = playerRagdollPacket.rotationY;

                            estimatedPlayerPos += playerRagdollPacket.velocity * compensationFactor;
                            estimatedPlayerRot += playerRagdollPacket.rotationYVel * compensationFactor;
                            for(int i = 0; i < estimatedRagdollPos.Length; i++) {
                                estimatedRagdollPos[i] += playerRagdollPacket.velocities[i] * compensationFactor;
                            }
                            for(int i = 0; i < estimatedRagdollRotation.Length; i++) {
                                estimatedRagdollRotation[i].eulerAngles += playerRagdollPacket.angularVelocities[i] * compensationFactor;
                            }
                            playerRagdollPacket.position = estimatedPlayerPos;
                            playerRagdollPacket.rotationY = estimatedPlayerRot;
                            playerRagdollPacket.ragdollPositions = estimatedRagdollPos;
                            playerRagdollPacket.ragdollRotations = estimatedRagdollRotation;
                        }

                        pnd.Apply(playerRagdollPacket);
                        pnd.PositionChanged();
                        ModManager.clientSync.MovePlayer(pnd);
                    }
                    break;

                case (byte) PacketType.PLAYER_HEALTH_SET:
                    PlayerHealthSetPacket playerHealthSetPacket = (PlayerHealthSetPacket) p;

                    if(ModManager.clientSync.syncData.players.ContainsKey(playerHealthSetPacket.playerId)) {
                        ModManager.clientSync.syncData.players[playerHealthSetPacket.playerId].Apply(playerHealthSetPacket);
                    }
                    break;

                case (byte) PacketType.PLAYER_HEALTH_CHANGE:
                    PlayerHealthChangePacket playerHealthChangePacket = (PlayerHealthChangePacket) p;

                    if(playerHealthChangePacket.ClientId == netclient.ClientId) {
                        Player.currentCreature.currentHealth += playerHealthChangePacket.change;

                        try {
                            if(Player.currentCreature.currentHealth <= 0 && !Player.invincibility)
                                Player.currentCreature.Kill();
                        } catch(NullReferenceException) { }

                        NetworkLocalPlayer.Instance.SendHealthPacket();
                    }
                    break;
                #endregion

                #region Item Packets
                case (byte) PacketType.ITEM_SPAWN:
                    ItemSpawnPacket itemSpawnPacket = (ItemSpawnPacket) p;

                    bool already_existed_on_server = false;
                    if(itemSpawnPacket.clientsideId < 0) {
                        already_existed_on_server = true;
                        itemSpawnPacket.clientsideId = Math.Abs(itemSpawnPacket.clientsideId);
                    }

                    if(ModManager.clientSync.syncData.items.ContainsKey(-itemSpawnPacket.clientsideId)) { // Item has been spawned by player
                        ItemNetworkData exisitingSync = ModManager.clientSync.syncData.items[-itemSpawnPacket.clientsideId];
                        exisitingSync.networkedId = itemSpawnPacket.itemId;

                        ModManager.clientSync.syncData.items.TryRemove(-itemSpawnPacket.clientsideId, out _);

                        if(ModManager.clientSync.syncData.items.ContainsKey(itemSpawnPacket.itemId)) { // Item has already been spawned by server before we sent it, so we can just despawn it
                            if(ModManager.clientSync.syncData.items[itemSpawnPacket.itemId] != exisitingSync) {
                                if(exisitingSync.clientsideItem != null && !exisitingSync.clientsideItem.isBrokenPiece) exisitingSync.clientsideItem.Despawn();
                            } else {
                                exisitingSync.ApplyPositionToItem();
                            }
                            return;
                        } else { // Assign item to its network Id
                            ModManager.clientSync.syncData.items.TryAdd(itemSpawnPacket.itemId, exisitingSync);
                        }

                        if(already_existed_on_server) { // Server told us he already knows about the item, so we unset the clientsideId to make sure we dont send unnessasary position updates
                            Log.Debug(Defines.CLIENT, $"Server knew about item {itemSpawnPacket.type} (Local: {exisitingSync.clientsideId} - Server: {itemSpawnPacket.itemId}) already (Probably map default item).");
                            exisitingSync.clientsideId = 0; // Server had the item already known, so reset that its been spawned by the player
                        }

                        exisitingSync.StartNetworking();
                    } else { // Item has been spawned by other player or already existed in session
                        if(ModManager.clientSync.syncData.items.ContainsKey(itemSpawnPacket.itemId)) {
                            //Spawner.TrySpawnItem(ModManager.clientSync.syncData.items[itemSpawnPacket.itemId]);
                            return;
                        }

                        ItemNetworkData ind = new ItemNetworkData();
                        ind.Apply(itemSpawnPacket);

                        Item item_found = SyncFunc.DoesItemAlreadyExist(ind, Item.allActive);
                        
                        if(item_found == null) {
                            Spawner.TrySpawnItem(ind);
                        } else {
                            ind.clientsideItem = item_found;
                            //item_found.disallowDespawn = true;

                            Log.Debug(Defines.CLIENT, $"Item {ind.dataId} ({ind.networkedId}) matched with server.");

                            ind.StartNetworking();
                        }
                        ModManager.clientSync.syncData.items.TryAdd(ind.networkedId, ind);
                    }
                    break;

                case (byte) PacketType.ITEM_DESPAWN:
                    ItemDespawnPacket itemDespawnPacket = (ItemDespawnPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemDespawnPacket.itemId)) {
                        ItemNetworkData ind = ModManager.clientSync.syncData.items[itemDespawnPacket.itemId];

                        if(ind.clientsideItem != null) {
                            ind.clientsideItem.Despawn();
                        }
                        ModManager.clientSync.syncData.items.TryRemove(itemDespawnPacket.itemId, out _);
                    }
                    break;

                case (byte) PacketType.ITEM_POSITION:
                    ItemPositionPacket itemPositionPacket = (ItemPositionPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemPositionPacket.itemId)) {
                        ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemPositionPacket.itemId];

                        itemNetworkData.Apply(itemPositionPacket);
                        itemNetworkData.PositionChanged();

                        itemNetworkData.ApplyPositionToItem();
                    }
                    break;

                case (byte) PacketType.ITEM_OWNER:
                    ItemOwnerPacket itemOwnerPacket = (ItemOwnerPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemOwnerPacket.itemId)) {
                        ModManager.clientSync.syncData.items[itemOwnerPacket.itemId].SetOwnership(itemOwnerPacket.owning);

                        ModManager.clientSync.syncData.items[itemOwnerPacket.itemId].networkItem?.OnHoldStateChanged();
                    }
                    break;

                case (byte) PacketType.ITEM_SNAPPING_SNAP:
                    ItemSnapPacket itemSnapPacket = (ItemSnapPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemSnapPacket.itemId)) {
                        ItemNetworkData ind = ModManager.clientSync.syncData.items[itemSnapPacket.itemId];

                        ind.Apply(itemSnapPacket);
                        ind.UpdateHoldState();
                    }
                    break;

                case (byte) PacketType.ITEM_SNAPPING_UNSNAP:
                    ItemUnsnapPacket itemUnsnapPacket = (ItemUnsnapPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemUnsnapPacket.itemId)) {
                        ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemUnsnapPacket.itemId];

                        itemNetworkData.Apply(itemUnsnapPacket);
                        itemNetworkData.UpdateHoldState();
                    }
                    break;

                case (byte) PacketType.ITEM_BREAK:
                    ItemBreakPacket itemBreakPacket = (ItemBreakPacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemBreakPacket.itemId)) {
                        ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemBreakPacket.itemId];

                        Breakable breakable = itemNetworkData.clientsideItem.GetComponent<Breakable>();
                        if(breakable != null) {
                            breakable.Break();

                            for(int i = 0; i < breakable.subBrokenBodies.Count; i++) {
                                if(breakable.subBrokenBodies.Count <= i) break;
                                Rigidbody rb = breakable.subBrokenBodies[i];
                                rb.velocity = itemBreakPacket.velocities[i];
                                rb.angularVelocity = itemBreakPacket.angularVelocities[i];
                            }

                            Log.Debug(Defines.SERVER, $"Broke item {itemNetworkData.dataId}.");
                        }
                    }
                    break;
                #endregion

                #region Imbues
                case (byte) PacketType.ITEM_IMBUE:
                    ItemImbuePacket itemImbuePacket = (ItemImbuePacket) p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemImbuePacket.itemId)) {
                        ModManager.clientSync.syncData.items[itemImbuePacket.itemId].Apply(itemImbuePacket);
                    }
                    break;
                #endregion

                #region Level Changing
                case (byte) PacketType.PREPARE_LEVEL_CHANGE:
                    Dispatcher.Enqueue(() => {
                        foreach(PlayerNetworkData playerSync in ModManager.clientSync.syncData.players.Values) { // Will despawn all player creatures and respawn them after level has changed
                            if(playerSync.creature == null) continue;

                            Creature c = playerSync.creature;
                            playerSync.creature = null;
                            playerSync.isSpawning = false;
                            try {
                                c.Despawn();
                                GameObject.Destroy(c.gameObject);
                            } catch(Exception) { }
                        }
                    });
                    
                    ModManager.clientInstance.allowTransmission = false;
                    break;

                case (byte) PacketType.DO_LEVEL_CHANGE:
                    LevelChangePacket levelChangePacket = (LevelChangePacket) p;

                    // Writeback data to client cache
                    ModManager.clientSync.syncData.serverlevel   = levelChangePacket.level;
                    ModManager.clientSync.syncData.servermode    = levelChangePacket.mode;
                    ModManager.clientSync.syncData.serveroptions = levelChangePacket.option_dict;

                    string currentLevel = "";
                    string currentMode = "";
                    Dictionary<string, string> currentOptions = new Dictionary<string, string>();
                    LevelInfo.ReadLevelInfo(out currentLevel, out currentMode, out currentOptions);
                    
                    if(!(currentLevel.Equals(ModManager.clientSync.syncData.serverlevel, StringComparison.OrdinalIgnoreCase))) {
                        LevelInfo.TryLoadLevel(ModManager.clientSync.syncData.serverlevel, ModManager.clientSync.syncData.servermode, ModManager.clientSync.syncData.serveroptions);
                    } else {
                        levelChangePacket.SendToServerReliable();
                    }
                    break;
                #endregion

                #region Creature Packets
                case (byte) PacketType.CREATURE_SPAWN:
                    CreatureSpawnPacket creatureSpawnPacket = (CreatureSpawnPacket) p;

                    if(creatureSpawnPacket.clientsideId > 0 && ModManager.clientSync.syncData.creatures.ContainsKey(-creatureSpawnPacket.clientsideId)) { // Creature has been spawned by player
                        CreatureNetworkData exisitingSync = ModManager.clientSync.syncData.creatures[-creatureSpawnPacket.clientsideId];
                        exisitingSync.networkedId = creatureSpawnPacket.creatureId;

                        ModManager.clientSync.syncData.creatures.TryRemove(-creatureSpawnPacket.clientsideId, out _);

                        ModManager.clientSync.syncData.creatures.TryAdd(creatureSpawnPacket.creatureId, exisitingSync);

                        exisitingSync.StartNetworking();
                    } else {
                        CreatureNetworkData cnd;
                        if(!ModManager.clientSync.syncData.creatures.ContainsKey(creatureSpawnPacket.creatureId)) { // If creature is not already there
                            cnd = new CreatureNetworkData();
                            cnd.Apply(creatureSpawnPacket);

                            Log.Info(Defines.CLIENT, $"Server has summoned {cnd.creatureType} ({cnd.networkedId})");
                            ModManager.clientSync.syncData.creatures.TryAdd(cnd.networkedId, cnd);
                        } else {
                            cnd = ModManager.clientSync.syncData.creatures[creatureSpawnPacket.creatureId];
                        }
                        Spawner.TrySpawnCreature(cnd);
                    }
                    break;

                case (byte) PacketType.CREATURE_POSITION:
                    CreaturePositionPacket creaturePositionPacket = (CreaturePositionPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creaturePositionPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creaturePositionPacket.creatureId];
                        if(cnd.isSpawning) break;

                        cnd.Apply(creaturePositionPacket);
                        cnd.ApplyPositionToCreature();
                    }
                    break;

                case (byte) PacketType.CREATURE_HEALTH_SET:
                    CreatureHealthSetPacket creatureHealthSetPacket = (CreatureHealthSetPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureHealthSetPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureHealthSetPacket.creatureId];
                        cnd.Apply(creatureHealthSetPacket);
                        cnd.ApplyHealthToCreature();
                    }
                    break;

                case (byte) PacketType.CREATURE_HEALTH_CHANGE:
                    CreatureHealthChangePacket creatureHealthChangePacket = (CreatureHealthChangePacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureHealthChangePacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureHealthChangePacket.creatureId];
                        cnd.Apply(creatureHealthChangePacket);
                        cnd.ApplyHealthToCreature();
                    }
                    break;

                case (byte) PacketType.CREATURE_DESPAWN:
                    CreatureDepawnPacket creatureDepawnPacket = (CreatureDepawnPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureDepawnPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureDepawnPacket.creatureId];

                        if(cnd.creature != null) {
                            cnd.creature.Despawn();
                        }
                        ModManager.clientSync.syncData.creatures.TryRemove(creatureDepawnPacket.creatureId, out _);
                    }
                    break;

                case (byte) PacketType.CREATURE_PLAY_ANIMATION:
                    CreatureAnimationPacket creatureAnimationPacket = (CreatureAnimationPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureAnimationPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureAnimationPacket.creatureId];
                        if(cnd.creature == null) return;

                        cnd.creature.PlayAttackAnimation(creatureAnimationPacket.animationClip);
                    }
                    break;

                case (byte) PacketType.CREATURE_RAGDOLL:
                    CreatureRagdollPacket creatureRagdollPacket = (CreatureRagdollPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureRagdollPacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureRagdollPacket.creatureId];
                        if(cnd.isSpawning) break;

                        cnd.Apply(creatureRagdollPacket);
                        cnd.ApplyPositionToCreature();
                    }
                    break;

                case (byte) PacketType.CREATURE_SLICE:
                    CreatureSlicePacket creatureSlicePacket = (CreatureSlicePacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureSlicePacket.creatureId)) {
                        CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureSlicePacket.creatureId];

                        RagdollPart.Type ragdollPartType = (RagdollPart.Type) creatureSlicePacket.slicedPart;

                        if(cnd.creature.ragdoll != null) {
                            RagdollPart rp = cnd.creature.ragdoll.GetPart(ragdollPartType);
                            if(rp != null) {
                                cnd.creature.ragdoll.TrySlice(rp);
                            } else {
                                Log.Err(Defines.CLIENT, $"Couldn't slice off {ragdollPartType} from {creatureSlicePacket.creatureId}.");
                            }
                        }
                    }
                    break;

                case (byte) PacketType.CREATURE_OWNER:
                    CreatureOwnerPacket creatureOwnerPacket = (CreatureOwnerPacket) p;

                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureOwnerPacket.creatureId)) {
                        ModManager.clientSync.syncData.creatures[creatureOwnerPacket.creatureId].SetOwnership(creatureOwnerPacket.owning);
                    }
                    break;
                #endregion

                #region Other Stuff
                case (byte) PacketType.CLEAR_DATA:
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

                    ModManager.clientSync.unfoundItemMode = ClientSync.UnfoundItemMode.DESPAWN;

                    break;

                case (byte) PacketType.DISPLAY_TEXT:
                    DisplayTextPacket displayTextPacket = (DisplayTextPacket) p;

                    TextDisplay.ShowTextDisplay(displayTextPacket);
                    break;

                case (byte) PacketType.SERVER_INFO:
                    serverInfo = (ServerInfoPacket) p;

                    DiscordIntegration.Instance.UpdateActivity();
                    break;
                #endregion

                default: break;
            }
        }

        internal void Disconnect() {
            if(netclient != null) {
                netclient.OnDataReceived -= OnPacket;
                netclient.Disconnect();
                netclient = null;
            }
        }

        internal void StartSync() {
            ModManager.clientSync.StartThreads();
        }
    }
}
