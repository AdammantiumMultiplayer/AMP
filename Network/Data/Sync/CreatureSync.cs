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

        public Packet CreatePosPacket() {
            Packet packet = new Packet((int) Packet.Type.creaturePos);

            packet.Write(networkedId);
            packet.Write(position);
            packet.Write(rotation);

            return packet;
        }

        public Packet CreateHealthPacket() {
            Packet packet = new Packet((int) Packet.Type.creatureHealth);

            packet.Write(networkedId);
            packet.Write(health);

            return packet;
        }

    }
}
