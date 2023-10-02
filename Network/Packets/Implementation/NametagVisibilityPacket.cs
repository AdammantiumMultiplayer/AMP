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

        public NametagVisibilityPacket() { }

        public NametagVisibilityPacket(bool is_visible) {
            this.is_visible = is_visible;
        }

        public override bool ProcessClient(NetamiteClient client) {
            HealthbarObject.defaultShowHealthBar = is_visible;
            HealthbarObject.defaultShowNameTag = is_visible;

            foreach(PlayerNetworkData pnd in ModManager.clientSync.syncData.players.Values) {
                if (pnd != null && pnd.networkCreature != null) {
                    if(pnd.networkCreature.healthBar != null) {
                        pnd.networkCreature.healthBar.SetHealthBarVisible(is_visible);
                        pnd.networkCreature.healthBar.SetNameVisible(is_visible);
                    }
                }
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
