using AMP.Data;
using AMP.Logging;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_BREAK)]
    public class ItemBreakPacket : NetPacket {
        [SyncedVar] public long itemId;
        [SyncedVar(true)] public Vector3[] velocities;
        [SyncedVar(true)] public Vector3[] angularVelocities;

        public ItemBreakPacket() { }

        public ItemBreakPacket(long itemId, Vector3[] velocities, Vector3[] angularVelocities) {
            this.itemId = itemId;
            this.velocities = velocities;
            this.angularVelocities = angularVelocities;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData itemNetworkData = ModManager.clientSync.syncData.items[itemId];

                Breakable breakable = itemNetworkData?.clientsideItem?.GetComponent<Breakable>();
                if(breakable != null) {
                    breakable.Break();

                    for(int i = 0; i < breakable.subBrokenBodies.Count; i++) {
                        if(breakable.subBrokenBodies.Count <= i) break;
                        PhysicBody pb = breakable.subBrokenBodies[i];
                        pb.velocity = velocities[i];
                        pb.angularVelocity = angularVelocities[i];
                    }

                    Log.Debug(Defines.SERVER, $"Broke item {itemNetworkData.dataId}.");
                }
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                ItemNetworkData ind = ModManager.clientSync.syncData.items[itemId];

                Log.Debug(Defines.SERVER, $"Broke item {ind.dataId} by {client.ClientName}.");

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
