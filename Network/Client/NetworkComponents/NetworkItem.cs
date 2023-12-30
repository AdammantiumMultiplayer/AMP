using AMP.Data;
using AMP.Datatypes;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using Netamite.Helper;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    internal class NetworkItem : NetworkPositionRotation {
        protected Item item;
        internal ItemNetworkData itemNetworkData;

        private bool isKinematicItem = false;

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

        void Start() {
            if(item == null) return;

            isKinematicItem = item.physicBody.isKinematic;
            if(item.holder != null || item.isGripped || item.handlers?.Count > 0) {
                isKinematicItem = false;
            }
        }

        internal override bool IsSending() {
            return itemNetworkData != null && itemNetworkData.clientsideId > 0;
        }

        internal float lastTime = 0f;
        public override void ManagedUpdate() {
            if(IsSending()) return;
            if(itemNetworkData.holdingStates == null || itemNetworkData.holdingStates.Length > 0) return;

            if(itemNetworkData.lastPositionTimestamp >= Time.time - Config.NET_COMP_DISABLE_DELAY) {
                if(lastTime > 0) UpdateItem();
                lastTime = 0f;

                base.ManagedUpdate();
            } else if((int) lastTime != (int) Time.time) {
                if(lastTime == 0) UpdateItem();
                lastTime = Time.time;

                transform.rotation = targetRot;
                transform.position = targetPos;
            }
        }

        #region Register Events
        public override void ManagedOnEnable() {
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

            foreach(Handle handle in itemNetworkData.clientsideItem.handles) {
                handle.SlidingStateChange += Handle_SlidingStateChange;
            }

            if(IsSending()) {
                OnHoldStateChanged();
            } else {
                itemNetworkData.UpdateHoldState();
            }
            UpdateItem();

            registeredEvents = true;
        }

        private void Handle_SlidingStateChange(RagdollHand ragdollHand, bool sliding, Handle handle, float position, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            if(!IsSending()) return;

            itemNetworkData.axisPosition = position;
            new ItemSlidePacket(itemNetworkData).SendToServerUnreliable();
        }
        #endregion

        #region Unregister Events
        public override void ManagedOnDisable() {
            UnregisterEvents();
        }

        internal void UnregisterEvents() {
            if(!registeredEvents) return;

            itemNetworkData.clientsideItem.OnDespawnEvent -= Item_OnDespawnEvent;
            itemNetworkData.clientsideItem.OnTelekinesisGrabEvent -= Item_OnTelekinesisGrabEvent;

            foreach(Handle handle in itemNetworkData.clientsideItem.handles) {
                handle.SlidingStateChange -= Handle_SlidingStateChange;
            }

            registeredEvents = false;
        }
        #endregion

        #region Events
        internal void OnBreak(Breakable breakable, PhysicBody[] pieces) {
            if(!IsSending()) new ItemOwnerPacket(itemNetworkData.networkedId, true).SendToServerReliable();

            Vector3[] velocities = new Vector3[breakable.subBrokenBodies.Count];
            Vector3[] angularVelocities = new Vector3[breakable.subBrokenBodies.Count];
            for(int i = 0; i < velocities.Length; i++) {
                velocities[i] = breakable.subBrokenBodies[i].velocity * 10;
                angularVelocities[i] = breakable.subBrokenBodies[i].angularVelocity;
            }
            new ItemBreakPacket(itemNetworkData.networkedId, velocities, angularVelocities).SendToServerReliable();
        }

        private void Item_OnDespawnEvent(EventTime eventTime) {
            if(!IsSending()) return;
            if(!ModManager.clientInstance.allowTransmission) return;

            if(itemNetworkData.clientsideId > 0 && itemNetworkData.networkedId > 0) { // Check if the item is already networked and is in ownership of the client
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
            itemNetworkData.SetOwnership(true);
        }
        #endregion

        internal bool hasSendedFirstTime = false;
        internal void OnHoldStateChanged() {
            if(itemNetworkData == null) return;

            ItemHoldingState[] holdingStates = itemNetworkData.holdingStates;
            //float axisPosition        = itemNetworkData.axisPosition;

            itemNetworkData.UpdateFromHolder();

            //if(itemNetworkData.axisPosition != axisPosition) {
            //    new ItemSlidePacket(itemNetworkData).SendToServerReliable();
            //}

            if(  hasSendedFirstTime
              && !ItemHoldingState.Equals(itemNetworkData.holdingStates, holdingStates)) return; // Nothing changed so no need to send it again / Also check if it has even be sent, otherwise send it anyways. Side and Draw Slot have valid default values

            if(!IsSending()) {
                new ItemOwnerPacket(itemNetworkData.networkedId, true).SendToServerReliable();
                itemNetworkData.SetOwnership(true);
            }

            hasSendedFirstTime = true;
            if(itemNetworkData.holdingStates.Length > 0) {  // currently held by a creature
                new ItemSnapPacket(itemNetworkData).SendToServerReliable();
            } else {                                        // was held by a creature, but now is not anymore
                new ItemUnsnapPacket(itemNetworkData).SendToServerReliable();
            }
        }

        internal void UpdateItem() {
            bool owner = IsSending();

            if(item != null) {
                bool active = itemNetworkData.lastPositionTimestamp >= NetworkData.GetDataTimestamp() - (Config.NET_COMP_DISABLE_DELAY * 1000);

                item.disallowDespawn = !owner || item.data.type == ItemData.Type.Prop;
                item.physicBody.useGravity = owner || (!owner && !active);
                //item.physicBody.isKinematic = (owner ? isKinematicItem : true); // TODO: Fix, causing snapped items to malfunction

                // Check if the item is active and set the tick rate accordingly
                if(active && !owner) {
                    // Item is active and receiving data, we want it to update every frame
                    NetworkComponentManager.SetTickRate(this, 1, ManagedLoops.Update);
                } else {
                    // Item is inactive and not receiving any new data, just update it from time to time
                    NetworkComponentManager.SetTickRate(this, Random.Range(150, 250), ManagedLoops.Update);
                }
            } else {
                // Item is sending data, just update it from time to time, probably not nessesary at all, but for good measure. The data sending step is done in a seperated thread
                NetworkComponentManager.SetTickRate(this, Random.Range(150, 250), ManagedLoops.Update);
            }
        }

        internal void UpdateIfNeeded() {
            if(NetworkComponentManager.GetTickRate(this, ManagedLoops.Update) != 1) {
                UpdateItem();
            }
        }
    }
}
