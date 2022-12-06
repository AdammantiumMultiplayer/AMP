using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    internal class NetworkItem : NetworkPositionRotation {
        protected Item item;
        internal ItemNetworkData itemNetworkData;

        internal void Init(ItemNetworkData itemNetworkData) {
            if(this.itemNetworkData != itemNetworkData) registeredEvents = false;
            this.itemNetworkData = itemNetworkData;

            targetPos = itemNetworkData.position;
            targetRot = Quaternion.Euler(itemNetworkData.rotation);

            RegisterEvents();
        }

        void Awake() {
            OnAwake();
        }

        protected void OnAwake() {
            item = GetComponent<Item>();
        }

        internal override bool IsSending() {
            return itemNetworkData != null && itemNetworkData.clientsideId > 0;
        }

        protected override void ManagedUpdate() {
            if(IsSending()) return;
            if(itemNetworkData.creatureNetworkId > 0) return;
            if(item.lastInteractionTime >= Time.time - Config.NET_COMP_DISABLE_DELAY) {
                base.ManagedUpdate();
            } else {
                if(!item.rb.useGravity) item.rb.useGravity = true;
            }
        }

        #region Register Events
        protected override void ManagedOnEnable() {
            if(registeredEvents) return;
            if(itemNetworkData == null) return;
            RegisterEvents();
        }

        private bool registeredEvents = false;
        internal void RegisterEvents() {
            if(registeredEvents) return;

            itemNetworkData.clientsideItem.OnDespawnEvent += Item_OnDespawnEvent;
            itemNetworkData.clientsideItem.OnTelekinesisGrabEvent += Item_OnTelekinesisGrabEvent;

            for(int i = 0; i < itemNetworkData.clientsideItem.imbues.Count; i++) {
                Imbue imbue = itemNetworkData.clientsideItem.imbues[i];
                int index = i;

                imbue.onImbueEnergyFilled += (spellData, amount, change, eventTime) => {
                    if(itemNetworkData.networkedId <= 0) return;
                    if(spellData != null && eventTime == EventTime.OnStart) {
                        new ItemImbuePacket(itemNetworkData.networkedId, spellData.id, index, amount + change).SendToServerReliable();
                    }
                };
                imbue.onImbueEnergyDrained += (spellData, amount, change, eventTime) => {
                    if(itemNetworkData.networkedId <= 0) return;
                    if(spellData != null && eventTime == EventTime.OnStart) {
                        new ItemImbuePacket(itemNetworkData.networkedId, spellData.id, index, amount + change).SendToServerReliable();
                    }
                };
                imbue.onImbueSpellChange += (spellData, amount, change, eventTime) => {
                    if(itemNetworkData.networkedId <= 0) return;
                    if(spellData != null && eventTime == EventTime.OnEnd) {
                        new ItemImbuePacket(itemNetworkData.networkedId, spellData.id, index, amount + change).SendToServerReliable();
                    }
                };
            }

            if(IsSending()) {
                OnHoldStateChanged();
            } else {
                itemNetworkData.UpdateHoldState();
            }
            UpdateItem();

            registeredEvents = true;
        }
        #endregion

        #region Unregister Events
        protected override void ManagedOnDisable() {
            UnregisterEvents();
        }

        internal void UnregisterEvents() {
            if(!registeredEvents) return;

            itemNetworkData.clientsideItem.OnDespawnEvent -= Item_OnDespawnEvent;
            itemNetworkData.clientsideItem.OnTelekinesisGrabEvent -= Item_OnTelekinesisGrabEvent;

            registeredEvents = false;
        }
        #endregion

        #region Events
        private void Item_OnDespawnEvent(EventTime eventTime) {
            if(!IsSending()) return;
            if(!ModManager.clientInstance.allowTransmission) return;
            if(itemNetworkData.clientsideId > 0) { // Check if the item is already networked and is in ownership of the client
                new ItemDespawnPacket(itemNetworkData).SendToServerReliable();
                Log.Debug(Defines.CLIENT, $"Event: Item {itemNetworkData.dataId} ({itemNetworkData.networkedId}) is despawned.");

                ModManager.clientSync.syncData.items.TryRemove(itemNetworkData.networkedId, out _);

                itemNetworkData.networkedId = 0;

                Destroy(this);
            }
        }

        // If the player grabs an item with telekenesis, we give him control over the position data
        private void Item_OnTelekinesisGrabEvent(Handle handle, SpellTelekinesis teleGrabber) {
            if(IsSending()) return;

            new ItemOwnerPacket(itemNetworkData.networkedId, true).SendToServerReliable();
        }
        #endregion

        internal bool hasSendedFirstTime = false;
        internal void OnHoldStateChanged() {
            if(itemNetworkData == null) return;

            Side holdingSide         = itemNetworkData.holdingSide;
            bool holderIsPlayer      = itemNetworkData.holderIsPlayer;
            Holder.DrawSlot drawSlot = itemNetworkData.drawSlot;
            long creatureNetworkId   = itemNetworkData.creatureNetworkId;

            itemNetworkData.UpdateFromHolder();

            if(  itemNetworkData.holdingSide       == holdingSide
              && itemNetworkData.holderIsPlayer    == holderIsPlayer
              && itemNetworkData.drawSlot          == drawSlot
              && itemNetworkData.creatureNetworkId == creatureNetworkId
              && hasSendedFirstTime) return; // Nothing changed so no need to send it again / Also check if it has even be sent, otherwise send it anyways. Side and Draw Slot have valid default values

            if(!IsSending()) new ItemOwnerPacket(itemNetworkData.networkedId, true).SendToServerReliable();

            hasSendedFirstTime = true;
            if(itemNetworkData.creatureNetworkId > 0) { // currently holded by a creature
                new ItemSnapPacket(itemNetworkData).SendToServerReliable();
            } else if(creatureNetworkId != 0) {         // was holded by a creature, but now is not anymore
                new ItemUnsnapPacket(itemNetworkData).SendToServerReliable();
            }
        }

        internal void UpdateItem() {
            bool owner = itemNetworkData.clientsideId > 0;
            
            if(item != null) {
                bool active = item.lastInteractionTime >= Time.time - Config.NET_COMP_DISABLE_DELAY;

                item.disallowDespawn = !owner;
                item.rb.useGravity = owner || !active;
            }
        }
    }
}
