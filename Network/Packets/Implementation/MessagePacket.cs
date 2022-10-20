using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MESSAGE)]
    public class MessagePacket {
        [SyncedVar] public string message;
    }
}
