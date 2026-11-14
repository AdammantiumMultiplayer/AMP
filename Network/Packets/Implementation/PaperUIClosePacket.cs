using AMP.Events;
using AMP.GameInteraction.Components;
using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    /// <summary>
    /// Server -> Client: Close the paper with this identifier.
    /// Client -> Server: The paper is gone on the client (player walked away, level change, ...).
    /// </summary>
    [PacketDefinition((byte) PacketType.UI_PAPER_CLOSE)]
    public class PaperUIClosePacket : AMPPacket {
        [SyncedVar] public string identifier = "";

        public PaperUIClosePacket() { }

        public PaperUIClosePacket(string identifier) {
            this.identifier = identifier;
        }

        public override bool ProcessClient(NetamiteClient client) {
            Dispatcher.Enqueue(() => {
                PaperUI.Close(identifier, false);
            });
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(identifier == null) return true;

            // Only tell the plugins if the paper was actually open, otherwise a client could spam the event
            if(client.openPaperUIs.TryRemove(identifier, out _)) {
                ServerEvents.InvokeOnPaperUIClosed(client, identifier);
            }
            return true;
        }
    }
}
