using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.WELCOME)]
    public class WelcomePacket {
        [SyncedVar] public long playerId;

        public WelcomePacket(long playerId) {
            this.playerId = playerId;
        }
    }
}
