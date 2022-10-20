using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_RAGDOLL)]
    public class PlayerRagdollPacket : NetPacket {
        [SyncedVar]       public long      creatureId;
        [SyncedVar]       public Vector3   position;
        [SyncedVar(true)] public float     rotationY;
        [SyncedVar(true)] public Vector3[] ragdollParts;

        public PlayerRagdollPacket(long creatureId, Vector3 position, float rotationY, Vector3[] ragdollParts) {
            this.creatureId   = creatureId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.ragdollParts = ragdollParts;
        }
    }
}
