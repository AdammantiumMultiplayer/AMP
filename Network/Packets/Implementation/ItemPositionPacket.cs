using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_POSITION)]
    public class ItemPositionPacket : NetPacket {
        [SyncedVar]       public long    itemId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;
        [SyncedVar(true)] public Vector3 velocity;
        [SyncedVar(true)] public Vector3 angularVelocity;
    }
}
