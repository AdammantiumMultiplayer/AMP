using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(false, (byte) PacketType.PLAYER_RAGDOLL)]
    public class PlayerRagdollPacket : NetPacket {
        [SyncedVar]       public long         timestamp; // This Timestamp is the client timestamp including the server time offset, so its basically the server time
        [SyncedVar]       public long         playerId;
        [SyncedVar]       public Vector3      position;
        [SyncedVar(true)] public float        rotationY;
        [SyncedVar(true)] public Vector3      velocity;
        [SyncedVar(true)] public float        rotationYVel;
        [SyncedVar(true)] public Vector3[]    ragdollPositions;
        [SyncedVar(true)] public Quaternion[] ragdollRotations;
        [SyncedVar(true)] public Vector3[]    velocities;
        [SyncedVar(true)] public Vector3[]    angularVelocities;

        public PlayerRagdollPacket() { }

        public PlayerRagdollPacket(long timestamp, long playerId, Vector3 position, float rotationY, Vector3 velocity, float rotationYVel, Vector3[] ragdollPositions, Quaternion[] ragdollRotations, Vector3[] velocities, Vector3[] angularVelocities) {
            this.timestamp    = timestamp;
            this.playerId     = playerId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.velocity     = velocity;
            this.rotationYVel = rotationYVel;
            this.ragdollPositions  = ragdollPositions;
            this.ragdollRotations  = ragdollRotations;
            this.velocities        = velocities;
            this.angularVelocities = angularVelocities;
        }

        public PlayerRagdollPacket(PlayerNetworkData pnd)
            : this( timestamp: pnd.dataTimestamp
                  , playerId:  pnd.clientId
                  , position:  pnd.position
                  , rotationY: pnd.rotationY
                  , velocity:  pnd.velocity
                  , rotationYVel:      pnd.rotationYVel
                  , ragdollPositions:  pnd.ragdollPositions
                  , ragdollRotations:  pnd.ragdollRotations
                  , velocities:        pnd.ragdollVelocity
                  , angularVelocities: pnd.ragdollAngularVelocity
                  ) {

        }
    }
}
