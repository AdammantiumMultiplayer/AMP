﻿using AMP.Datatypes;
using Netamite.Network.Packet.Attributes;
using System;
using ThunderRoad;

namespace AMP.Network.Data {
    [Serializable]
    public class ItemHoldingState {
        [SyncedVar] public int holderNetworkId = 0;
        [SyncedVar] public Holder.DrawSlot equipmentSlot = Holder.DrawSlot.None;
        [SyncedVar] public byte holdingIndex = 0;
        [SyncedVar] public byte orientationIndex = 0;
        [SyncedVar] public Side holdingSide;
        [SyncedVar] public ItemHolderType holderType;

        public ItemHoldingState() { }

        public ItemHoldingState(int holderNetworkId, byte holdingIndex, Side holdingSide, ItemHolderType holderType) {
            this.holderNetworkId = holderNetworkId;
            this.holdingIndex = holdingIndex;
            this.holdingSide = holdingSide;
            this.holderType = holderType;
        }

        public ItemHoldingState(int holderNetworkId, Holder.DrawSlot equipmentSlot, ItemHolderType holderType) {
            this.holderNetworkId = holderNetworkId;
            this.equipmentSlot = equipmentSlot;
            this.holderType = holderType;
        }

        public override string ToString() {
            if(equipmentSlot != Holder.DrawSlot.None) {
                return "slot " + equipmentSlot + " on " + holderType + " " + holderNetworkId;
            } else {
                return "hand " + holdingSide + " on " + holderType + " " + holderNetworkId;
            }
        }

        public override bool Equals(object obj) {
            if(obj is ItemHoldingState) {
                ItemHoldingState other = (ItemHoldingState) obj;
                return this.holderNetworkId == other.holderNetworkId
                    && this.equipmentSlot   == other.equipmentSlot
                    && this.holdingIndex    == other.holdingIndex
                    && this.holdingSide     == other.holdingSide
                    && this.holderType      == other.holderType;
            }
            return false;
        }

        public static bool Equals(ItemHoldingState[] list1, ItemHoldingState[] list2) {
            if(list1.Length != list2.Length) return false;

            foreach(ItemHoldingState item1 in list1) {
                bool has_match = false;
                foreach(ItemHoldingState item2 in list2) {
                    if(item1.Equals(item2)) { 
                        has_match = true;
                        break;
                    }
                }
                if(!has_match) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            int hashCode = -677144781;
            hashCode = hashCode * -1521134295 + holderNetworkId.GetHashCode();
            hashCode = hashCode * -1521134295 + equipmentSlot.GetHashCode();
            hashCode = hashCode * -1521134295 + holdingIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + holdingSide.GetHashCode();
            hashCode = hashCode * -1521134295 + holderType.GetHashCode();
            return hashCode;
        }
    }
}
