using AMP.Extension;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_PLAY_ANIMATION)]
    public class CreatureAnimationPacket : AMPPacket {
        [SyncedVar] public long   creatureId;
        [SyncedVar] public string animationClip;

        public CreatureAnimationPacket() { }

        public CreatureAnimationPacket(long creatureId, string animationClip) {
            this.creatureId    = creatureId;
            this.animationClip = animationClip;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];
                if(cnd.creature == null) return true;
                if(cnd.isSpawning) return true;

                Dispatcher.Enqueue(() => {
                    cnd.creature.PlayAttackAnimation(animationClip);
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                if(ModManager.serverInstance.creature_owner[creatureId] != client.ClientId) return true;

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
