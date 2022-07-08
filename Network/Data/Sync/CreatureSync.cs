using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class CreatureSync {

        public int networkedId = 0;

        public string creatureId;
        public string containerID;
        public int factionId;

        public Vector3 position;
        public Vector3 rotation;

        public int clientsideId = 0;
        public Creature clientsideCreature;

        public int clientTarget = 0;

        public float health = 100;


        public Packet CreateSpawnPacket() {
            Packet packet = new Packet((int) Packet.Type.creatureSpawn);

            packet.Write(networkedId);
            packet.Write(clientsideId);
            packet.Write(creatureId);
            packet.Write(containerID);
            packet.Write(factionId);
            packet.Write(position);
            packet.Write(rotation);

            return packet;
        }

        public void ApplySpawnPacket(Packet packet) {
            networkedId  = packet.ReadInt();
            clientsideId = packet.ReadInt();
            creatureId   = packet.ReadString();
            containerID  = packet.ReadString();
            factionId    = packet.ReadInt();
            position     = packet.ReadVector3();
            rotation     = packet.ReadVector3();
        }

        public Packet CreatePosPacket() {
            Packet packet = new Packet((int) Packet.Type.creaturePos);

            packet.Write(networkedId);
            packet.Write(position);
            packet.Write(rotation);

            return packet;
        }

        public void ApplyPosPacket(Packet packet) {
            position = packet.ReadVector3();
            rotation = packet.ReadVector3();
        }

        public void ApplyPositionToCreature() {
            if(clientsideCreature == null) return;

            clientsideCreature.Teleport(position, Quaternion.Euler(rotation));
        }

        public Packet CreateHealthPacket() {
            Packet packet = new Packet((int) Packet.Type.creatureHealth);

            packet.Write(networkedId);
            packet.Write(health);

            return packet;
        }

        public void ApplyHealthPacket(Packet packet) {
            health = packet.ReadFloat();
        }

        public void ApplyHealthToCreature() {
            if(clientsideCreature != null) {
                clientsideCreature.currentHealth = health;
            }
        }

        public Packet CreateDespawnPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet((int) Packet.Type.creatureDespawn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }

        internal void UpdatePositionFromCreature() {
            if(clientsideCreature == null) return;

            position = clientsideCreature.transform.position;
            rotation = clientsideCreature.transform.eulerAngles;
        }
    }
}
