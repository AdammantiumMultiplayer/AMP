using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.SERVER_INFO)]
    public class ServerInfoPacket : NetPacket {
        [SyncedVar] public string version = "";
        [SyncedVar] public int    max_players = 99;

        public ServerInfoPacket() { }

        public ServerInfoPacket(string version, int max_players) {
            this.max_players = max_players;
        }
    }
}
