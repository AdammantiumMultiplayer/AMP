using AMP.Datatypes;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.SIZE_CHANGE)]
    public class SizeChangePacket : AMPPacket {
        [SyncedVar] public byte    type;
        [SyncedVar] public int     id;
        [SyncedVar] public Vector3 size;

        public SizeChangePacket() { }

        public SizeChangePacket(ItemHolderType type, int id, Vector3 size) {
            this.type = (byte) type;
            this.id   = id;
            this.size = size;
        }

        public SizeChangePacket(ItemHolderType type, int id, float height) 
            : this(type:  type
                  , id:   id
                  , size: new Vector3(1, height, 1)
                  ) {

        }

        public SizeChangePacket(PlayerNetworkData pnd) 
            : this( type: ItemHolderType.PLAYER,
                    id:   pnd.clientId,
                    height: pnd.creature.GetHeight()
                  ) {

        }

        public SizeChangePacket(CreatureNetworkData cnd)
            : this(type:    ItemHolderType.CREATURE,
                    id:     cnd.networkedId,
                    height: cnd.creature.GetHeight()
                  ) {
            }

        public override bool ProcessClient(NetamiteClient client) {
            Creature creature = null;
            switch((ItemHolderType) type) {
                case ItemHolderType.ITEM:
                    if(ModManager.serverInstance.items.ContainsKey(id)) {
                        ItemNetworkData ind = ModManager.serverInstance.items[id];
                        if(ind.clientsideItem != null) {
                            ind.clientsideItem.transform.localScale = size;
                        }
                    }
                    break;
                case ItemHolderType.PLAYER:
                    if(ModManager.clientSync.syncData.players.ContainsKey(id)) {
                        PlayerNetworkData ps = ModManager.clientSync.syncData.players[id];
                        ps.height = size.y;
                        creature = ps.creature;
                    }
                    break;
                case ItemHolderType.CREATURE:
                    if(ModManager.clientSync.syncData.creatures.ContainsKey(id)) {
                        CreatureNetworkData cs = ModManager.clientSync.syncData.creatures[id];
                        cs.height = size.y;
                        creature = cs.creature;
                    }
                    break;
                default: break;
            }

            if(creature != null) {
                creature.SetHeight(size.y);
            }

            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            server.SendReliableToAllExcept(this, client.ClientId);

            return true;
        }
    }
}
