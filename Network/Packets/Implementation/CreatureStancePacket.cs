using AMP.Extension;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    /// <summary>
    /// Syncs the stance (idle, fists, shield, dual wield, bow, ...) and the idle pose within that stance
    /// </summary>
    [PacketDefinition((byte) PacketType.CREATURE_STANCE)]
    public class CreatureStancePacket : AMPPacket {
        [SyncedVar] public int    creatureId;
        [SyncedVar] public string stanceId;
        [SyncedVar] public string idlePoseId;

        public CreatureStancePacket() { }

        public CreatureStancePacket(int creatureId, string stanceId, string idlePoseId) {
            this.creatureId = creatureId;
            this.stanceId   = stanceId;
            this.idlePoseId = idlePoseId;
        }

        public CreatureStancePacket(CreatureNetworkData cnd)
            : this(cnd.networkedId, cnd.stanceId, cnd.idlePoseId) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];
                // Remember it, so it can be applied once the creature has finished spawning
                cnd.stanceId   = stanceId;
                cnd.idlePoseId = idlePoseId;

                if(cnd.isSpawning) return true;

                Dispatcher.Enqueue(() => {
                    if(cnd.creature == null) return;
                    if(cnd.networkCreature != null && cnd.networkCreature.IsSending()) return; // We are the owner by now, our brain takes care of it

                    cnd.creature.ApplyStance(stanceId, idlePoseId);
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                int owner;
                if(!ModManager.serverInstance.creature_owner.TryGetValue(creatureId, out owner) || owner != client.ClientId) return true;

                // Keep it, so players joining later get the current stance as well
                CreatureNetworkData cnd = ModManager.serverInstance.creatures[creatureId];
                cnd.stanceId   = stanceId;
                cnd.idlePoseId = idlePoseId;

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
