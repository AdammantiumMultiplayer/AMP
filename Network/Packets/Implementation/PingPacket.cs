using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PING)]
    public class PingPacket : NetPacket {
        public PingPacket() { }
    }
}
