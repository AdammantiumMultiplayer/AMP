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
                try {
                    KeyValuePair<int, CreatureSync> entry = ModManager.clientSync.syncData.creatures.First(value => clientsideItem.holder.creature.Equals(value.Value.clientsideCreature));
                    if(entry.Value.networkedId > 0) {
                        drawSlot = clientsideItem.holder.drawSlot;
                        creatureNetworkId = entry.Value.networkedId;
                    }
                }catch(InvalidOperationException) { }
            }
        }

        public Packet SnapItemPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.itemSnap);
                packet.Write(networkedId);
                packet.Write(creatureNetworkId);
                packet.Write((int) drawSlot);
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
