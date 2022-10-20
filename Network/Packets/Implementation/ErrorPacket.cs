using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ERROR)]
    public class ErrorPacket {
        [SyncedVar] public string error_message;
    }
}
