using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.DISCONNECT)]
    public class DisconnectPacket : NetPacket {
        [SyncedVar] public long   playerId;
        [SyncedVar] public string reason;

        public DisconnectPacket() { }

        public DisconnectPacket(long playerId, string reason) {
            this.playerId = playerId;
            this.reason  = reason;
        }
    }
}
