using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_CHANGE)]
    public class PlayerHealthChangePacket : NetPacket {
        [SyncedVar] public int   ClientId;
        [SyncedVar] public float change;

        public PlayerHealthChangePacket() { }

        public PlayerHealthChangePacket(int ClientId, float change) {
            this.ClientId = ClientId;
            this.change   = change;
        }
    }
}
