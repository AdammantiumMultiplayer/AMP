using AMP.Events;
using AMP.Network.Client;
using AMP.Network.Data;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_CHANGE)]
    public class PlayerHealthChangePacket : AMPPacket {
        [SyncedVar] public int   ClientId;
        [SyncedVar] public float change;

        public PlayerHealthChangePacket() { }

        public PlayerHealthChangePacket(int ClientId, float change) {
            this.ClientId = ClientId;
            this.change   = change;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ClientId == client.ClientId) {
                if(Player.currentCreature == null) return true;
                if(!Player.currentCreature.initialized) return true;
                if(Player.currentCreature.isKilled) return true;

                Dispatcher.Enqueue(() => {
                    if(change > 0) {
                        Player.currentCreature.Heal(change);
                    } else {
                        if(Player.invincibility) {
                            Player.currentCreature.currentHealth -= Math.Abs(change);

                            try {
                                if(Player.currentCreature.currentHealth <= 0) {
                                    Player.currentCreature.Kill();
                                }
                            } catch(NullReferenceException) { }

                            NetworkLocalPlayer.Instance.SendHealthPacket();
                        } else {
                            Player.currentCreature?.Damage(Math.Abs(change));
                        }
                    }
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(!ModManager.safeFile.hostingSettings.pvpEnable) return true;

            if(client.GetDamageMultiplicator() <= 0) return true;

            if(change < 0) { // Its damage
                change *= client.GetDamageMultiplicator();

                if(change >= 0) return true; // Damage is zero after the damage multiplication

                ClientData damaged = ModManager.serverInstance.GetClientById(ClientId);
                if(damaged != null) {
                    damaged.player.lastDamager = client; 
                    ServerEvents.InvokeOnPlayerDamaged(damaged, change, client);
                }
            }

            server.SendTo(ClientId, this);
            return true;
        }
    }
}
