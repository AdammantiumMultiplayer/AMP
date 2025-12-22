using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MODERATION_BAN)]
    public class ModerationBanPacket : AMPPacket {
        [SyncedVar] public int ClientId;
        
        public ModerationBanPacket() { }
        
        public ModerationBanPacket(int ClientId) {
            this.ClientId = ClientId;
        }
        
        public override bool ProcessClient(NetamiteClient client) {
            return true;
        }
        
        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            ClientData target = (ClientData)server.GetClientById(ClientId);
            if (target == null) return true;

            if (client.permissionLevel >= PermissionLevel.LOBBY_ADMIN) {
                if (client.permissionLevel >= PermissionLevel.MODERATOR) {
                    target.TempBan("You got locked out\n of the lobby by a moderator");
                } else {
                    target.TempBan("You got locked out\n by the lobby admin");
                }
            } else {
                Log.Warn($"{client.ClientName} tried to ban player ${ClientId}, but didn't have the permission.");
            }
            return true;
        }
    }
}
