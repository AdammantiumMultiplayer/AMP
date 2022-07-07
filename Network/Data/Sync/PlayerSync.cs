using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class PlayerSync {
        public int clientId = 0;
        public string name = "";

        public string creatureId = "HumanMale";
        public float height = 1.8f;

        public Vector3 handLeftPos = Vector3.zero;
        public Vector3 handLeftRot = Vector3.zero;

        public Vector3 handRightPos = Vector3.zero;
        public Vector3 handRightRot = Vector3.zero;

        public Vector3 headRot = Vector3.zero;

        public Vector3 playerPos = Vector3.zero;
        public float playerRot   = 0f;

        // Client only stuff
        public bool isSpawning = false;
        public Creature creature;
        public Transform leftHandTarget;
        public Transform rightHandTarget;
        public Transform headTarget;

        public Packet CreateConfigPacket() {
            Packet packet = new Packet((int) Packet.Type.playerData);
            packet.Write(clientId);
            packet.Write(name);

            packet.Write(creatureId);
            packet.Write(height);

            packet.Write(playerPos);
            packet.Write(playerRot);

            return packet;
        }

        public void ApplyConfigPacket(Packet packet) {
            clientId   = packet.ReadInt();
            name       = packet.ReadString();

            creatureId = packet.ReadString();
            height     = packet.ReadFloat();

            playerPos  = packet.ReadVector3();
            playerRot  = packet.ReadFloat();
        }



        public Packet CreatePosPacket() {
            Packet packet = new Packet((int) Packet.Type.playerPos);

            packet.Write(clientId);

            packet.Write(handLeftPos);
            packet.Write(handLeftRot);

            packet.Write(handRightPos);
            packet.Write(handRightRot);

            packet.Write(headRot);

            packet.Write(playerPos);
            packet.Write(playerRot);

            return packet;
        }

        public void ApplyPosPacket(Packet packet) {
            clientId = packet.ReadInt();

            handLeftPos = packet.ReadVector3();
            handLeftRot = packet.ReadVector3();

            handRightPos = packet.ReadVector3();
            handRightRot = packet.ReadVector3();

            headRot = packet.ReadVector3();

            playerPos = packet.ReadVector3();
            playerRot = packet.ReadFloat();
        }

        internal void ApplyPos(PlayerSync other) {
            playerPos    = other.playerPos;
            playerRot    = other.playerRot;
            handLeftPos  = other.handLeftPos;
            handLeftRot  = other.handLeftRot;
            handRightPos = other.handRightPos;
            handRightRot = other.handRightRot;
            headRot      = other.headRot;
        }
    }
}
