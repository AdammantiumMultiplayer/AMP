using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_SLICE)]
    public class CreatureSlicePacket : AMPPacket {
        [SyncedVar] public int  creatureId;
        [SyncedVar] public int  slicedPart;

        public CreatureSlicePacket() { }

        public CreatureSlicePacket(int creatureId, int slicedPart) {
            this.creatureId = creatureId;
            this.slicedPart = slicedPart;
        }

        public CreatureSlicePacket(int creatureId, RagdollPart.Type slicedPart)
            : this( creatureId: creatureId
                  , slicedPart: (int) slicedPart
                  ) {
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];

                RagdollPart.Type ragdollPartType = (RagdollPart.Type) slicedPart;

                if(cnd.creature.ragdoll != null) {
                    RagdollPart rp = cnd.creature.ragdoll.GetPart(ragdollPartType);
                    if(rp != null) {
                        Dispatcher.Enqueue(() => {
                            cnd.creature.ragdoll.TrySlice(rp);
                        });
                    } else {
                        Log.Err(Defines.CLIENT, $"Couldn't slice off {ragdollPartType} from {creatureId}.");
                    }
                }
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
