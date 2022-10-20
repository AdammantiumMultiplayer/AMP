using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_POSITION)]
    public class CreaturePositionPacket : NetPacket {
        [SyncedVar]       public long    creatureId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;

        public CreaturePositionPacket(long creatureId, Vector3 position, Vector3 rotation) {
            this.creatureId = creatureId;
            this.position   = position;
            this.rotation   = rotation;
        }
    }
}
