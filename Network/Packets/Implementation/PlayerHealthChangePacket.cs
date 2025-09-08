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
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_CHANGE)]
    public class PlayerHealthChangePacket : AMPPacket {
        [SyncedVar] public int   ClientId;
        [SyncedVar] public float change;
        [SyncedVar] public bool  doneByPlayer;
        [SyncedVar] public Vector3 pushbackForce;

        public PlayerHealthChangePacket() { }

        public PlayerHealthChangePacket(int ClientId, float change, Vector3 pushbackForce, bool doneByPlayer = false) {
            this.ClientId      = ClientId;
            this.change        = change;
            this.doneByPlayer  = doneByPlayer;
            this.pushbackForce = pushbackForce;
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

                        Player.currentCreature.AddForce(pushbackForce, ForceMode.Impulse);
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
                pushbackForce *= client.GetPlayerPushbackMultiplicator();

                if(change >= 0) return true; // Damage is zero after the damage multiplication

                ClientData damaged = ModManager.serverInstance.GetClientById(ClientId);
                if(damaged != null) {

                    if(doneByPlayer) {
                        damaged.player.lastDamager = client;
                    } else {
                        damaged.player.lastDamager = null;
                    }
                    ServerEvents.InvokeOnPlayerDamaged(damaged, change, damaged.player.lastDamager);
                }
            }

            server.SendTo(ClientId, this);
            return true;
        }
    }
}
