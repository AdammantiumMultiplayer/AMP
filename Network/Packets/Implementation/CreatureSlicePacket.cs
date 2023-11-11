using AMP.Data;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using Steamworks;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_SLICE)]
    public class CreatureSlicePacket : AMPPacket {
        [SyncedVar] public int  creatureId;
        [SyncedVar] public int  slicedPart;
        [SyncedVar] public byte section;

        public CreatureSlicePacket() { }

        public CreatureSlicePacket(int creatureId, int slicedPart, byte section) {
            this.creatureId = creatureId;
            this.slicedPart = slicedPart;
            this.section    = section;
        }

        public CreatureSlicePacket(int creatureId, RagdollPart.Type slicedPart, RagdollPart.Section section)
            : this( creatureId: creatureId
                  , slicedPart: (int) slicedPart
                  , section:    (byte) section
                  ) {
        }

        public CreatureSlicePacket(int creatureId, RagdollPart part)
            : this(creatureId: creatureId
                  , slicedPart: part.type
                  , section:    part.section
                  ) {
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];

                RagdollPart.Type ragdollPartType = (RagdollPart.Type) slicedPart;

                if(cnd.creature.ragdoll != null) {
                    RagdollPart rp = cnd.creature.ragdoll.GetPart(ragdollPartType, (RagdollPart.Section) section);
                    if(rp == null) {
                        rp = cnd.creature.ragdoll.GetPart(ragdollPartType);
                    }

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
