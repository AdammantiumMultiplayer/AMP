using AMP.Data;
using AMP.Datatypes;
using AMP.Events;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using Netamite.Network.Packet;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThunderRoad;
using UnityEngine;
using PacketType = AMP.Network.Packets.PacketType;

namespace AMP.Network.Server {
    public class Server {
        internal NetamiteServer netamiteServer;

        public string currentLevel = null;
        public string currentMode = null;
        internal Dictionary<string, string> currentOptions = new Dictionary<string, string>();

        internal ConcurrentDictionary<int, ClientData> clientData = new ConcurrentDictionary<int, ClientData>();

        private long currentItemId = 1;
        internal ConcurrentDictionary<long, ItemNetworkData> items = new ConcurrentDictionary<long, ItemNetworkData>();
        internal ConcurrentDictionary<long, int> item_owner = new ConcurrentDictionary<long, int>();

        internal long currentCreatureId = 1;
        internal ConcurrentDictionary<long, CreatureNetworkData> creatures = new ConcurrentDictionary<long, CreatureNetworkData>();
        internal ConcurrentDictionary<long, int> creature_owner = new ConcurrentDictionary<long, int>();

        public static string DEFAULT_MAP = "Home";
        public static string DEFAULT_MODE = "Default";

        public int connectedClients {
            get { return netamiteServer.Clients.Length; }
        }
        public int spawnedItems {
            get { return items.Count; }
        }
        public int spawnedCreatures {
            get { return creatures.Count; }
        }
        public Dictionary<int, string> connectedClientList {
            get {
                Dictionary<int, string> test = new Dictionary<int, string>();
                foreach (var item in clientData) {
                    test.Add(item.Key, "NF");
                }
                return test;
            }
        }

        internal void Stop() {
            Log.Info(Defines.SERVER, $"Stopping server...");

            netamiteServer.Stop();

            Log.Info(Defines.SERVER, $"Server stopped.");
        }

        internal void Start(NetamiteServer netamiteServer) {
            this.netamiteServer = netamiteServer;
            Log.Info(Defines.SERVER, $"Starting server...");

            long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            netamiteServer.BeforeStart += (time) => {
                Dictionary<string, string> options;
                bool levelInfoSuccess = LevelInfo.ReadLevelInfo(out currentLevel, out currentMode, out options);

                if(!levelInfoSuccess || currentLevel.Equals("CharacterSelection")) {
                    currentLevel = DEFAULT_MAP;
                    currentMode = DEFAULT_MODE;
                }

                Log.Info(Defines.SERVER,
                         $"Server started after {DateTimeOffset.Now.ToUnixTimeMilliseconds() - ms}ms.\n" +
                         $"\t Level: {currentLevel} / Mode: {currentMode}\n" +
                         $"\t Options:\n\t{string.Join("\n\t", options.Select(p => p.Key + " = " + p.Value))}\n" +
                         $"\t Max-Players: {netamiteServer.MaxClients}\n" +
                         $"\t Has password: {(netamiteServer.HasConnectToken ? "Yes" : "No")}"
                         );

                netamiteServer.OnDisconnect += OnClientDisconnect;
                netamiteServer.OnDataReceived += ProcessPacket;
                netamiteServer.OnConnect += GreetPlayer;
            };

            netamiteServer.Start();
        }
        private void OnClientDisconnect(ClientInformation client, string reason) {
            LeavePlayer(client, reason);
        }

        internal void GreetPlayer(ClientInformation client) {
            GreetPlayer(client, false);
        }

        internal void GreetPlayer(ClientInformation client, bool loadedLevel = false) {
            ClientData cd;
            if(!clientData.ContainsKey(client.ClientId)) {
                cd = new ClientData();
                clientData.TryAdd(client.ClientId, cd);
            } else {
                cd = clientData[client.ClientId];
            }

            if(cd.greeted) return;

            netamiteServer.SendTo(client, new ServerInfoPacket(Defines.MOD_VERSION, netamiteServer.MaxClients));

            if(currentLevel.Length > 0 && !loadedLevel) {
                Log.Debug(Defines.SERVER, $"Waiting for player {client.ClientName} to load into the level.");
                netamiteServer.SendTo(client, new LevelChangePacket(currentLevel, currentMode, currentOptions));
                return;
            }

            // Send all player data to the new client
            foreach(ClientData other_client in clientData.Values) {
                if(other_client.playerSync == null) continue;
                netamiteServer.SendTo(client, new PlayerDataPacket(other_client.playerSync));
                netamiteServer.SendTo(client, new PlayerEquipmentPacket(other_client.playerSync));
            }

            SendItemsAndCreatures(client);

            Log.Info(Defines.SERVER, $"Player {client.ClientName} ({client.ClientId}) joined the server.");

            try { if(ServerEvents.OnPlayerJoin != null) ServerEvents.OnPlayerJoin.Invoke(client); } catch (Exception e) { Log.Err(e); }

            cd.greeted = true;
        }

        private void SendItemsAndCreatures(ClientInformation client) {
            if(items.Count > 0 || creatures.Count > 0) {
                // Clear all already present stuff first
                netamiteServer.SendTo(client, new ClearPacket(true, true));
            }

            // Send all spawned creatures to the client
            foreach(KeyValuePair<long, CreatureNetworkData> entry in creatures) {
                netamiteServer.SendTo(client, new CreatureSpawnPacket(entry.Value));
            }

            // Send all spawned items to the client
            foreach(KeyValuePair<long, ItemNetworkData> entry in items) {
                netamiteServer.SendTo(client, new ItemSpawnPacket(entry.Value));
                if(entry.Value.holderNetworkId > 0) {
                    netamiteServer.SendTo(client, new ItemSnapPacket(entry.Value));
                }
            }

            netamiteServer.SendTo(client, new AllowTransmissionPacket(true));
        }

        private void ProcessPacket(ClientInformation client, NetPacket p) {
            if(p == null) return;
            if(client == null) return;
            if(!clientData.ContainsKey(client.ClientId)) return;

            ClientData cd = clientData[client.ClientId];

            PacketType type = (PacketType) p.getPacketType();

            //Log.Warn("SERVER", type);

            switch(type) {
                #region Player Packets
                case PacketType.PLAYER_DATA:
                    PlayerDataPacket playerDataPacket = (PlayerDataPacket) p;

                    if(cd.playerSync == null) {
                        cd.playerSync = new PlayerNetworkData() { clientId = client.ClientId };
                    }
                    cd.playerSync.Apply(playerDataPacket);

                    cd.playerSync.name = Regex.Replace(client.ClientName, @"[^\u0000-\u007F]+", string.Empty);

                    cd.playerSync.clientId = client.ClientId;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    netamiteServer.SendToAll(new PlayerDataPacket(client.playerSync));//, client.playerId);
                    #else
                    netamiteServer.SendToAllExcept(new PlayerDataPacket(cd.playerSync), client.ClientId);
                    #endif
                    break;

                case PacketType.PLAYER_POSITION:
                    PlayerPositionPacket playerPositionPacket = (PlayerPositionPacket) p;

                    if(cd.playerSync == null) break;

                    cd.playerSync.Apply(playerPositionPacket);
                    cd.playerSync.clientId = client.ClientId;

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    netamiteServer.SendToAll(new PlayerPositionPacket(cd.playerSync));//, client.ClientId);
                    #else
                    netamiteServer.SendToAllExcept(new PlayerPositionPacket(cd.playerSync), client.ClientId);
                    #endif
                    break;

                case PacketType.PLAYER_EQUIPMENT:
                    PlayerEquipmentPacket playerEquipmentPacket = (PlayerEquipmentPacket) p;

                    cd.playerSync.Apply(playerEquipmentPacket);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    netamiteServer.SendToAll(new PlayerEquipmentPacket(client.playerSync));
                    #else
                    netamiteServer.SendToAllExcept(new PlayerEquipmentPacket(cd.playerSync), client.ClientId);
                    #endif
                    break;

                case PacketType.PLAYER_RAGDOLL:
                    PlayerRagdollPacket playerRagdollPacket = (PlayerRagdollPacket) p;

                    if(cd.playerSync == null) break;
                    if(client.ClientId != playerRagdollPacket.playerId) return;

                    cd.playerSync.Apply(playerRagdollPacket);

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    netamiteServer.SendToAll(playerRagdollPacket);
                    #else
                    netamiteServer.SendToAllExcept(playerRagdollPacket, client.ClientId);
                    #endif
                    break;

                case PacketType.PLAYER_HEALTH_SET:
                    PlayerHealthSetPacket playerHealthSetPacket = (PlayerHealthSetPacket) p;

                    if(cd.playerSync.Apply(playerHealthSetPacket)) {
                        try { if(ServerEvents.OnPlayerKilled != null) ServerEvents.OnPlayerKilled.Invoke(cd.playerSync, client); } catch(Exception e) { Log.Err(e); }
                    }

                    #if DEBUG_SELF
                    // Just for debug to see yourself
                    netamiteServer.SendToAll(new PlayerHealthSetPacket(client.playerSync));
                    #else
                    netamiteServer.SendToAllExcept(new PlayerHealthSetPacket(cd.playerSync), client.ClientId);
                    #endif
                    break;

                case PacketType.PLAYER_HEALTH_CHANGE:
                    PlayerHealthChangePacket playerHealthChangePacket = (PlayerHealthChangePacket) p;
                    
                    if(!ModManager.safeFile.hostingSettings.pvpEnable) break;
                    if(ModManager.safeFile.hostingSettings.pvpDamageMultiplier <= 0) break;

                    if(clientData.ContainsKey(playerHealthChangePacket.ClientId)) {
                        if(playerHealthChangePacket.change < 0) playerHealthChangePacket.change *= ModManager.safeFile.hostingSettings.pvpDamageMultiplier;

                        netamiteServer.SendTo(playerHealthChangePacket.ClientId, playerHealthChangePacket);
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
                        items.TryAdd(ind.networkedId, ind);
                        UpdateItemOwner(ind, client);
                        Log.Debug(Defines.SERVER, $"{client.ClientName} has spawned item {ind.dataId} ({ind.networkedId})" );
                    } else {
                        ind.clientsideId = -ind.clientsideId;
                        Log.Debug(Defines.SERVER, $"{client.ClientName} has duplicate of {ind.dataId} ({ind.networkedId})");
                        was_duplicate = true;
                    }

                    netamiteServer.SendTo(client, new ItemSpawnPacket(ind));

                    if(was_duplicate) return; // If it was a duplicate, dont send it to other players

                    ind.clientsideId = 0;

                    netamiteServer.SendToAllExcept(new ItemSpawnPacket(ind), client.ClientId);

                    try { if(ServerEvents.OnItemSpawned != null) ServerEvents.OnItemSpawned.Invoke(ind, client); } catch(Exception e) { Log.Err(e); }
                    
                    Cleanup.CheckItemLimit(client);
                    break;

                case PacketType.ITEM_DESPAWN:
                    ItemDespawnPacket itemDespawnPacket = (ItemDespawnPacket) p;

                    if(items.ContainsKey(itemDespawnPacket.itemId)) {
                        ind = items[itemDespawnPacket.itemId];

                        Log.Debug(Defines.SERVER, $"{client.ClientName} has despawned item {ind.dataId} ({ind.networkedId})");

                        netamiteServer.SendToAllExcept(itemDespawnPacket, client.ClientId);

                        items.TryRemove(itemDespawnPacket.itemId, out _);
                        item_owner.TryRemove(itemDespawnPacket.itemId, out _);

                        try { if(ServerEvents.OnItemDespawned != null) ServerEvents.OnItemDespawned.Invoke(ind, client); } catch(Exception e) { Log.Err(e); }
                    }

                    break;

                case PacketType.ITEM_POSITION:
                    ItemPositionPacket itemPositionPacket = (ItemPositionPacket) p;

                    if(items.ContainsKey(itemPositionPacket.itemId)) {
                        ind = items[itemPositionPacket.itemId];

                        ind.Apply(itemPositionPacket);

                        netamiteServer.SendToAllExcept(itemPositionPacket, client.ClientId);
                    }
                    break;

                case PacketType.ITEM_OWNER:
                    ItemOwnerPacket itemOwnerPacket = (ItemOwnerPacket) p;

                    if(itemOwnerPacket.itemId > 0 && items.ContainsKey(itemOwnerPacket.itemId)) {
                        UpdateItemOwner(items[itemOwnerPacket.itemId], client);
                    }
                    break;

                case PacketType.ITEM_SNAPPING_SNAP:
                    ItemSnapPacket itemSnapPacket = (ItemSnapPacket) p;

                    if(itemSnapPacket.itemId > 0 && items.ContainsKey(itemSnapPacket.itemId)) {
                        ind = items[itemSnapPacket.itemId];

                        ind.Apply(itemSnapPacket);

                        Log.Debug(Defines.SERVER, $"Snapped item {ind.dataId} to {ind.holderNetworkId} to { (ind.equipmentSlot == Holder.DrawSlot.None ? "hand " + ind.holdingSide : "slot " + ind.equipmentSlot) }.");
                        netamiteServer.SendToAllExcept(itemSnapPacket, client.ClientId);
                    }
                    break;

                case PacketType.ITEM_SNAPPING_UNSNAP:
                    ItemUnsnapPacket itemUnsnapPacket = (ItemUnsnapPacket) p;

                    if(itemUnsnapPacket.itemId > 0 && items.ContainsKey(itemUnsnapPacket.itemId)) {
                        ind = items[itemUnsnapPacket.itemId];
                        Log.Debug(Defines.SERVER, $"Unsnapped item {ind.dataId} from {ind.holderNetworkId}.");

                        ind.Apply(itemUnsnapPacket);

                        netamiteServer.SendToAllExcept(itemUnsnapPacket, client.ClientId);
                    }
                    break;

                case PacketType.ITEM_BREAK:
                    ItemBreakPacket itemBreakPacket = (ItemBreakPacket)p;

                    if(ModManager.clientSync.syncData.items.ContainsKey(itemBreakPacket.itemId)) {
                        ind = ModManager.clientSync.syncData.items[itemBreakPacket.itemId];
                        
                        Log.Debug(Defines.SERVER, $"Broke item {ind.dataId} by {client.ClientName}.");

                        netamiteServer.SendToAllExcept(itemBreakPacket, client.ClientId);
                    }
                    break;
                #endregion

                #region Imbues
                case PacketType.ITEM_IMBUE:
                    ItemImbuePacket itemImbuePacket = (ItemImbuePacket) p;

                    netamiteServer.SendToAllExcept(p, client.ClientId); // Just forward them atm
                    break;
                #endregion

                #region Level Changing
                case PacketType.DO_LEVEL_CHANGE:
                    LevelChangePacket levelChangePacket = (LevelChangePacket) p;

                    if(!cd.greeted) {
                        if(levelChangePacket.eventTime == EventTime.OnEnd) {
                            GreetPlayer(client, true);
                        }
                        return;
                    }

                    if(levelChangePacket.level == null) return;
                    if(levelChangePacket.mode  == null) return;

                    if(levelChangePacket.level.Equals("characterselection", StringComparison.OrdinalIgnoreCase)) return;

                    if(!(levelChangePacket.level.Equals(currentLevel, StringComparison.OrdinalIgnoreCase) && levelChangePacket.mode.Equals(currentMode, StringComparison.OrdinalIgnoreCase))) { // Player is the first to join that level
                        if(!ModManager.safeFile.hostingSettings.allowMapChange) {
                            Log.Err(Defines.SERVER, $"{ client.ClientName } tried changing level.");
                            LeavePlayer(client, "Player tried to change level.");
                            return;
                        }

                        if(levelChangePacket.eventTime == EventTime.OnStart) {
                            Log.Info(Defines.SERVER, $"{client.ClientName} started to load level {levelChangePacket.level} with mode {levelChangePacket.mode}.");
                            netamiteServer.SendToAllExcept(new PrepareLevelChangePacket(client.ClientName, levelChangePacket.level, levelChangePacket.mode), client.ClientId);

                            netamiteServer.SendToAllExcept(
                                  new DisplayTextPacket("level_change", $"Player {client.ClientName} is loading into {levelChangePacket.level}.\nPlease stay in your level.", Color.yellow, Vector3.forward * 2, true, true, 60)
                                , client.ClientId
                            );
                        } else {
                            currentLevel = levelChangePacket.level;
                            currentMode = levelChangePacket.mode;

                            currentOptions = levelChangePacket.option_dict;

                            ClearItemsAndCreatures();
                            netamiteServer.SendToAllExcept(new ClearPacket(true, true), client.ClientId);
                            netamiteServer.SendToAllExcept(levelChangePacket, client.ClientId);
                        }
                    }
                    
                    if(levelChangePacket.eventTime == EventTime.OnEnd) {
                        Log.Info(Defines.SERVER, $"{client.ClientName} loaded level {currentLevel} with mode {currentMode}.");
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

                    UpdateCreatureOwner(cnd, client);
                    creatures.TryAdd(cnd.networkedId, cnd);
                    Log.Debug(Defines.SERVER, $"{client.ClientName} has summoned {cnd.creatureType} ({cnd.networkedId})");

                    netamiteServer.SendTo(client, new CreatureSpawnPacket(cnd));

                    cnd.clientsideId = 0;

                    netamiteServer.SendToAllExcept(new CreatureSpawnPacket(cnd), client.ClientId);

                    try { if(ServerEvents.OnCreatureSpawned != null) ServerEvents.OnCreatureSpawned.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }

                    Cleanup.CheckCreatureLimit(client);

                    // Check for the closest player to the NPC and asign them to the player
                    //if(Config.REASSIGN_CREATURE_TO_NEXT_BEST_PLAYER) {
                    //    ClientData cd = ServerFunc.GetClosestPlayerTo(cnd.position, cnd.position.SQ_DIST(client.playerSync.position), Config.REASSIGN_CREATURE_THRESHOLD_PERCENTAGE);
                    //    if(cd != null && cd != client) {
                    //        Log.Debug($"Creature { cnd.creatureType } ({ cnd.networkedId }) spawned closer to { client.name }, updating owner.");
                    //        UpdateCreatureOwner(cnd, cd);
                    //    }
                    //}
                    break;


                case PacketType.CREATURE_POSITION:
                    CreaturePositionPacket creaturePositionPacket = (CreaturePositionPacket) p;

                    if(creatures.ContainsKey(creaturePositionPacket.creatureId)) {
                        if(creature_owner[creaturePositionPacket.creatureId] != client.ClientId) return;

                        cnd = creatures[creaturePositionPacket.creatureId];
                        cnd.Apply(creaturePositionPacket);

                        netamiteServer.SendToAllExcept(creaturePositionPacket, client.ClientId);
                    }
                    break;

                case PacketType.CREATURE_HEALTH_SET:
                    CreatureHealthSetPacket creatureHealthSetPacket = (CreatureHealthSetPacket) p;

                    if(creatures.ContainsKey(creatureHealthSetPacket.creatureId)) {
                        cnd = creatures[creatureHealthSetPacket.creatureId];
                        if(cnd.Apply(creatureHealthSetPacket)) {
                            try { if(ServerEvents.OnCreatureKilled != null) ServerEvents.OnCreatureKilled.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                        }

                        netamiteServer.SendToAllExcept(creatureHealthSetPacket, client.ClientId);
                    }
                    break;

                case PacketType.CREATURE_HEALTH_CHANGE:
                    CreatureHealthChangePacket creatureHealthChangePacket = (CreatureHealthChangePacket) p;

                    if(creatures.ContainsKey(creatureHealthChangePacket.creatureId)) {
                        cnd = creatures[creatureHealthChangePacket.creatureId];
                        if(cnd.Apply(creatureHealthChangePacket)) {
                            try { if(ServerEvents.OnCreatureKilled != null) ServerEvents.OnCreatureKilled.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                        }

                        netamiteServer.SendToAllExcept(creatureHealthChangePacket, client.ClientId);

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

                        Log.Debug(Defines.SERVER, $"{client.ClientName} has despawned creature {cnd.creatureType} ({cnd.networkedId})");
                        netamiteServer.SendToAllExcept(creatureDepawnPacket, client.ClientId);

                        creatures.TryRemove(creatureDepawnPacket.creatureId, out _);
                        creature_owner.TryRemove(creatureDepawnPacket.creatureId, out _);

                        try { if(ServerEvents.OnCreatureDespawned != null) ServerEvents.OnCreatureDespawned.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                    }
                    break;

                case PacketType.CREATURE_PLAY_ANIMATION:
                    CreatureAnimationPacket creatureAnimationPacket = (CreatureAnimationPacket) p;

                    if(creatures.ContainsKey(creatureAnimationPacket.creatureId)) {
                        if(creature_owner[creatureAnimationPacket.creatureId] != client.ClientId) return;

                        netamiteServer.SendToAllExcept(creatureAnimationPacket, client.ClientId);
                    }
                    break;

                case PacketType.CREATURE_RAGDOLL:
                    CreatureRagdollPacket creatureRagdollPacket = (CreatureRagdollPacket) p;

                    if(creatures.ContainsKey(creatureRagdollPacket.creatureId)) {
                        if(creature_owner[creatureRagdollPacket.creatureId] != client.ClientId) return;

                        cnd = creatures[creatureRagdollPacket.creatureId];
                        cnd.Apply(creatureRagdollPacket);

                        netamiteServer.SendToAllExcept(creatureRagdollPacket, client.ClientId);
                    }
                    break;

                case PacketType.CREATURE_SLICE:
                    CreatureSlicePacket creatureSlicePacket = (CreatureSlicePacket) p;

                    if(creatures.ContainsKey(creatureSlicePacket.creatureId)) {
                        netamiteServer.SendToAllExcept(creatureSlicePacket, client.ClientId);
                    }
                    break;

                case PacketType.CREATURE_OWNER:
                    CreatureOwnerPacket creatureOwnerPacket = (CreatureOwnerPacket) p;

                    if(creatureOwnerPacket.creatureId > 0 && creatures.ContainsKey(creatureOwnerPacket.creatureId)) {
                        UpdateCreatureOwner(creatures[creatureOwnerPacket.creatureId], client);
                    }
                    break;
                #endregion

                #region Magic Stuff
                case PacketType.MAGIC_SET:
                    MagicSetPacket magicSetPacket = (MagicSetPacket) p;

                    netamiteServer.SendToAllExcept(magicSetPacket, client.ClientId);
                    break;

                case PacketType.MAGIC_UPDATE:

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

        internal void UpdateItemOwner(ItemNetworkData itemNetworkData, ClientInformation newOwner) {
            int oldOwnerId = 0;
            if(item_owner.ContainsKey(itemNetworkData.networkedId)) {
                try {
                    oldOwnerId = item_owner[itemNetworkData.networkedId];
                } catch(Exception) { }
                item_owner[itemNetworkData.networkedId] = newOwner.ClientId;

                Log.Debug(Defines.SERVER, $"{newOwner.ClientName} has taken ownership of item {itemNetworkData.dataId} ({itemNetworkData.networkedId})");

                netamiteServer.SendTo(newOwner, new ItemOwnerPacket(itemNetworkData.networkedId, true));
                netamiteServer.SendToAllExcept(new ItemOwnerPacket(itemNetworkData.networkedId, false), newOwner.ClientId);
            } else {
                item_owner.TryAdd(itemNetworkData.networkedId, newOwner.ClientId);
            }

            if(oldOwnerId != newOwner.ClientId) {
                try { if(ServerEvents.OnItemOwnerChanged != null) ServerEvents.OnItemOwnerChanged.Invoke(itemNetworkData, null, newOwner); } catch(Exception e) { Log.Err(e); }
            }
        }

        internal void UpdateCreatureOwner(CreatureNetworkData creatureNetworkData, ClientInformation newOwner) {
            int oldOwnerId = 0;
            if(creature_owner.ContainsKey(creatureNetworkData.networkedId)) {
                try {
                    oldOwnerId = creature_owner[creatureNetworkData.networkedId];
                } catch(Exception) { }
                if(creature_owner[creatureNetworkData.networkedId] != newOwner.ClientId) {
                    creature_owner[creatureNetworkData.networkedId] = newOwner.ClientId;

                    netamiteServer.SendTo(newOwner, new CreatureOwnerPacket(creatureNetworkData.networkedId, true));
                    netamiteServer.SendToAllExcept(new CreatureOwnerPacket(creatureNetworkData.networkedId, false), newOwner.ClientId);

                    Log.Debug(Defines.SERVER, $"{newOwner.ClientName} has taken ownership of creature {creatureNetworkData.creatureType} ({creatureNetworkData.networkedId})");

                    List<ItemNetworkData> holdingItems = items.Values.Where(ind => ind.holderNetworkId == creatureNetworkData.networkedId).ToList();
                    foreach(ItemNetworkData item in holdingItems) {
                        if(item.holderType == ItemHolderType.PLAYER) continue; // Don't transfer items that are held by a player
                        UpdateItemOwner(item, newOwner);
                    }
                }
            } else {
                creature_owner.TryAdd(creatureNetworkData.networkedId, newOwner.ClientId);
            }

            if(oldOwnerId != newOwner.ClientId) {
                try { if(ServerEvents.OnItemOwnerChanged != null) ServerEvents.OnCreatureOwnerChanged.Invoke(creatureNetworkData, null, newOwner); } catch(Exception e) { Log.Err(e); }
            }
        }

        internal void ClearItemsAndCreatures() {
            creatures.Clear();
            creature_owner.Clear();
            items.Clear();
            item_owner.Clear();
        }

        internal void LeavePlayer(ClientInformation client, string reason = "Player disconnected") {
            Log.Debug(Defines.SERVER, $"{client.ClientName} initialized a disconnect. {reason}");

            if(netamiteServer.Clients.Length <= 0) {
                ClearItemsAndCreatures();
                Log.Info(Defines.SERVER, $"Clearing server because last player disconnected.");
            } else {
                try {
                    ClientInformation migrateUser = netamiteServer.Clients.First(entry => entry.ClientId != client.ClientId);
                    try {
                        KeyValuePair<long, int>[] entries = item_owner.Where(entry => entry.Value == client.ClientId).ToArray();

                        if(entries.Length > 0) {
                            foreach(KeyValuePair<long, int> entry in entries) {
                                if(items.ContainsKey(entry.Key)) {
                                    item_owner[entry.Key] = migrateUser.ClientId;
                                    netamiteServer.SendTo(migrateUser, new ItemOwnerPacket(entry.Key, true));
                                }
                            }
                            Log.Info(Defines.SERVER, $"Migrated items from { client.ClientName } to { migrateUser.ClientName }.");
                        }
                    } catch(Exception e) {
                        Log.Err(Defines.SERVER, $"Couldn't migrate items from {client.ClientName} to { migrateUser.ClientName}.\n{e}");
                    }

                    try {
                        KeyValuePair<long, int>[] entries = creature_owner.Where(entry => entry.Value == client.ClientId).ToArray();

                        if(entries.Length > 0) {
                            foreach(KeyValuePair<long, int> entry in entries) {
                                if(creatures.ContainsKey(entry.Key)) {
                                    creature_owner[entry.Key] = migrateUser.ClientId;
                                    netamiteServer.SendTo(migrateUser, new CreatureOwnerPacket(entry.Key, true));
                                }
                            }
                            Log.Info(Defines.SERVER, $"Migrated creatures from { client.ClientName } to { migrateUser.ClientName }.");
                        }
                    } catch(Exception e) {
                        Log.Err(Defines.SERVER, $"Couldn't migrate creatures from { client.ClientName } to { migrateUser.ClientName }.\n{e}");
                    }
                } catch(Exception e) {
                    Log.Err(Defines.SERVER, $"Couldn't migrate stuff from { client.ClientName } to other client.\n{e}");
                }
            }

            try {
                clientData.TryRemove(client.ClientId, out _);
            } catch(Exception e) {
                Log.Err($"Unable to remove client from list {client.ClientName}: {e}");
            }

            try { if(ServerEvents.OnPlayerQuit != null) ServerEvents.OnPlayerQuit.Invoke(client); } catch(Exception e) { Log.Err(e); }

            Log.Info(Defines.SERVER, $"{client.ClientName} disconnected. {reason}");
        }
    }
}
