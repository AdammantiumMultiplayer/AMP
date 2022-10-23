using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.SERVER_STATUS_PING)]
    public class ServerPingPacket : NetPacket {
        [SyncedVar] public string server_name;
        [SyncedVar] public string server_icon; // URL or Base64, im not sure yet
        [SyncedVar] public short  connected_players;
        [SyncedVar] public short  max_players;
        [SyncedVar] public string map_name;
        [SyncedVar] public string map_mode;
        [SyncedVar] public bool   pvp_enabled;

        public ServerPingPacket() { }
    }
}
