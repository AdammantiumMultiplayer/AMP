using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.PLAYER_POSITION)]
    public class PlayerPositionPacket : NetPacket {
        [SyncedVar]       public long    playerId;

        [SyncedVar(true)] public Vector3 handLeftPos;
        [SyncedVar(true)] public Vector3 handLeftRot;

        [SyncedVar(true)] public Vector3 handRightPos;
        [SyncedVar(true)] public Vector3 handRightRot;

        [SyncedVar(true)] public Vector3 headPos;
        [SyncedVar(true)] public Vector3 headRot;

        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public float   rotationY;

        public PlayerPositionPacket() { }

        public PlayerPositionPacket(long playerId, Vector3 handLeftPos, Vector3 handLeftRot, Vector3 handRightPos, Vector3 handRightRot, Vector3 headPos, Vector3 headRot, Vector3 playerPos, float playerRot) {
            this.playerId     = playerId;
            this.handLeftPos  = handLeftPos;
            this.handLeftRot  = handLeftRot;
            this.handRightPos = handRightPos;
            this.handRightRot = handRightRot;
            this.headPos      = headPos;
            this.headRot      = headRot;
            this.position    = playerPos;
            this.rotationY    = playerRot;
        }

        public PlayerPositionPacket(PlayerNetworkData pnd) 
            : this( playerId:     pnd.clientId
                  , handLeftPos:  pnd.handLeftPos
                  , handLeftRot:  pnd.handLeftRot
                  , handRightPos: pnd.handRightPos
                  , handRightRot: pnd.handRightRot
                  , headPos:      pnd.headPos
                  , headRot:      pnd.headRot
                  , playerPos:    pnd.position
                  , playerRot:    pnd.rotationY
                  ) {

        }
    }
}
