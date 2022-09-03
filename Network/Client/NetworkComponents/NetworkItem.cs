using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data.Sync;
using ThunderRoad;

namespace AMP.Network.Client.NetworkComponents {
    public class NetworkItem : NetworkPositionRotation {
        protected Item item;
        public ItemNetworkData itemNetworkData;

        public void Init(ItemNetworkData itemNetworkData) {
            if(this.itemNetworkData != itemNetworkData) registeredEvents = false;
            this.itemNetworkData = itemNetworkData;

            //Log.Warn("INIT Item");

            RegisterEvents();
        }

        void Awake() {
            OnAwake();
        }

        protected void OnAwake() {
            item = GetComponent<Item>();
        }

        public new bool IsOwning() {
            return itemNetworkData.clientsideId > 0;
        }

        private bool registeredEvents = false;
        public void RegisterEvents() {
            if(registeredEvents) return;

            itemNetworkData.clientsideItem.OnDespawnEvent += (item) => {
                if(itemNetworkData == null) return;
                if(itemNetworkData.networkedId <= 0) return;
                if(itemNetworkData.clientsideId > 0) { // Check if the item is already networked and is in ownership of the client
                    itemNetworkData.DespawnPacket().SendToServerReliable();
                    Log.Debug($"[Client] Event: Item {itemNetworkData.dataId} ({itemNetworkData.networkedId}) is despawned.");

                    ModManager.clientSync.syncData.items.Remove(itemNetworkData.networkedId);

                    itemNetworkData.networkedId = 0;
                } else {
                    //TODO Just respawn the item?
                }
            };

            // If the player grabs an item with telekenesis, we give him control over the position data
            itemNetworkData.clientsideItem.OnTelekinesisGrabEvent += (handle, teleGrabber) => {
                if(itemNetworkData == null) return;
                if(itemNetworkData.clientsideId > 0) return;
                if(itemNetworkData.networkedId <= 0) return;
                
                itemNetworkData.TakeOwnershipPacket().SendToServerReliable();
            };

            // If the player grabs an item, we give him control over the position data, and tell the others to attach it to the character
            //itemNetworkData.clientsideItem.OnGrabEvent += (handle, ragdollHand) => {
            //    if(itemNetworkData == null) return;
            //    if(itemNetworkData.networkedId <= 0) return;
            //    itemNetworkData.UpdateFromHolder();
            //
            //    if(itemNetworkData.drawSlot != Holder.DrawSlot.None || itemNetworkData.creatureNetworkId <= 0) return;
            //    if(!itemNetworkData.holderIsPlayer && (!ModManager.clientSync.syncData.creatures.ContainsKey(itemNetworkData.creatureNetworkId) || ModManager.clientSync.syncData.creatures[itemNetworkData.creatureNetworkId].clientsideId <= 0)) return;
            //
            //    if(itemNetworkData.clientsideId <= 0) {
            //        itemNetworkData.TakeOwnershipPacket().SendToServerReliable();
            //    }
            //
            //    Log.Debug($"[Client] Event: Grabbed item {itemNetworkData.dataId} by {itemNetworkData.creatureNetworkId} with hand {itemNetworkData.holdingSide}.");
            //
            //    if(itemNetworkData.creatureNetworkId > 0) {
            //        itemNetworkData.SnapItemPacket().SendToServerReliable();
            //    }
            //};
            //
            //// If the item is dropped by the player, drop it for everyone
            //itemNetworkData.clientsideItem.OnUngrabEvent += (handle, ragdollHand, throwing) => {
            //    if(itemNetworkData == null) return;
            //    if(!itemNetworkData.AllowSyncGrabEvent()) return;
            //    if(itemNetworkData.creatureNetworkId <= 0) return;
            //    //if(!ModManager.clientSync.syncData.creatures.ContainsKey(itemNetworkData.creatureNetworkId)) return;
            //
            //    itemNetworkData.UpdateFromHolder();
            //
            //    if(itemNetworkData.drawSlot != Holder.DrawSlot.None) return;
            //    if(!itemNetworkData.holderIsPlayer && (ModManager.clientSync.syncData.creatures.ContainsKey(itemNetworkData.creatureNetworkId) && ModManager.clientSync.syncData.creatures[itemNetworkData.creatureNetworkId].clientsideId <= 0)) return;
            //
            //    Log.Debug($"[Client] Event: Ungrabbed item {itemNetworkData.dataId} by {itemNetworkData.creatureNetworkId} with hand {itemNetworkData.holdingSide}.");
            //
            //    itemNetworkData.creatureNetworkId = 0;
            //    itemNetworkData.UnSnapItemPacket().SendToServerReliable();
            //};
            //
            //// If the item is equipped to a slot, do it for everyone
            //itemNetworkData.clientsideItem.OnSnapEvent += (holder) => {
            //    if(itemNetworkData == null) return;
            //    if(!itemNetworkData.AllowSyncGrabEvent()) return;
            //
            //    itemNetworkData.UpdateFromHolder();
            //
            //    if(itemNetworkData.creatureNetworkId > 0) {
            //        if(!itemNetworkData.holderIsPlayer && ModManager.clientSync.syncData.creatures[itemNetworkData.creatureNetworkId].clientsideId <= 0) return;
            //
            //        Log.Debug($"[Client] Event: Snapped item {itemNetworkData.dataId} to {itemNetworkData.creatureNetworkId} in slot {itemNetworkData.drawSlot}.");
            //
            //        itemNetworkData.SnapItemPacket().SendToServerReliable();
            //    }
            //};
            //
            //// If the item is unequipped by the player, do it for everyone
            //itemNetworkData.clientsideItem.OnUnSnapEvent += (holder) => {
            //    if(itemNetworkData == null) return;
            //    if(!itemNetworkData.AllowSyncGrabEvent()) return;
            //
            //    Log.Debug($"[Client] Event: Unsnapped item {itemNetworkData.dataId} from {itemNetworkData.creatureNetworkId}.");
            //    itemNetworkData.creatureNetworkId = 0;
            //
            //    itemNetworkData.UnSnapItemPacket().SendToServerReliable();
            //};
            //
            //// Check if the item is already equipped, if so, tell the others players
            //if((itemNetworkData.clientsideItem.holder != null && itemNetworkData.clientsideItem.holder.creature != null)
            //   || (itemNetworkData.clientsideItem.mainHandler != null && itemNetworkData.clientsideItem.mainHandler.creature != null)) {
            //    if(itemNetworkData.clientsideId <= 0) itemNetworkData.TakeOwnershipPacket().SendToServerReliable();
            //
            //    itemNetworkData.UpdateFromHolder();
            //
            //    if(itemNetworkData.creatureNetworkId <= 0) return;
            //    if(itemNetworkData.networkedId <= 0) return;
            //
            //    itemNetworkData.SnapItemPacket().SendToServerReliable();
            //} else {
            //    if(itemNetworkData.creatureNetworkId > 0) { // Update the hold state if the item is already held by a creature
            //        itemNetworkData.UpdateHoldState();
            //    }
            //}

            for(int i = 0; i < itemNetworkData.clientsideItem.imbues.Count; i++) {
                Imbue imbue = itemNetworkData.clientsideItem.imbues[i];
                int index = i;

                imbue.onImbueEnergyFilled += (spellData, amount, change, eventTime) => {
                    if(itemNetworkData.networkedId <= 0) return;
                    if(spellData != null && eventTime == EventTime.OnStart) {
                        itemNetworkData.CreateImbuePacket(spellData.id, index, amount + change).SendToServerReliable();
                    }
                };
                imbue.onImbueEnergyDrained += (spellData, amount, change, eventTime) => {
                    if(itemNetworkData.networkedId <= 0) return;
                    if(spellData != null && eventTime == EventTime.OnStart) {
                        itemNetworkData.CreateImbuePacket(spellData.id, index, amount + change).SendToServerReliable();
                    }
                };
                imbue.onImbueSpellChange += (spellData, amount, change, eventTime) => {
                    if(itemNetworkData.networkedId <= 0) return;
                    if(spellData != null && eventTime == EventTime.OnEnd) {
                        itemNetworkData.CreateImbuePacket(spellData.id, index, amount + change).SendToServerReliable();
                    }
                };
            }

            itemNetworkData.clientsideItem.OnHeldActionEvent += ((ragdollHand, handle, action) => {
                switch(action) {
                    case Interactable.Action.Grab:
                    case Interactable.Action.Ungrab:
                        break;
                    case Interactable.Action.AlternateUseStart:
                    case Interactable.Action.AlternateUseStop:
                    case Interactable.Action.UseStart:
                    case Interactable.Action.UseStop:
                        // TODO: Sync imbues
                        break;
                }
            });

            registeredEvents = true;
        }
    
        public void OnHoldStateChanged() {
            if(!IsOwning()) itemNetworkData.TakeOwnershipPacket().SendToServerReliable();

            itemNetworkData.UpdateFromHolder();
            itemNetworkData.SnapItemPacket().SendToServerReliable();
        }
    }
}
