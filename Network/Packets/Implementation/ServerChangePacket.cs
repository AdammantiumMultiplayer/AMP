using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Client.Implementation;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    /*
     * This packet allows a server to migrate the user to a different server.
     * This only allows connecting to a different port on the same server for now.
     * Why: Because im not sure if it could be abused in any way
     */
    [PacketDefinition((byte) PacketType.SERVER_CHANGE)]
    public class ServerChangePacket : AMPPacket {
        [SyncedVar] public ushort  port;

        public ServerChangePacket() { }

        public ServerChangePacket(ushort port) {
            this.port = port;
        }

        public override bool ProcessClient(NetamiteClient client) {
            ModManager.SwitchServer(port);
            
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            // Do nothing, no client is allowed to send a change server packet
            return true;
        }
    }
}
