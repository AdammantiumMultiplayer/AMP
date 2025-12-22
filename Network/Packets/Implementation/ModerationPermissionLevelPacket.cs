using AMP.Datatypes;
using AMP.Network.Data;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MODERATION_PERMISSION_LEVEL)]
    public class ModerationPermissionLevelPacket : AMPPacket {
        [SyncedVar] public PermissionLevel PermLevel;

        public ModerationPermissionLevelPacket() { }
        
        public ModerationPermissionLevelPacket(PermissionLevel PermLevel) {
            this.PermLevel = PermLevel;
        }
        
        public override bool ProcessClient(NetamiteClient client) {
            ModManager.clientSync.syncData.permissionLevel = PermLevel;
            
            return true;
        }
        
        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            return true;
        }
    }
}
