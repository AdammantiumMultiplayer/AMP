using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ALLOW_TRANSMISSION)]
    public class AllowTransmissionPacket : NetPacket {
        [SyncedVar] public bool allow;

        public AllowTransmissionPacket() { }

        public AllowTransmissionPacket(bool allow) {
            this.allow = allow;
        }
    }
}
