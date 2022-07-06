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

        public int clientsideId = 0;
        public Item clientsideItem;

        public Vector3 position;
        public Vector3 rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        public Packet CreateSpawnPacket() {
            Packet packet = new Packet((int) Packet.Type.itemSpawn);

            packet.Write(networkedId);
            packet.Write(dataId);
            packet.Write(clientsideId);
            packet.Write(position);
            packet.Write(rotation);

            return packet;
        }

        public void RestoreSpawnPacket(Packet packet) {
            networkedId  = packet.ReadInt();
            dataId       = packet.ReadString();
            clientsideId = packet.ReadInt();
            position     = packet.ReadVector3();
            rotation     = packet.ReadVector3();
        }

        public Packet CreateSyncPacket() {
            Packet packet = new Packet((int) Packet.Type.itemPos);

            packet.Write(networkedId);
            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);
            packet.Write(angularVelocity);

            return packet;
        }

        public void RestoreSyncPacket(Packet packet) {
            networkedId     = packet.ReadInt();
            position        = packet.ReadVector3();
            rotation        = packet.ReadVector3();
            velocity        = packet.ReadVector3();
            angularVelocity = packet.ReadVector3();
        }

        public Packet DespawnPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet((int) Packet.Type.itemDespawn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }
    }
}
