using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_DATA)]
    public class PlayerDataPacket {
        [SyncedVar]       public long    playerId;
        [SyncedVar]       public string  name;
        [SyncedVar]       public string  creatureId;
        [SyncedVar(true)] public float   height;
        [SyncedVar]       public Vector3 playerPos;
        [SyncedVar(true)] public float   playerRot;
    }
}
