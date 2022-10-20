using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;
using System.Net.Sockets;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_RAGDOLL)]
    public class PlayerRagdollPacket : NetPacket {
        [SyncedVar]       public long      playerId;
        [SyncedVar]       public Vector3   position;
        [SyncedVar(true)] public float     rotationY;
        [SyncedVar(true)] public Vector3[] ragdollParts;

        public PlayerRagdollPacket(long playerId, Vector3 position, float rotationY, Vector3[] ragdollParts) {
            this.playerId     = playerId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.ragdollParts = ragdollParts;
        }

        public PlayerRagdollPacket(PlayerNetworkData pnd) : this(pnd.clientId, pnd.playerPos, pnd.playerRot, null) {
            ragdollParts = new Vector3[pnd.ragdollParts.Length];

            for(byte i = 0; i < ragdollParts.Length; i++) {
                Vector3 offset = pnd.ragdollParts[i];
                if(i % 2 == 0) offset -= pnd.playerPos; // Remove offset only to positions, they are at the even indexes
                ragdollParts[i] = offset;
            }
        }
    }
}
