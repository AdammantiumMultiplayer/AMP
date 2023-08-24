using AMP.Discord;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.SERVER_INFO)]
    public class ServerInfoPacket : AMPPacket {
        [SyncedVar] public string version = "";
        [SyncedVar] public int    max_players = 99;

        public ServerInfoPacket() { }

        public ServerInfoPacket(string version, int max_players) {
            this.version = version;
            this.max_players = max_players;
        }


        public override bool ProcessClient(NetamiteClient client) {
            DiscordIntegration.Instance.UpdateActivity();
            return true;
        }
    }
}
