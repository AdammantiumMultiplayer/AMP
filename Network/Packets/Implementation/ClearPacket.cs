using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using System;
using System.Linq;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CLEAR_DATA)]
    public class ClearPacket : AMPPacket {
        [SyncedVar] public bool clearItems = true;
        [SyncedVar] public bool clearCreatures = true;

        public ClearPacket() { }

        public ClearPacket(bool clearItems, bool clearCreatures) {
            this.clearItems = clearItems;
            this.clearCreatures = clearCreatures;
        }

        public override bool ProcessClient(NetamiteClient client) {
            var sync = ModManager.clientSync;
            if(sync == null) return true;
            if(sync.syncData == null) return true;

            if(clearCreatures) {
                foreach(CreatureNetworkData cnd in sync.syncData.creatures.Values.ToList()) {
                    if(cnd == null) continue;
                    if(cnd.creature == null) continue;

                    try {
                        Dispatcher.Enqueue(() => {
                            cnd?.creature?.Despawn();
                        });
                    } catch(Exception e) {
                        Log.Err(e);
                    }
                    ClientSync.PrintAreaStuff("Creature 3");
                }
                sync.syncData.creatures.Clear();
            }
            if(clearItems) {
                foreach(ItemNetworkData ind in sync.syncData.items.Values.ToList()) {
                    if(ind == null) continue;
                    if(ind.clientsideItem == null) continue;

                    try {
                        Dispatcher.Enqueue(() => {
                            ind?.clientsideItem?.Despawn();
                        });
                    } catch(Exception e) {
                        Log.Err(e);
                    }

                    ClientSync.PrintAreaStuff("Item 2");
                }
                sync.syncData.items.Clear();
                ModManager.clientInstance.clearedItems = true;
            }

            return true;
        }
    }
}
