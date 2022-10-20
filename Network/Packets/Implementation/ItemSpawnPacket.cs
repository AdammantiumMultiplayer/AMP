using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SPAWN)]
    public class ItemSpawnPacket : NetPacket {
        [SyncedVar]       public long    itemId;
        [SyncedVar]       public string  type;
        [SyncedVar]       public byte    category;
        [SyncedVar]       public long    clientsideId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;
    }
}
