using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.SERVER_INFO)]
    public class ServerInfoPacket : NetPacket {
        [SyncedVar] public int max_players = 99;

        public ServerInfoPacket() { }

        public ServerInfoPacket(int max_players) {
            this.max_players = max_players;
        }
    }
}
