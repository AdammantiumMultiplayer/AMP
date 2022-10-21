using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_RAGDOLL)]
    public class CreatureRagdollPacket : NetPacket {
        [SyncedVar]       public long      creatureId;
        [SyncedVar]       public Vector3   position;
        [SyncedVar(true)] public float     rotationY;
        [SyncedVar(true)] public Vector3[] ragdollParts;

        public CreatureRagdollPacket() { }

        public CreatureRagdollPacket(long creatureId, Vector3 position, float rotationY, Vector3[] ragdollParts) {
            this.creatureId   = creatureId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.ragdollParts = ragdollParts;
        }

        public CreatureRagdollPacket(CreatureNetworkData cnd) 
            : this( creatureId:   cnd.networkedId
                  , position:     cnd.position
                  , rotationY:    cnd.rotationY
                  , ragdollParts: cnd.ragdollParts
                  ){

        }
    }
}
