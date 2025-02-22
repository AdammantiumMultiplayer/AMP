﻿using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Helper;
using AMP.Network.Packets.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class ItemNetworkData : NetworkData {
        #region Values
        public int networkedId = 0;
        internal string dataId;
        public ItemData.Type category;

        // Clientside Item Id, if 0 we dont own that item
        // Gets asigned when an item is first spawned
        internal int clientsideId = 0;
        public Item clientsideItem;
        private NetworkItem _networkItem;
        internal NetworkItem networkItem {
            get {
                if(clientsideItem == null) return null;
                if(_networkItem == null) _networkItem = clientsideItem.GetComponent<NetworkItem>();
                return _networkItem;
            }
        }

        public Vector3 position;
        public Vector3 rotation;
        internal Vector3 velocity;
        internal Vector3 angularVelocity;

        internal ItemHoldingState[] holdingStates = new ItemHoldingState[0];
        internal string holdingStatesInfo {
            get {
                List<string> states = new List<string>();
                
                if(holdingStates != null) {
                    foreach(ItemHoldingState state in holdingStates) {
                        states.Add(state.ToString());
                    }
                }
                return string.Join(", ", states.ToArray());
            }
        }

        internal float[] axisPosition = new float[0];

        internal bool isMagicProjectile = false;

        internal bool isSpawning = false;

        internal long lastPositionTimestamp = 0;
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
            if(p.timestamp < lastPositionTimestamp) return;

            lastPositionTimestamp = p.timestamp;

            position        = p.position;
            rotation        = p.rotation;
            velocity        = p.velocity;
            angularVelocity = p.angularVelocity;
        }

        internal void Apply(ItemSnapPacket p) {
            holdingStates   = p.itemHoldingStates;
        }

        internal void Apply(ItemUnsnapPacket p) {
            holdingStates   = new ItemHoldingState[0];
        }

        internal void Apply(ItemSlidePacket p) {
            axisPosition = p.axisPosition;
        }

        internal void PositionChanged() {
            if(networkItem != null) {
                networkItem.UpdateIfNeeded();
            }
        }

        internal void ApplyPositionToItem() {
            if(networkItem == null) return;
            if(holdingStates != null && holdingStates.Length > 0) return;

            networkItem.targetPos = position;
            networkItem.positionVelocity = velocity;
            networkItem.targetRot = Quaternion.Euler(rotation);
            networkItem.rotationVelocity = angularVelocity;
            //clientsideItem.transform.position = position;
            //clientsideItem.transform.eulerAngles = rotation;
            //clientsideItem.rb.velocity = velocity;
            //clientsideItem.rb.angularVelocity = angularVelocity;

            PositionChanged();
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
                ItemHoldingState state = new ItemHoldingState();
                if(SyncFunc.GetCreature(clientsideItem.holder.creature, out state.holderType, out state.holderNetworkId)) {
                    holdingStates = new ItemHoldingState[0];
                    state.equipmentSlot = clientsideItem.holder.drawSlot;
                    holdingStates = new ItemHoldingState[] { state };
                    return; // Just exit, no need to do more checks if the item is snapped somewhere on a creature
                }

                if(clientsideItem.holder.parentItem != null) {
                    NetworkItem ni = clientsideItem.holder.parentItem.GetComponent<NetworkItem>();
                    if(ni != null) {
                        holdingStates = new ItemHoldingState[] {
                            new ItemHoldingState(ni.itemNetworkData.networkedId, 0, Side.Right, ItemHolderType.ITEM)
                        };
                        
                        return; // Just exit, no need to do more checks if the item is snapped somewhere on another item
                    }
                }
            }

            List<ItemHoldingState> states = new List<ItemHoldingState>();
            List<float> axisPos = new List<float>();
            byte counter = 1;
            foreach(Handle handle in clientsideItem.handles) {
                foreach(RagdollHand ragdollHand in handle.handlers) {
                    ItemHoldingState itemHoldingState = new ItemHoldingState();
                    if(SyncFunc.GetCreature(ragdollHand.creature, out itemHoldingState.holderType, out itemHoldingState.holderNetworkId)) {
                        itemHoldingState.holdingIndex = counter;
                        itemHoldingState.holdingSide = ragdollHand.side;
                        if(ragdollHand.gripInfo != null) {
                            axisPos.Add(ragdollHand.gripInfo.axisPosition);
                            int index = handle.orientations.IndexOf(ragdollHand.gripInfo.orientation);
                            if(index != -1) {
                                itemHoldingState.orientationIndex = (byte) index;
                            }
                        } else {
                            axisPos.Add(0);
                            itemHoldingState.orientationIndex = 0;
                        }
                        states.Add(itemHoldingState);
                        // Continue, so dual weilding might finally work properly
                    }
                }
                counter++;
            }

            holdingStates = states.ToArray();
            axisPosition = axisPos.ToArray();

            // If there are no states, just set all values to none
            if(states.Count == 0) {
                axisPosition = new float[0];
            }
        }

        internal void UpdateSlidePos() {
            int cnt = 0;
            foreach(Handle handle in clientsideItem?.handles) {
                if(cnt >= axisPosition.Length) return;

                if(handle.handlers.Count > 0) {
                    if(handle.handlers[0].gripInfo != null) {
                        handle.handlers[0].gripInfo.axisPosition = axisPosition[cnt];
                        handle.UpdateHandle(handle.handlers[0]);
                    }
                    cnt++;
                }
            }
        }

        internal void UpdateHoldState() {
            if(clientsideItem == null) return;

            if(holdingStates == null || holdingStates.Length == 0) {
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

                clientsideItem.ResetRagdollCollision();
            } else {
                // Check that all hands that grab are registered as grabbing
                int index = 0;
                foreach(Handle handle in clientsideItem.handles) {
                    bool isHeld = holdingStates.Where(s => s.holdingIndex == index).ToArray().Length > 0;

                    if(!isHeld) {
                        handle?.Release();
                    }

                    index++;
                }

                // Check that all hands that should grab are grabbing
                foreach(ItemHoldingState holdingState in holdingStates) {
                    if(holdingState.holderType == ItemHolderType.ITEM) {
                        if(ModManager.clientSync.syncData.items.ContainsKey(holdingState.holderNetworkId)) {
                            ItemNetworkData ind = ModManager.clientSync.syncData.items[holdingState.holderNetworkId];
                            if(ind.clientsideItem == null) return;
                            if(ind.clientsideItem.childHolders.Count <= 0) return;

                            ind.clientsideItem.childHolders[0].Snap(clientsideItem);

                            Log.Debug(Defines.CLIENT, $"Put item {dataId} into {ind.dataId}.");

                            // TODO: Probably return? Needs more testing
                        }
                    } else {
                        Creature creature = null;
                        string name = "";
                        switch(holdingState.holderType) {
                            case ItemHolderType.PLAYER:
                                if(ModManager.clientSync.syncData.players.ContainsKey(holdingState.holderNetworkId)) {
                                    PlayerNetworkData ps = ModManager.clientSync.syncData.players[holdingState.holderNetworkId];
                                    creature = ps.creature;
                                    name = "player " + ps.name;
                                }
                                break;
                            case ItemHolderType.CREATURE:
                                if(ModManager.clientSync.syncData.creatures.ContainsKey(holdingState.holderNetworkId)) {
                                    CreatureNetworkData cs = ModManager.clientSync.syncData.creatures[holdingState.holderNetworkId];
                                    creature = cs.creature;
                                    name = "creature " + cs.creatureType;
                                }
                                break;
                            default: break;
                        }

                        if(holdingState.holderType == ItemHolderType.NONE) return;
                        if(creature == null) return;

                        if(holdingState.equipmentSlot == Holder.DrawSlot.None) { // its held in hand

                            for(byte i = 1; i <= clientsideItem.handles.Count; i++) {
                                Handle handle = clientsideItem.handles[i - 1];
                                if(i == holdingState.holdingIndex) {
                                    // Brute Force all other items from that hand to be ungrabbed - Hopefully this finally fixes it
                                    RagdollHand rh = creature.GetHand(holdingState.holdingSide);
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
                                            if(handle.orientations.Count <= holdingState.orientationIndex) holdingState.orientationIndex = 0;
                                            HandlePose handlePose = null;
                                            if(handle.orientations.Count > holdingState.orientationIndex) {
                                                handlePose = handle.orientations[holdingState.orientationIndex];
                                            }
                                            if(handlePose != null) {
                                                try {
                                                    rh.Grab(handle, handlePose, 0);
                                                }catch(Exception e) { Log.Err(e); }
                                            } else {
                                                try {
                                                    rh.Grab(handle);
                                                } catch(Exception e) { Log.Err(e); }
                                            }
                                            clientsideItem.IgnoreRagdollCollision(rh.ragdoll);
                                        } catch(Exception e) {
                                            Log.Err(e);
                                        }
                                    }
                                } else {
                                    /*foreach(RagdollHand rh in handle.handlers) {
                                        rh.UnGrab(false);
                                    }*/
                                }
                            }

                            Log.Debug(Defines.CLIENT, $"Grabbed item {dataId} by {name} with hand {holdingState.holdingSide}.");
                        } else { // its in a equipment slot
                                 // Brute Force all other items to be unsnapped - Hopefully this finally fixes it
                            Holder h = creature.equipment.GetHolder(holdingState.equipmentSlot);
                            foreach(Item item in Item.allActive) {
                                if(item.holder == h) {
                                    creature.equipment.GetHolder(holdingState.equipmentSlot).UnSnap(item);
                                }
                            }

                            creature.equipment.GetHolder(holdingState.equipmentSlot).Snap(clientsideItem);

                            Log.Debug(Defines.CLIENT, $"Snapped item {dataId} to {name} with slot {holdingState.equipmentSlot}.");
                        }
                        creature.RefreshCollisionOfGrabbedItems();
                    }
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
                if(imbue.spellCastBase != null && spellCastBase.hashId == imbue.spellCastBase.hashId) {
                    imbue.UnloadCurrentSpell();
                }
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

            ItemHolderType holderType = ItemHolderType.NONE;
            int holderNetworkId = 0;
            if(holdingStates.Length > 0) {
                holderType = holdingStates[0].holderType;
                holderNetworkId = holdingStates[0].holderNetworkId;
            }

            return holderType == ItemHolderType.PLAYER || !(ModManager.clientSync.syncData.creatures.ContainsKey(holderNetworkId) && ModManager.clientSync.syncData.creatures[holderNetworkId].clientsideId <= 0);
        }
        #endregion
    }
}
