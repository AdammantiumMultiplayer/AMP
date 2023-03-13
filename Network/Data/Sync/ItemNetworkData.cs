using AMP.Data;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class ItemNetworkData {
        #region Values
        internal long networkedId = 0;
        internal string dataId;
        internal ItemData.Type category;

        // Clientside Item Id, if 0 we dont own that item
        // Gets asigned when an item is first spawned
        internal long clientsideId = 0;
        internal Item clientsideItem;
        private NetworkItem _networkItem;
        internal NetworkItem networkItem {
            get {
                if(clientsideItem == null) return null;
                if(_networkItem == null) _networkItem = clientsideItem.GetComponent<NetworkItem>();
                return _networkItem;
            }
        }

        internal Vector3 position;
        internal Vector3 rotation;
        internal Vector3 velocity;
        internal Vector3 angularVelocity;

        internal Holder.DrawSlot drawSlot;
        internal byte holdingIndex = 0;
        internal Side holdingSide;
        internal bool holderIsPlayer = false;
        internal long creatureNetworkId = 0;

        internal bool isSpawning = false;
        #endregion

        #region Packet Generation and Reading
        internal void Apply(ItemSpawnPacket p) {
            networkedId  = p.itemId;
            dataId       = p.type;
            category     = (ItemData.Type) p.category;
            clientsideId = p.clientsideId;
            position     = p.position;
            rotation     = p.rotation;
        }

        internal void Apply(ItemPositionPacket p) {
            position        = p.position;
            rotation        = p.rotation;
            velocity        = p.velocity;
            angularVelocity = p.angularVelocity;
        }

        internal void Apply(ItemSnapPacket p) {
            creatureNetworkId = p.holderCreatureId;
            drawSlot          = (Holder.DrawSlot) p.drawSlot;
            holdingIndex      = p.holdingIndex;
            holdingSide       = (Side) p.holdingSide;
            holderIsPlayer    = p.holderIsPlayer;
        }

        internal void Apply(ItemUnsnapPacket p) {
            drawSlot          = Holder.DrawSlot.None;
            creatureNetworkId = 0;
            holdingIndex      = 0;
            holderIsPlayer    = false;
        }

        internal void PositionChanged() {
            if(clientsideItem != null) clientsideItem.lastInteractionTime = Time.time;
        }

        internal void ApplyPositionToItem() {
            if(networkItem == null) return;
            if(creatureNetworkId > 0) return;

            networkItem.targetPos = position;
            networkItem.targetRot = Quaternion.Euler(rotation);
            //clientsideItem.transform.position = position;
            //clientsideItem.transform.eulerAngles = rotation;
            //clientsideItem.rb.velocity = velocity;
            //clientsideItem.rb.angularVelocity = angularVelocity;
        }

        internal void UpdatePositionFromItem() {
            if(clientsideItem == null) return;

            position = clientsideItem.transform.position;
            rotation = clientsideItem.transform.eulerAngles;
            velocity = clientsideItem.rb.velocity;
            angularVelocity = clientsideItem.rb.angularVelocity;
        }

        internal void SetOwnership(bool owner) {
            if(owner) {
                if(clientsideId <= 0) clientsideId = ModManager.clientSync.syncData.currentClientItemId++;
            } else {
                clientsideId = 0;
            }
            networkItem?.UpdateItem();
        }

        internal void UpdateFromHolder() {
            if(clientsideItem == null) return;

            if(clientsideItem.holder != null && clientsideItem.holder.creature != null) {
                if(SyncFunc.GetCreature(clientsideItem.holder.creature, out holderIsPlayer, out creatureNetworkId)) {
                    holdingIndex = 0;
                    drawSlot = clientsideItem.holder.drawSlot;
                    return;
                }
            }

            // For future use, its for items stuck to the item
            //foreach(Holder holder in clientsideItem.childHolders) {
            //    Log.Debug(holder);
            //    if(holder.creature != null) {
            //        if(SyncFunc.GetCreature(clientsideItem.holder.creature, out holderIsPlayer, out creatureNetworkId)) {
            //            holdingIndex = 0;
            //            drawSlot = holder.drawSlot;
            //            break;
            //        }
            //    }
            //}

            byte counter = 1;
            foreach(Handle handle in clientsideItem.handles) {
                if(handle.handlers.Count > 0) {
                    RagdollHand ragdollHand = handle.handlers[0];
                    if(SyncFunc.GetCreature(ragdollHand.creature, out holderIsPlayer, out creatureNetworkId)) {
                        drawSlot = Holder.DrawSlot.None;
                        holdingIndex = counter;
                        holdingSide = ragdollHand.side;
                        return;
                    }
                }
                counter++;
            }

            drawSlot = Holder.DrawSlot.None;
            holdingIndex = 0;
            creatureNetworkId = 0;
        }

        internal void UpdateHoldState() {
            if(clientsideItem == null) return;

            if(creatureNetworkId <= 0) {
                if(clientsideItem.holder != null)
                    clientsideItem.holder.UnSnap(clientsideItem);
                if(clientsideItem.mainHandler != null)
                    clientsideItem.mainHandler.UnGrab(false);
            } else {
                Creature creature = null;
                string name = "";
                if(holderIsPlayer) {
                    if(ModManager.clientSync.syncData.players.ContainsKey(creatureNetworkId)) {
                        PlayerNetworkData ps = ModManager.clientSync.syncData.players[creatureNetworkId];
                        creature = ps.creature;
                        name = "player " + ps.name;
                    }
                } else {
                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureNetworkId)) {
                        CreatureNetworkData cs = ModManager.clientSync.syncData.creatures[creatureNetworkId];
                        creature = cs.creature;
                        name = "creature " + cs.creatureType;
                    }
                }

                if(creature == null) return;

                if(drawSlot == Holder.DrawSlot.None) { // its held in hand
                    
                    for(byte i = 1; i <= clientsideItem.handles.Count; i++) {
                        Handle handle = clientsideItem.handles[i - 1];
                        if(i == holdingIndex) {
                            if(! handle.handlers.Contains(creature.GetHand(holdingSide))) {
                                creature.GetHand(holdingSide).Grab(handle);
                            }
                        } else {
                            foreach(RagdollHand rh in handle.handlers) {
                                rh.UnGrab(false);
                            }
                        }
                    }

                    Log.Debug(Defines.CLIENT, $"Grabbed item {dataId} by {name} with hand {holdingSide}.");
                } else { // its in a equipment slot
                    creature.equipment.GetHolder(drawSlot).Snap(clientsideItem);

                    Log.Debug(Defines.CLIENT, $"Snapped item {dataId} to {name} with slot {drawSlot}.");
                }
                creature.RefreshCollisionOfGrabbedItems();
            }
        }
        #endregion

        #region Imbues
        internal void Apply(ItemImbuePacket p) {
            if(clientsideItem == null) return;

            if(clientsideItem.imbues.Count > p.index) {
                SpellCastCharge spellCastBase = Catalog.GetData<SpellCastCharge>(p.type, true);

                if(spellCastBase == null) {// If the client doesnt have the spell, just ignore it
                    Log.Err(Defines.CLIENT, $"Couldn't find spell {p.type}, please check you mods.");
                    return;
                }

                //spellCastBase = spellCastBase.Clone();

                Imbue imbue = clientsideItem.imbues[p.index];
                
                float energy = p.amount - imbue.energy;
                if(imbue.spellCastBase == null) energy = p.amount;
                imbue.Transfer(spellCastBase, energy);

                //spellCastBase.Load(imbue, spellCastBase.level);
                
                //imbue.spellCastBase = spellCastBase;
                //imbue.energy = energy;
            }
        }
        #endregion

        #region Check Functions
        internal bool AllowSyncGrabEvent() {
            if(networkedId < 0) return false;
            if(clientsideId < 0) return false;
            if(clientsideItem == null) return false;

            if(clientsideItem.GetComponentInParent<NetworkPlayerCreature>() != null) return false; // Custom creature is another player

            return holderIsPlayer || !(ModManager.clientSync.syncData.creatures.ContainsKey(creatureNetworkId) && ModManager.clientSync.syncData.creatures[creatureNetworkId].clientsideId <= 0);
        }
        #endregion
    }
}
