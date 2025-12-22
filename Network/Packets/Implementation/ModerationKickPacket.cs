using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MODERATION_KICK)]
    public class ModerationKickPacket : AMPPacket {
        [SyncedVar] public int ClientId;
        
        public ModerationKickPacket() { }
        
        public ModerationKickPacket(int ClientId) {
            this.ClientId = ClientId;
        }
        
        public override bool ProcessClient(NetamiteClient client) {
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            ClientData target = (ClientData) server.GetClientById(ClientId);
            if (target == null) return true;
            
            if (client.permissionLevel >= PermissionLevel.LOBBY_ADMIN) {
                if (client.permissionLevel >= PermissionLevel.MODERATOR) {
                    target.Kick("You got kicked by a moderator");
                } else {
                    target.Kick("You got kicked by the lobby admin");
                }
            } else {
                Log.Warn($"{client.ClientName} tried to kick player ${ClientId}, but didn't have the permission.");
            }
            
            return true;
        }
    }
}
