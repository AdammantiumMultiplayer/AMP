using AMP.Logging;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class ItemSync {
        public int networkedId = 0;
        public string dataId;

        // Clientside Item Id, if 0 we dont own that item
        // Gets asigned when an item is first spawned
        public int clientsideId = 0;
        public Item clientsideItem;
        public bool registeredEvents = false;

        public Vector3 position;
        public Vector3 rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        public Holder.DrawSlot drawSlot;
        public Side holdingSide;
        public bool holderIsPlayer = false;
        public int creatureNetworkId;

        public Packet CreateSpawnPacket() {
            Packet packet = new Packet(Packet.Type.itemSpawn);

            packet.Write(networkedId);
            packet.Write(dataId);
            packet.Write(clientsideId);
            packet.Write(position);
            packet.Write(rotation);

            return packet;
        }

        public void ApplySpawnPacket(Packet packet) {
            networkedId  = packet.ReadInt();
            dataId       = packet.ReadString();
            clientsideId = packet.ReadInt();
            position     = packet.ReadVector3();
            rotation     = packet.ReadVector3();
        }

        public Packet CreatePosPacket() {
            Packet packet = new Packet(Packet.Type.itemPos);

            packet.Write(networkedId);
            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);
            packet.Write(angularVelocity);

            return packet;
        }

        public void ApplyPosPacket(Packet packet) {
            position        = packet.ReadVector3();
            rotation        = packet.ReadVector3();
            velocity        = packet.ReadVector3();
            angularVelocity = packet.ReadVector3();
        }


        public Packet DespawnPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemDespawn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }

        public void ApplyPositionToItem() {
            if(clientsideItem == null) return;

            clientsideItem.transform.position = position;
            clientsideItem.transform.eulerAngles = rotation;
            clientsideItem.rb.velocity = velocity;
            clientsideItem.rb.angularVelocity = angularVelocity;
        }

        public void UpdatePositionFromItem() {
            if(clientsideItem == null) return;

            position = clientsideItem.transform.position;
            rotation = clientsideItem.transform.eulerAngles;
            velocity = clientsideItem.rb.velocity;
            angularVelocity = clientsideItem.rb.angularVelocity;
        }


        public Packet TakeOwnershipPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemOwn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }

        public void SetOwnership(bool owner) {
            if(owner) {
                if(clientsideId <= 0) clientsideId = ModManager.clientSync.syncData.currentClientItemId++;
            } else {
                clientsideId = 0;
            }
            if(clientsideItem != null)
                clientsideItem.disallowDespawn = owner;
        }

        public void UpdateFromHolder() {
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

        public void UpdateHoldState() {
            if(clientsideItem == null) return;

            if(creatureNetworkId <= 0) {
                if(clientsideItem.holder != null)
                    clientsideItem.holder.UnSnap(clientsideItem);
                if(clientsideItem.mainHandler != null && clientsideItem.mainHandler.isGrabbed)
                    clientsideItem.mainHandler.UnGrab(false);
            } else {
                Creature creature = null;
                string name = "";
                if(holderIsPlayer) {
                    if(ModManager.clientSync.syncData.players.ContainsKey(creatureNetworkId)) {
                        PlayerSync ps = ModManager.clientSync.syncData.players[creatureNetworkId];
                        creature = ps.creature;
                        name = "player " + ps.name;
                    }
                } else {
                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureNetworkId)) {
                        CreatureSync cs = ModManager.clientSync.syncData.creatures[creatureNetworkId];
                        creature = cs.clientsideCreature;
                        name = "creature " + cs.creatureId;
                    }
                }

                if(creature == null) return;

                if(drawSlot == Holder.DrawSlot.None) {
                    if(clientsideItem.mainHandler != null) clientsideItem.mainHandler.UnGrab(false);
                    creature.GetHand(holdingSide).Grab(clientsideItem.GetMainHandle(holdingSide));

                    Log.Debug($"[Client] Grabbed item {dataId} by {name} with hand {holdingSide}.");
                } else {
                    if(clientsideItem.mainHandler != null) clientsideItem.mainHandler.UnGrab(false);
                    creature.equipment.GetHolder(drawSlot).Snap(clientsideItem);

                    Log.Debug($"[Client] Snapped item {dataId} to player {name} with slot {drawSlot}.");
                }
            }
        }

        public Packet SnapItemPacket() {
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


        public Packet UnSnapItemPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemUnSnap);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }
    }
}
