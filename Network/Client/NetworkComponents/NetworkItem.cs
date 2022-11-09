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

            //Log.Warn("INIT Item");

            RegisterEvents();
        }

        void Awake() {
            OnAwake();
        }

        protected void OnAwake() {
            item = GetComponent<Item>();
        }

        internal override bool IsSending() {
            return itemNetworkData.clientsideId > 0;
        }

        protected override void ManagedUpdate() {
            if(IsSending()) return;
            if(itemNetworkData.creatureNetworkId > 0) return;
            if(item.lastInteractionTime < Time.time - Config.NET_COMP_DISABLE_DELAY) return;

            //Log.Info("NetworkItem");

            base.ManagedUpdate();
        }

        #region Register Events
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

            //itemNetworkData.clientsideItem.OnHeldActionEvent += (ragdollHand, handle, action) => {
            //    switch(action) {
            //        case Interactable.Action.Grab:
            //        case Interactable.Action.Ungrab:
            //            break;
            //        case Interactable.Action.AlternateUseStart:
            //        case Interactable.Action.AlternateUseStop:
            //        case Interactable.Action.UseStart:
            //        case Interactable.Action.UseStop:
            //            break;
            //    }
            //};

            if(IsSending()) {
                itemNetworkData.UpdateFromHolder();
                if(itemNetworkData.creatureNetworkId > 0) {
                    new ItemSnapPacket(itemNetworkData).SendToServerReliable();
                }
            }

            registeredEvents = true;
        }
        #endregion

        #region Unregister Events
        protected override void ManagedOnDisable() {
            Destroy(this);
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
            if(itemNetworkData.clientsideId > 0) { // Check if the item is already networked and is in ownership of the client
                new ItemDespawnPacket(itemNetworkData).SendToServerReliable();
                Log.Debug(Defines.CLIENT, $"Event: Item {itemNetworkData.dataId} ({itemNetworkData.networkedId}) is despawned.");

                ModManager.clientSync.syncData.items.Remove(itemNetworkData.networkedId);

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

        internal void OnHoldStateChanged() {
            if(itemNetworkData == null) return;
            if(!IsSending()) new ItemOwnerPacket(itemNetworkData.networkedId, true).SendToServerReliable();

            itemNetworkData.UpdateFromHolder();

            if(itemNetworkData.creatureNetworkId > 0) {
                new ItemSnapPacket(itemNetworkData).SendToServerReliable();
            } else {
                new ItemUnsnapPacket(itemNetworkData).SendToServerReliable();
            }
        }
    }
}
