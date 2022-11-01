using AMP.Network.Data.Sync;
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

        public ItemSpawnPacket() { }

        public ItemSpawnPacket(long itemId, string type, byte category, long clientsideId, Vector3 position, Vector3 rotation) {
            this.itemId       = itemId;
            this.type         = type;
            this.category     = category;
            this.clientsideId = clientsideId;
            this.position     = position;
            this.rotation     = rotation;
        }

        public ItemSpawnPacket(ItemNetworkData ind) 
            : this( itemId:       ind.networkedId
                  , type:         ind.dataId
                  , category:     (byte) ind.category
                  , clientsideId: ind.clientsideId
                  , position:     ind.position
                  , rotation:     ind.rotation
                  ) {

        }
    }
}
