using AMP.Data;
using AMP.Events;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using System;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ITEM_SPAWN)]
    public class ItemSpawnPacket : AMPPacket {
        [SyncedVar]       public int     itemId;
        [SyncedVar]       public string  type;
        [SyncedVar]       public byte    category;
        [SyncedVar]       public int     clientsideId;
        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public Vector3 rotation;
        [SyncedVar]       public bool    isMagicProjectile;

        public ItemSpawnPacket() { }

        public ItemSpawnPacket(int itemId, string type, byte category, int clientsideId, Vector3 position, Vector3 rotation, bool isMagicProjectile) {
            this.itemId       = itemId;
            this.type         = type;
            this.category     = category;
            this.clientsideId = clientsideId;
            this.position     = position;
            this.rotation     = rotation;
            this.isMagicProjectile = isMagicProjectile;
        }

        public ItemSpawnPacket(ItemNetworkData ind) 
            : this( itemId:       ind.networkedId
                  , type:         ind.dataId
                  , category:     (byte) ind.category
                  , clientsideId: ind.clientsideId
                  , position:     ind.position
                  , rotation:     ind.rotation
                  , isMagicProjectile: ind.isMagicProjectile
                  ) {

            ItemMagicProjectile projectile = ind.clientsideItem?.GetComponentInChildren<ItemMagicProjectile>(true);
            if(projectile != null && projectile.imbueSpellCastCharge != null) {
                SpellData spellData = projectile.imbueSpellCastCharge;
                this.type = spellData.id;
                this.isMagicProjectile = true;
            }
        }

        public override bool ProcessClient(NetamiteClient client) {
            bool already_existed_on_server = false;
            if(clientsideId < 0) {
                already_existed_on_server = true;
                clientsideId = Math.Abs(clientsideId);
            }

            if(ModManager.clientSync.syncData.items.ContainsKey(-clientsideId)) { // Item has been spawned by player
                ItemNetworkData exisitingSync = ModManager.clientSync.syncData.items[-clientsideId];
                exisitingSync.networkedId = itemId;

                ModManager.clientSync.syncData.items.TryRemove(-clientsideId, out _);

                if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) { // Item with that networking id is already spawned on the client, so no need to double it
                    if(ModManager.clientSync.syncData.items[itemId] != exisitingSync) {
                        Dispatcher.Enqueue(() => {
                            if(exisitingSync.clientsideItem == null) return;
                            if(exisitingSync.clientsideItem.isBrokenPiece) return;
                            if(exisitingSync.isSpawning) return;
                            if(exisitingSync.clientsideItem.IsHanded()) return;

                            exisitingSync.clientsideItem.Despawn();
                            ClientSync.PrintAreaStuff("Item 4");
                        });
                    } else {
                        Dispatcher.Enqueue(() => {
                            exisitingSync.ApplyPositionToItem();
                        });
                    }
                    return true;
                } else { // Assign item to its network Id
                    ModManager.clientSync.syncData.items.TryAdd(itemId, exisitingSync);
                }

                if(already_existed_on_server) { // Server told us he already knows about the item, so we unset the clientsideId to make sure we dont send unnessasary position updates
                    Log.Debug(Defines.CLIENT, $"Server knew about item {type} (Local: {exisitingSync.clientsideId} - Server: {itemId}) already (Probably map default item).");
                    exisitingSync.clientsideId = 0; // Server had the item already known, so reset that its been spawned by the player
                }

                Dispatcher.Enqueue(() => {
                    exisitingSync.StartNetworking();
                });
            } else { // Item has been spawned by other player or already existed in session
                if(ModManager.clientSync.syncData.items.ContainsKey(itemId)) {
                    //Spawner.TrySpawnItem(ModManager.clientSync.syncData.items[itemSpawnPacket.itemId]);
                    return true;
                }

                ItemNetworkData ind = new ItemNetworkData();
                ind.Apply(this);

                Dispatcher.Enqueue(() => {
                    ThunderRoad.Item item_found = SyncFunc.DoesItemAlreadyExist(ind, ThunderRoad.Item.allActive);

                    if(item_found == null) {
                        Spawner.TrySpawnItem(ind);
                    } else {
                        ind.clientsideItem = item_found;
                        //item_found.disallowDespawn = true;

                        Log.Debug(Defines.CLIENT, $"Item {ind.dataId} ({ind.networkedId}) matched with server.");

                        ind.StartNetworking();
                    }
                    ModManager.clientSync.syncData.items.TryAdd(ind.networkedId, ind);
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            ItemNetworkData ind = new ItemNetworkData();
            ind.Apply(this);

            //Log.Debug($"{itemId}  {type}  {category}");

            ItemNetworkData found = SyncFunc.DoesItemAlreadyExist(ind, ModManager.serverInstance.items.Values.ToList());
            if(found != null) ind.networkedId = found.networkedId;

            bool was_duplicate = false;
            if(ind.networkedId <= 0) {
                ind.networkedId = ModManager.serverInstance.NextItemId;
                ModManager.serverInstance.items.TryAdd(ind.networkedId, ind);
                ModManager.serverInstance.UpdateItemOwner(ind, client);
                Log.Debug(Defines.SERVER, $"{client.ClientName} has spawned item {ind.dataId} ({ind.networkedId})");
            } else {
                ind.clientsideId = -ind.clientsideId;
                Log.Debug(Defines.SERVER, $"{client.ClientName} has duplicate of {ind.dataId} ({ind.networkedId})");
                was_duplicate = true;
            }

            server.SendTo(client, new ItemSpawnPacket(ind));

            if(was_duplicate) return true; // If it was a duplicate, dont send it to other players

            ind.clientsideId = 0;

            server.SendToAllExcept(new ItemSpawnPacket(ind), client.ClientId);

            ServerEvents.InvokeOnItemSpawned(ind, client);

            Cleanup.CheckItemLimit(client);
            return true;
        }
    }
}
