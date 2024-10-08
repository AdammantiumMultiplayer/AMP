﻿using AMP.Data;
using AMP.Events;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System.Linq;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_SPAWN)]
    public class CreatureSpawnPacket : AMPPacket {
        [SyncedVar]       public int      creatureId;
        [SyncedVar]       public int      clientsideId;
        [SyncedVar]       public string   type;
        [SyncedVar]       public string   container;
        [SyncedVar]       public byte     factionId;
        [SyncedVar]       public Vector3  position;
        [SyncedVar(true)] public float    rotationY;
        [SyncedVar]       public float    health;
        [SyncedVar]       public float    maxHealth;
        [SyncedVar(true)] public float    height;
        [SyncedVar]       public string[] equipment = new string[0];
        [SyncedVar]       public Color[]  colors    = new Color[0];
        [SyncedVar]       public string   ethnicGroup = "";

        public CreatureSpawnPacket() { }
        
        public CreatureSpawnPacket(int creatureId, int clientsideId, string type, string container, byte factionId, Vector3 position, float rotationY, float health, float maxHealth, float height, string[] equipment, Color[] colors, string ethnicGroup) {
            this.creatureId   = creatureId;
            this.clientsideId = clientsideId;
            this.type         = type;
            this.container    = container;
            this.factionId    = factionId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.health       = health;
            this.maxHealth    = maxHealth;
            this.height       = height;
            this.equipment    = equipment;
            this.colors       = colors;
            this.ethnicGroup  = ethnicGroup;
        }

        public CreatureSpawnPacket(CreatureNetworkData cnd)
            : this( creatureId:   cnd.networkedId
                  , clientsideId: cnd.clientsideId
                  , type:         cnd.creatureType
                  , container:    cnd.containerID
                  , factionId:    cnd.factionId
                  , position:     cnd.position
                  , rotationY:    cnd.rotationY
                  , health:       cnd.health
                  , maxHealth:    cnd.maxHealth
                  , height:       cnd.height
                  , equipment:    cnd.equipment
                  , colors:       cnd.colors
                  , ethnicGroup:  cnd.ethnicGroup
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            Dispatcher.Enqueue(() => {
                if(clientsideId > 0 && ModManager.clientSync.syncData.creatures.ContainsKey(-clientsideId)) { // Creature has been spawned by player
                    CreatureNetworkData exisitingSync = ModManager.clientSync.syncData.creatures[-clientsideId];
                    exisitingSync.networkedId = creatureId;

                    ModManager.clientSync.syncData.creatures.TryRemove(-clientsideId, out _);

                    ModManager.clientSync.syncData.creatures.TryAdd(creatureId, exisitingSync);

                    exisitingSync.StartNetworking();
                } else {
                    CreatureNetworkData cnd;
                    if(!ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) { // If creature is not already there
                        cnd = new CreatureNetworkData();
                        cnd.Apply(this);

                        Log.Info(Defines.CLIENT, $"Server has summoned {cnd.creatureType} ({cnd.networkedId})");
                        ModManager.clientSync.syncData.creatures.TryAdd(cnd.networkedId, cnd);
                    } else {
                        cnd = ModManager.clientSync.syncData.creatures[creatureId];
                    }
                    Spawner.TrySpawnCreature(cnd);
                }
            });
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            CreatureNetworkData cnd = new CreatureNetworkData();
            cnd.Apply(this);


            if(SyncFunc.DoesCreatureAlreadyExist(cnd, ModManager.serverInstance.creatures.Values.ToList()) != null) {
                server.SendTo(client, new CreatureDepawnPacket(-cnd.clientsideId));
            } else {
                cnd.networkedId = ModManager.serverInstance.NextCreatureId;

                ModManager.serverInstance.UpdateCreatureOwner(cnd, client);
                ModManager.serverInstance.creatures.TryAdd(cnd.networkedId, cnd);
                Log.Debug(Defines.SERVER, $"{client.ClientName} has summoned {cnd.creatureType} ({cnd.networkedId})");

                server.SendTo(client, new CreatureSpawnPacket(cnd));

                cnd.clientsideId = 0;

                server.SendToAllExcept(new CreatureSpawnPacket(cnd), client.ClientId);

                ServerEvents.InvokeOnCreatureSpawned(cnd, client);

                Cleanup.CheckCreatureLimit(client);
            }
            return true;
        }
    }
}
