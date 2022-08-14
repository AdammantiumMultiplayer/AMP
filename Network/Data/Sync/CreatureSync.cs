using AMP.Network.Client;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class CreatureSync {

        public long networkedId = 0;

        public string creatureId;
        public string containerID;
        public int factionId;

        public Vector3 position;
        public Vector3 rotation;
        public Vector3 velocity;

        public long clientsideId = 0;
        public Creature clientsideCreature;
        public bool registeredEvents = false;
        private NetworkCreature _networkCreature;
        public NetworkCreature networkCreature {
            get {
                if(_networkCreature == null) _networkCreature = clientsideCreature.GetComponent<NetworkCreature>();
                return _networkCreature;
            }
        }

        public long clientTarget = 0;

        public float health = 100;

        public List<string> equipment = new List<string>();

        public Packet CreateSpawnPacket() {
            Packet packet = new Packet(Packet.Type.creatureSpawn);

            packet.Write(networkedId);
            packet.Write(clientsideId);
            packet.Write(creatureId);
            packet.Write(containerID);
            packet.Write(factionId);
            packet.Write(position);
            packet.Write(rotation);

            packet.Write(equipment.Count);
            foreach(string line in equipment)
                packet.Write(line);

            return packet;
        }

        public void ApplySpawnPacket(Packet p) {
            networkedId  = p.ReadLong();
            clientsideId = p.ReadLong();
            creatureId   = p.ReadString();
            containerID  = p.ReadString();
            factionId    = p.ReadInt();
            position     = p.ReadVector3();
            rotation     = p.ReadVector3();

            int count = p.ReadInt();
            equipment.Clear();
            for(int i = 0; i < count; i++) {
                equipment.Add(p.ReadString());
            }
        }

        public Packet CreatePosPacket() {
            Packet packet = new Packet(Packet.Type.creaturePos);

            packet.Write(networkedId);
            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);

            return packet;
        }

        public void ApplyPosPacket(Packet packet) {
            position = packet.ReadVector3();
            rotation = packet.ReadVector3();
            velocity = packet.ReadVector3();
        }

        public void ApplyPositionToCreature() {
            if(clientsideCreature == null) return;
            if(clientsideCreature.isKilled) return;

            clientsideCreature.transform.eulerAngles = rotation;
            //clientsideCreature.transform.position = position;

            networkCreature.targetPos = position;
            networkCreature.velocity = velocity;
            //clientsideCreature.locomotion.rb.velocity = velocity;
            //clientsideCreature.locomotion.velocity = velocity;
        }

        public Packet CreateHealthPacket() {
            Packet packet = new Packet(Packet.Type.creatureHealth);

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

                //Log.Debug($"Creature {clientsideCreature.creatureId} is now at health {health}.");

                if(clientsideCreature.currentHealth <= 0) {
                    clientsideCreature.Kill();
                }
            }
        }

        public Packet CreateDespawnPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.creatureDespawn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }

        internal void UpdatePositionFromCreature() {
            if(clientsideCreature == null) return;

            position = clientsideCreature.transform.position;
            rotation = clientsideCreature.transform.eulerAngles;
            velocity = clientsideCreature.locomotion.velocity;
        }
    }
}
