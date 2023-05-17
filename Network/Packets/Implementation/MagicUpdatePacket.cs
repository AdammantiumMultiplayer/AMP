using AMP.Datatypes;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MAGIC_UPDATE)]
    internal class MagicUpdatePacket : NetPacket {
        [SyncedVar]       public byte   handIndex;
        [SyncedVar]       public long   casterNetworkId;
        [SyncedVar]       public byte casterType;
        [SyncedVar(true)] public float  currentCharge;

        public MagicUpdatePacket() { }
        
        public MagicUpdatePacket(byte handIndex, long casterNetworkId, ItemHolderType casterType, float currentCharge) {
            this.handIndex       = handIndex;
            this.casterNetworkId = casterNetworkId;
            this.casterType      = (byte) casterType;
            this.currentCharge   = currentCharge;
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
