using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MODERATION_VOTE_KICK)]
    public class ModerationVoteKickPacket : AMPPacket {
        [SyncedVar] public int ClientId;
        
        public ModerationVoteKickPacket() { }
        
        public ModerationVoteKickPacket(int ClientId) {
            this.ClientId = ClientId;
        }
        
        public override bool ProcessClient(NetamiteClient client) {
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            
            return true;
        }
    }
}
