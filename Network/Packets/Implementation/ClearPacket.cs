using AMP.Network.Client;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using System.Linq;
using System;
using AMP.Logging;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CLEAR_DATA)]
    public class ClearPacket : NetPacket {
        [SyncedVar] public bool clearItems = true;
        [SyncedVar] public bool clearCreatures = true;

        public ClearPacket() { }

        public ClearPacket(bool clearItems, bool clearCreatures) {
            this.clearItems = clearItems;
            this.clearCreatures = clearCreatures;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync == null) return true;
            if(ModManager.clientSync.syncData == null) return true;

            if(clearCreatures) {
                foreach(CreatureNetworkData cnd in ModManager.clientSync.syncData.creatures.Values.ToList()) {
                    if(cnd == null) continue;
                    if(cnd.creature == null) continue;

                    try {
                        cnd.creature?.Despawn();
                    } catch(Exception e) {
                        Log.Err(e);
                    }
                }
                ModManager.clientSync.syncData.creatures.Clear();
            }
            if(clearItems) {
                foreach(ItemNetworkData ind in ModManager.clientSync.syncData.items.Values.ToList()) {
                    if(ind == null) continue;
                    if(ind.clientsideItem == null) continue;

                    try {
                        ind.clientsideItem?.Despawn();
                    } catch(Exception e) {
                        Log.Err(e);
                    }
                }
                ModManager.clientSync.syncData.items.Clear();
            }

            ModManager.clientSync.unfoundItemMode = ClientSync.UnfoundItemMode.DESPAWN;
            return true;
        }
    }
}
