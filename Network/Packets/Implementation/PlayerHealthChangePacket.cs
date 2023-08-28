using AMP.Events;
using AMP.Logging;
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
                if(Player.currentCreature.isKilled) return true;
                Dispatcher.Enqueue(() => {
                    if(change > 0) {
                        Player.currentCreature.Heal(change);
                    } else {
                        if(Player.invincibility) {
                            Player.currentCreature.currentHealth -= change;

                            try {
                                if(Player.currentCreature.currentHealth <= 0) {
                                    Player.currentCreature.Kill();
                                }
                            } catch(NullReferenceException) { }

                            NetworkLocalPlayer.Instance.SendHealthPacket();
                        } else {
                            Player.currentCreature.Damage(Math.Abs(change));
                        }
                    }
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(!ModManager.safeFile.hostingSettings.pvpEnable) return true;
            if(ModManager.safeFile.hostingSettings.pvpDamageMultiplier <= 0) return true;

            if(change < 0) { // Its damage
                change *= ModManager.safeFile.hostingSettings.pvpDamageMultiplier;

                try { if(ServerEvents.OnPlayerDamaged != null) ServerEvents.OnPlayerDamaged.Invoke((ClientData) ModManager.serverInstance.netamiteServer.GetClientById(ClientId), change, client); } catch(Exception e) { Log.Err(e); }
            }

            server.SendTo(ClientId, this);
            return true;
        }
    }
}
