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

        [SyncedVar]       public Vector3 playerPos;
        [SyncedVar(true)] public float   playerRot;

    }
}
