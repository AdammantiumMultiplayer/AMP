using AMP.Network.Data;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.PLAYER_TELEPORT)]
    internal class PlayerTeleportPacket : AMPPacket {
        [SyncedVar] public Vector3 targetPosition;
        [SyncedVar] public float targetRotation;

        public PlayerTeleportPacket() {

        }

        public PlayerTeleportPacket(Vector3 targetPosition, float targetRotation) {
            this.targetPosition = targetPosition;
            this.targetRotation = targetRotation;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(Player.local != null && Player.currentCreature != null) {
                Player.currentCreature.Teleport(targetPosition, Quaternion.Euler(0, targetRotation, 0));
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            // This is a packet only the server will send to the clients to force them to
            // positions. Its not allowed to get sent by a client.
            return true;
        }
    }
}
