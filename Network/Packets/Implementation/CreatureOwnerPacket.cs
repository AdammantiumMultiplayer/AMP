﻿using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_OWNER)]
    public class CreatureOwnerPacket : AMPPacket {
        [SyncedVar] public int  creatureId;
        [SyncedVar] public bool owning;

        public CreatureOwnerPacket() { }

        public CreatureOwnerPacket(int creatureId, bool owning) {
            this.creatureId = creatureId;
            this.owning     = owning;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(owning && !ModManager.clientSync.syncData.owningCreatures.Contains(creatureId)) ModManager.clientSync.syncData.owningCreatures.Add(creatureId);
            if(!owning && ModManager.clientSync.syncData.owningCreatures.Contains(creatureId)) ModManager.clientSync.syncData.owningCreatures.Remove(creatureId);

            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                ModManager.clientSync.syncData.creatures[creatureId].SetOwnership(owning);
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(creatureId > 0 && ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                ModManager.serverInstance.UpdateCreatureOwner(ModManager.serverInstance.creatures[creatureId], client);
            }
            return true;
        }
    }
}
