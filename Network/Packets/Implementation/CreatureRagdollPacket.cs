using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_RAGDOLL)]
    public class CreatureRagdollPacket : NetPacket {
        [SyncedVar]       public long      creatureId;
        [SyncedVar]       public Vector3   position;
        [SyncedVar(true)] public float     rotationY;
        [SyncedVar(true)] public Vector3[] ragdollParts;
    }
}
