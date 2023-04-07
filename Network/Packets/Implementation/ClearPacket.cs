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
    public class ClearPacket : NetPacket {
        [SyncedVar] public bool clearItems = true;
        [SyncedVar] public bool clearCreatures = true;
        [SyncedVar] public bool preventNewItems = true;

        public ClearPacket() { }

        public ClearPacket(bool clearItems, bool clearCreatures, bool preventNewItems) {
            this.clearItems = clearItems;
            this.clearCreatures = clearCreatures;
            this.preventNewItems = preventNewItems;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync == null) return true;
            if(ModManager.clientSync.syncData == null) return true;

            if(clearCreatures) {
                foreach(CreatureNetworkData cnd in ModManager.clientSync.syncData.creatures.Values.ToList()) {
                    if(cnd == null) continue;
                    if(cnd.creature == null) continue;

                    try {
                        Dispatcher.Enqueue(() => {
                            cnd?.creature?.Despawn();
                        });
                    } catch(Exception e) {
                        Log.Err(e);
                    }
                }
                lock(ModManager.clientSync.syncData.creatures) {
                    ModManager.clientSync.syncData.creatures.Clear();
                }
            }
            if(clearItems) {
                foreach(ItemNetworkData ind in ModManager.clientSync.syncData.items.Values.ToList()) {
                    if(ind == null) continue;
                    if(ind.clientsideItem == null) continue;

                    try {
                        Dispatcher.Enqueue(() => {
                            ind?.clientsideItem?.Despawn();
                        });
                    } catch(Exception e) {
                        Log.Err(e);
                    }
                }
                lock(ModManager.clientSync.syncData.items) {
                    ModManager.clientSync.syncData.items.Clear();
                }
            }

            if(preventNewItems) {
                ModManager.clientSync.unfoundItemMode = ClientSync.UnfoundItemMode.DESPAWN; 
            }
            return true;
        }
    }
}
