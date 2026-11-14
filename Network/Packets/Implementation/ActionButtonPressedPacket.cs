using AMP.Events;
using AMP.Network.Data;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System;

namespace AMP.Network.Packets.Implementation {
    /// <summary>
    /// Client -> Server: The player pressed a button on a paper UI.
    /// </summary>
    [PacketDefinition((byte) PacketType.UI_ACTION_BUTTON_PRESSED)]
    public class ActionButtonPressedPacket : AMPPacket {
        [SyncedVar] public string identifier = "";
        [SyncedVar] public string action     = "";

        public ActionButtonPressedPacket() { }

        public ActionButtonPressedPacket(string identifier, string action) {
            this.identifier = identifier;
            this.action = action;
        }

        public override bool ProcessClient(NetamiteClient client) {
            return true; // Only the server handles this
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(identifier == null || action == null) return true;

            // Only accept actions from papers the server actually opened for this client
            string[] allowedActions;
            if(!client.openPaperUIs.TryGetValue(identifier, out allowedActions)) return true;
            if(Array.IndexOf(allowedActions, action) < 0) return true;

            ServerEvents.InvokeOnActionButtonPressed(client, identifier, action);
            return true;
        }
    }
}
