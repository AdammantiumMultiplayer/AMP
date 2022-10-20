using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.DISCONNECT)]
    public class DisconnectPacket : NetPacket {
        [SyncedVar] public long   playerId;
        [SyncedVar] public string message;

        public DisconnectPacket(long playerId, string message) {
            this.playerId = playerId;
            this.message  = message;
        }
    }
}
