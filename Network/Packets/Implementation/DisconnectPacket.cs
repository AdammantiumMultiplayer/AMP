using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.DISCONNECT)]
    public class DisconnectPacket {
        [SyncedVar] public long playerId;
        [SyncedVar] public string message;
    }
}
