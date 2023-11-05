using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using System;
using System.ComponentModel;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class ItemNetworkData : NetworkData {
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

        internal Holder.DrawSlot equipmentSlot;
        internal byte holdingIndex = 0;
        internal Side holdingSide;
        internal ItemHolderType holderType = ItemHolderType.NONE;
        internal long holderNetworkId = 0;

        internal bool isMagicProjectile = false;

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
            isMagicProjectile = p.isMagicProjectile;
        }

        internal void Apply(ItemPositionPacket p) {
            position        = p.position;
            rotation        = p.rotation;
            velocity        = p.velocity;
            angularVelocity = p.angularVelocity;
        }

        internal void Apply(ItemSnapPacket p) {
            holderNetworkId = p.holderNetworkId;
            equipmentSlot   = (Holder.DrawSlot) p.drawSlot;
            holdingIndex    = p.holdingIndex;
            holdingSide     = (Side) p.holdingSide;
            holderType      = (ItemHolderType) p.holderType;
        }

        internal void Apply(ItemUnsnapPacket p) {
            equipmentSlot   = Holder.DrawSlot.None;
            holderNetworkId = 0;
            holdingIndex    = 0;
            holderType      = ItemHolderType.NONE;
        }

        internal void PositionChanged() {
            if(clientsideItem != null) clientsideItem.lastInteractionTime = Time.time;
        }

        internal void ApplyPositionToItem() {
            if(networkItem == null) return;
            if(holderNetworkId > 0) return;

            networkItem.targetPos = position;
            networkItem.positionVelocity = velocity;
            networkItem.targetRot = Quaternion.Euler(rotation);
            networkItem.rotationVelocity = angularVelocity;
            //clientsideItem.transform.position = position;
            //clientsideItem.transform.eulerAngles = rotation;
            //clientsideItem.rb.velocity = velocity;
            //clientsideItem.rb.angularVelocity = angularVelocity;
        }

        internal void UpdatePositionFromItem() {
            if(clientsideItem == null) return;

            position = clientsideItem.transform.position;
            rotation = clientsideItem.transform.eulerAngles;
            velocity = clientsideItem.physicBody.velocity;
            angularVelocity = clientsideItem.physicBody.angularVelocity;
            
            RecalculateDataTimestamp();
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
                if(SyncFunc.GetCreature(clientsideItem.holder.creature, out holderType, out holderNetworkId)) {
                    holdingIndex = 0;
                    equipmentSlot = clientsideItem.holder.drawSlot;
                    return;
                }

                if(clientsideItem.holder.parentItem != null) {
                    NetworkItem ni = clientsideItem.holder.parentItem.GetComponent<NetworkItem>();
                    if(ni != null) {
                        holderNetworkId = ni.itemNetworkData.networkedId;
                        holderType = ItemHolderType.ITEM;
                        holdingIndex = 0;
                        equipmentSlot = Holder.DrawSlot.None;
                    }
                }
            }

            byte counter = 1;
            foreach(Handle handle in clientsideItem.handles) {
                if(handle.handlers.Count > 0) {
                    RagdollHand ragdollHand = handle.handlers[0];
                    if(SyncFunc.GetCreature(ragdollHand.creature, out holderType, out holderNetworkId)) {
                        equipmentSlot = Holder.DrawSlot.None;
                        holdingIndex = counter;
                        holdingSide = ragdollHand.side;
                        return;
                    }
                }
                counter++;
            }

            equipmentSlot = Holder.DrawSlot.None;
            holdingIndex = 0;
            holderNetworkId = 0;
            holderType = ItemHolderType.NONE;
        }

        internal void UpdateHoldState() {
            if(clientsideItem == null) return;

            if(holderNetworkId <= 0) {
                if(clientsideItem.holder != null)
                    clientsideItem.holder.UnSnap(clientsideItem);

                if(clientsideItem.mainHandler != null) {
                    clientsideItem.mainHandler.UnGrab(false);
                    
                    if(clientsideItem?.mainHandler?.handles != null) {
                        foreach(HandleRagdoll hr in clientsideItem.mainHandler.handles) {
                            hr.Release();
                        }
                    }
                }

                foreach(Handle handle in clientsideItem.handles) {
                    handle.Release();
                }
            } else {
                if(holderType == ItemHolderType.ITEM) {
                    if(ModManager.clientSync.syncData.items.ContainsKey(holderNetworkId)) {
                        ItemNetworkData ind = ModManager.clientSync.syncData.items[holderNetworkId];
                        if(ind.clientsideItem == null) return;
                        if(ind.clientsideItem.childHolders.Count <= 0) return;

                        ind.clientsideItem.childHolders[0].Snap(clientsideItem);

                        Log.Debug(Defines.CLIENT, $"Put item {dataId} into {ind.dataId}.");
                    }
                } else {
                    Creature creature = null;
                    string name = "";
                    switch(holderType) {
                        case ItemHolderType.PLAYER:
                            if(ModManager.clientSync.syncData.players.ContainsKey(holderNetworkId)) {
                                PlayerNetworkData ps = ModManager.clientSync.syncData.players[holderNetworkId];
                                creature = ps.creature;
                                name = "player " + ps.name;
                            }
                            break;
                        case ItemHolderType.CREATURE:
                            if(ModManager.clientSync.syncData.creatures.ContainsKey(holderNetworkId)) {
                                CreatureNetworkData cs = ModManager.clientSync.syncData.creatures[holderNetworkId];
                                creature = cs.creature;
                                name = "creature " + cs.creatureType;
                            }
                            break;
                        default: break;
                    }
                
                    if(holderType == ItemHolderType.NONE) return;
                    if(creature == null) return;

                    if(equipmentSlot == Holder.DrawSlot.None) { // its held in hand
                    
                        for(byte i = 1; i <= clientsideItem.handles.Count; i++) {
                            Handle handle = clientsideItem.handles[i - 1];
                            if(i == holdingIndex) {
                                // Brute Force all other items to be ungrabbed - Hopefully this finally fixes it
                                RagdollHand rh = creature.GetHand(holdingSide);
                                foreach(Item item in Item.allActive) {
                                    foreach(Handle ihandle in item.handles) {
                                        if(ihandle == handle) continue;
                                        if(ihandle.handlers.Contains(rh)) {
                                            rh.UnGrab(ihandle);
                                        }
                                    }
                                }

                                if(!handle.handlers.Contains(rh)) {
                                    try {
                                        rh.Grab(handle);
                                    }catch(Exception e) {
                                        Log.Err(e);
                                    }
                                }
                            } else {
                                foreach(RagdollHand rh in handle.handlers) {
                                    rh.UnGrab(false);
                                }
                            }
                        }

                        Log.Debug(Defines.CLIENT, $"Grabbed item {dataId} by {name} with hand {holdingSide}.");
                    } else { // its in a equipment slot
                        // Brute Force all other items to be unsnapped - Hopefully this finally fixes it
                        Holder h = creature.equipment.GetHolder(equipmentSlot);
                        foreach(Item item in Item.allActive) {
                            if(item.holder == h) {
                                creature.equipment.GetHolder(equipmentSlot).UnSnap(item);
                            }
                        }

                        creature.equipment.GetHolder(equipmentSlot).Snap(clientsideItem);

                        Log.Debug(Defines.CLIENT, $"Snapped item {dataId} to {name} with slot {equipmentSlot}.");
                    }
                    creature.RefreshCollisionOfGrabbedItems();
                }
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

            return holderType == ItemHolderType.PLAYER || !(ModManager.clientSync.syncData.creatures.ContainsKey(holderNetworkId) && ModManager.clientSync.syncData.creatures[holderNetworkId].clientsideId <= 0);
        }
        #endregion
    }
}
