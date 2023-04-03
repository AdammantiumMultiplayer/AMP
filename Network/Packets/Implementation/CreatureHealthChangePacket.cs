using AMP.Events;
using AMP.Logging;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_HEALTH_CHANGE)]
    public class CreatureHealthChangePacket : NetPacket {
        [SyncedVar]       public long  creatureId;
        [SyncedVar(true)] public float change;

        public CreatureHealthChangePacket() { }

        public CreatureHealthChangePacket(long creatureId, float change) {
            this.creatureId = creatureId;
            this.change     = change;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];
                cnd.Apply(this);
                cnd.ApplyHealthToCreature();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.serverInstance.creatures[creatureId];
                if(cnd.Apply(this)) {
                    try { if(ServerEvents.OnCreatureKilled != null) ServerEvents.OnCreatureKilled.Invoke(cnd, client); } catch(Exception e) { Log.Err(e); }
                }

                server.SendToAllExcept(this, client.ClientId);

                // If the damage the player did is more than 30% of the already dealt damage,
                // then change the npc to that players authority
                if(change / (cnd.maxHealth - cnd.health) > 0.3) {
                    ModManager.serverInstance.UpdateCreatureOwner(cnd, client);
                }
            }
            return true;
        }
    }
}
