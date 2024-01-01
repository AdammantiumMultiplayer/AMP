using AMP.Datatypes;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_FOLLOW)]
    public class CreatureFollowPacket : AMPPacket {
        [SyncedVar] public int creatureId;
        [SyncedVar] public ItemHolderType followType;
        [SyncedVar] public int followId;

        public CreatureFollowPacket() { }

        public CreatureFollowPacket(int creatureId, ItemHolderType followType, int followId) {
            this.creatureId = creatureId;
            this.followType = followType;
            this.followId   = followId;
        }

        public CreatureFollowPacket(int creatureId, ClientData client) {
            this.creatureId = creatureId;
            this.followType = ItemHolderType.PLAYER;
            this.followId   = client.ClientId;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];

                if(followId >= 0 && cnd.creature != null && cnd.creature.brain != null && cnd.creature.brain.instance != null) {
                    Creature creature = SyncFunc.GetCreature(followType, followId, true);

                    if(creature != null) {
                        BrainModuleFollow module = cnd.creature.brain.instance.GetModule<BrainModuleFollow>();
                        if(module != null) {
                            module.StartFollowing(creature);
                        }
                    }
                }

            }
            return true;
        }
    }
}
