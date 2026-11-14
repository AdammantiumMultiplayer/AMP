using AMP.GameInteraction.Components;
using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System.Linq;

namespace AMP.Network.Packets.Implementation {
    /// <summary>
    /// Shows a piece of paper with a text and action buttons in front of the player.
    /// The paper floats without gravity, can be grabbed and moved, and disappears
    /// once the player moves further away than closeDistance from where it spawned.
    /// Send it with ClientData.ShowPaperUI, so the server knows which actions are allowed.
    /// </summary>
    [PacketDefinition((byte) PacketType.UI_PAPER_SHOW)]
    public class PaperUIPacket : AMPPacket {
        [SyncedVar] public string   identifier    = "";
        [SyncedVar] public string   text          = "";
        // Two parallel arrays instead of a own class, so the default array converter can handle it
        [SyncedVar] public string[] buttonActions = new string[0];
        [SyncedVar] public string[] buttonLabels  = new string[0];
        [SyncedVar] public float    distance      = 0.5f; // Distance in front of the head where the paper spawns
        [SyncedVar] public float    closeDistance = 2.5f; // Paper disappears when the player is further away from the spawn point

        public PaperUIPacket() { }

        public PaperUIPacket(string identifier, string text) {
            this.identifier = identifier;
            this.text = text;
        }

        public PaperUIPacket(string identifier, string text, string[] buttonActions, string[] buttonLabels) : this(identifier, text) {
            this.buttonActions = buttonActions ?? new string[0];
            this.buttonLabels  = buttonLabels  ?? new string[0];
        }

        public override bool ProcessClient(NetamiteClient client) {
            // Netamite doesn't set empty arrays, so they can be null after reading
            if(buttonActions == null) buttonActions = new string[0];
            if(buttonLabels  == null) buttonLabels  = new string[0];

            Dispatcher.Enqueue(() => {
                PaperUI.Show(this);
            });
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            return true; // Clients are not allowed to send this
        }

        public PaperUIPacket AddButton(string action, string label) {
            buttonActions = (buttonActions ?? new string[0]).Append(action).ToArray();
            buttonLabels  = (buttonLabels  ?? new string[0]).Append(label).ToArray();
            return this;
        }

        public PaperUIPacket SetText(string text) {
            this.text = text;
            return this;
        }

        public PaperUIPacket SetDistance(float distance) {
            this.distance = distance;
            return this;
        }

        public PaperUIPacket SetCloseDistance(float closeDistance) {
            this.closeDistance = closeDistance;
            return this;
        }
    }
}
