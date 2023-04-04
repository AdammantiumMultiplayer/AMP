using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_OWNER)]
    public class CreatureOwnerPacket : NetPacket {
        [SyncedVar] public long creatureId;
        [SyncedVar] public bool owning;

        public CreatureOwnerPacket() { }

        public CreatureOwnerPacket(long creatureId, bool owning) {
            this.creatureId = creatureId;
            this.owning     = owning;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                ModManager.clientSync.syncData.creatures[creatureId].SetOwnership(owning);
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(creatureId > 0 && ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                ModManager.serverInstance.UpdateCreatureOwner(ModManager.serverInstance.creatures[creatureId], client);
            }
            return true;
        }
    }
}
