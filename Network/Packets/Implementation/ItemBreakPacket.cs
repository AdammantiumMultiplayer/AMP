using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_BREAK)]
    public class ItemBreakPacket : NetPacket {
        [SyncedVar] public long itemId;
        [SyncedVar(true)] public Vector3[] velocities;
        [SyncedVar(true)] public Vector3[] angularVelocities;

        public ItemBreakPacket() { }

        public ItemBreakPacket(long itemId, Vector3[] velocities, Vector3[] angularVelocities) {
            this.itemId = itemId;
            this.velocities = velocities;
            this.angularVelocities = angularVelocities;
        }
    }
}
