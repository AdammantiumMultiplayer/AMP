using AMP.Datatypes;
using AMP.GameInteraction;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.SupportFunctions;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.NAMETAG_VISIBILITY)]
    public class NametagVisibilityPacket : AMPPacket {
        [SyncedVar] public bool is_visible;
        [SyncedVar] public int[] affected_players = new int[0];

        public NametagVisibilityPacket() { }

        public NametagVisibilityPacket(bool is_visible) {
            this.is_visible       = is_visible;
        }

        public NametagVisibilityPacket(bool is_visible, params int[] affected_players) {
            this.is_visible       = is_visible;
            this.affected_players = affected_players;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(affected_players != null && affected_players.Length > 0) {
                foreach(var player in affected_players) {
                    HealthbarObject.SetHealthBarVisible(player, is_visible);
                    HealthbarObject.SetNameTagVisible(player, is_visible);
                }
            } else {
                HealthbarObject.SetHealthBarVisible(-1, is_visible);
                HealthbarObject.SetNameTagVisible(-1, is_visible);
            }

            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            //server.SendReliableToAllExcept(this, client.ClientId);
            // Its a server side packet
            return true;
        }
    }
}
