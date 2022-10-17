using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Helper;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    internal class ItemNetworkData {
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
        internal Side holdingSide;
        internal bool holderIsPlayer = false;
        internal long creatureNetworkId;
        #endregion

        #region Packet Generation and Reading
        internal Packet CreateSpawnPacket() {
            Packet packet = new Packet(Packet.Type.itemSpawn);

            packet.Write(networkedId);
            packet.Write(dataId);
            packet.Write((byte) category);
            packet.Write(clientsideId);
            packet.Write(position);
            packet.WriteLP(rotation);

            return packet;
        }

        internal void ApplySpawnPacket(Packet packet) {
            networkedId  = packet.ReadLong();
            dataId       = packet.ReadString();
            category     = (ItemData.Type) packet.ReadByte();
            clientsideId = packet.ReadLong();
            position     = packet.ReadVector3();
            rotation     = packet.ReadVector3LP();

            PositionChanged();
        }

        internal Packet CreatePosPacket() {
            Packet packet = new Packet(Packet.Type.itemPos);

            packet.Write(networkedId);
            packet.Write(position);
            packet.WriteLP(rotation);
            packet.WriteLP(velocity);
            packet.WriteLP(angularVelocity);

            return packet;
        }

        internal void ApplyPosPacket(Packet packet) {
            position        = packet.ReadVector3();
            rotation        = packet.ReadVector3LP();
            velocity        = packet.ReadVector3LP();
            angularVelocity = packet.ReadVector3LP();

            PositionChanged();
        }

        internal void PositionChanged() {
            if(clientsideItem != null) clientsideItem.lastInteractionTime = Time.time;
        }

        internal Packet DespawnPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemDespawn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
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


        internal Packet TakeOwnershipPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemOwn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }

        internal void SetOwnership(bool owner) {
            if(owner) {
                if(clientsideId <= 0) clientsideId = ModManager.clientSync.syncData.currentClientItemId++;
            } else {
                clientsideId = 0;
            }
            if(clientsideItem != null) {
                clientsideItem.disallowDespawn = owner;
            }
        }

        internal void UpdateFromHolder() {
            if(clientsideItem == null) return;

            if(clientsideItem.holder != null && clientsideItem.holder.creature != null) {
                if(SyncFunc.GetCreature(clientsideItem.holder.creature, out holderIsPlayer, out creatureNetworkId)) {
                    drawSlot = clientsideItem.holder.drawSlot;
                    return;
                }
            } else {
                if(clientsideItem.mainHandler != null && clientsideItem.mainHandler.creature != null) {
                    if(SyncFunc.GetCreature(clientsideItem.mainHandler.creature, out holderIsPlayer, out creatureNetworkId)) {
                        drawSlot = Holder.DrawSlot.None;
                        
                        holdingSide = clientsideItem.mainHandler.side;
                        return;
                    }
                }
            }
            drawSlot = Holder.DrawSlot.None;
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
                        creature = cs.clientsideCreature;
                        name = "creature " + cs.creatureId;
                    }
                }

                if(creature == null) return;

                if(drawSlot == Holder.DrawSlot.None) {
                    //if(clientsideItem.mainHandler != null) clientsideItem.mainHandler.UnGrab(false); // Probably dont need to ungrab, so its possible to hold a sword with 2 hands
                    if(clientsideItem.GetMainHandle(holdingSide).handlers.Contains(creature.GetHand(holdingSide))) return;
                    creature.GetHand(holdingSide).Grab(clientsideItem.GetMainHandle(holdingSide));

                    Log.Debug($"[Client] Grabbed item {dataId} by {name} with hand {holdingSide}.");
                } else {
                    creature.equipment.GetHolder(drawSlot).Snap(clientsideItem);

                    Log.Debug($"[Client] Snapped item {dataId} to {name} with slot {drawSlot}.");
                }
            }
        }

        internal Packet SnapItemPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemSnap);
                packet.Write(networkedId);
                packet.Write(creatureNetworkId);
                packet.Write((byte) drawSlot);
                packet.Write((byte) holdingSide);
                packet.Write(holderIsPlayer);
                return packet;
            }
            return null;
        }


        internal Packet UnSnapItemPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemUnSnap);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }
        #endregion

        #region Imbues
        internal Packet CreateImbuePacket(string type, int index, float amount) {
            Packet packet = new Packet(Packet.Type.itemImbue);

            packet.Write(networkedId);
            packet.Write(type);
            packet.Write(index);
            packet.WriteLP(amount);

            return packet;
        }
        internal void ApplyImbuePacket(Packet p) {
            if(clientsideItem == null) return;

            string type = p.ReadString();
            int index = p.ReadInt();
            float amount = p.ReadFloatLP();

            if(clientsideItem.imbues.Count > index) {
                SpellCastCharge spellCastBase = Catalog.GetData<SpellCastCharge>(type, true);

                if(spellCastBase == null) {// If the client doesnt have the spell, just ignore it
                    Log.Err($"[Client] Couldn't find spell {type}, please check you mods.");
                    return;
                }

                //spellCastBase = spellCastBase.Clone();

                Imbue imbue = clientsideItem.imbues[index];
                
                float energy = amount - imbue.energy;
                if(imbue.spellCastBase == null) energy = amount;
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
