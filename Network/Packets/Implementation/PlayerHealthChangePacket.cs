using AMP.Network.Client;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_CHANGE)]
    public class PlayerHealthChangePacket : NetPacket {
        [SyncedVar] public int   ClientId;
        [SyncedVar] public float change;

        public PlayerHealthChangePacket() { }

        public PlayerHealthChangePacket(int ClientId, float change) {
            this.ClientId = ClientId;
            this.change   = change;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ClientId == client.ClientId) {
                Player.currentCreature.currentHealth += change;

                try {
                    if(Player.currentCreature.currentHealth <= 0 && !Player.invincibility) {
                        Dispatcher.Enqueue(() => {
                            Player.currentCreature.Kill();
                        });
                    }
                } catch(NullReferenceException) { }

                NetworkLocalPlayer.Instance.SendHealthPacket();
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(!ModManager.safeFile.hostingSettings.pvpEnable) return true;
            if(ModManager.safeFile.hostingSettings.pvpDamageMultiplier <= 0) return true;

            if(change < 0) change *= ModManager.safeFile.hostingSettings.pvpDamageMultiplier;

            server.SendTo(ClientId, this);
            return true;
        }
    }
}
