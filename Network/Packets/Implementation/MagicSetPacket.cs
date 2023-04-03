using AMP.Datatypes;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MAGIC_SET)]
    internal class MagicSetPacket : NetPacket {
        [SyncedVar] public string magicId;
        [SyncedVar] public byte   handIndex;
        [SyncedVar] public long   casterNetworkId;
        [SyncedVar] public byte   casterType;

        public MagicSetPacket() { }
        
        public MagicSetPacket(string magicId, byte handIndex, long casterNetworkId, ItemHolderType casterType) {
            this.magicId         = magicId;
            this.handIndex       = handIndex;
            this.casterNetworkId = casterNetworkId;
            this.casterType      = (byte) casterType;
        }

        public override bool ProcessClient(NetamiteClient client) {
            // TODO: Apply Magic
            return base.ProcessClient(client);
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            server.SendToAllExcept(this, client.ClientId);
            return true;
        }
    }
}
