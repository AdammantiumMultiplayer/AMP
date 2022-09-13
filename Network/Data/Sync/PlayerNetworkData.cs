using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.SupportFunctions;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    internal class PlayerNetworkData {
        #region Values
        internal long clientId = 0;
        internal string name = "";

        internal string creatureId = "HumanMale";
        internal float height = 1.8f;

        internal Vector3 handLeftPos = Vector3.zero;
        internal Vector3 handLeftRot = Vector3.zero;

        internal Vector3 handRightPos = Vector3.zero;
        internal Vector3 handRightRot = Vector3.zero;

        internal Vector3 headPos = Vector3.zero;
        internal Vector3 headRot = Vector3.zero;

        internal Vector3 playerVel = Vector3.zero;
        internal Vector3 playerPos = Vector3.zero;
        internal float playerRot   = 0f;


        internal float health = 1f;

        internal List<string> equipment = new List<string>();
        internal Color[] colors = new Color[6];

        // Client only stuff
        internal bool isSpawning = false;
        internal Creature creature;
        private NetworkPlayerCreature _networkCreature;
        internal NetworkPlayerCreature networkCreature {
            get {
                if(_networkCreature == null) _networkCreature = creature.GetComponent<NetworkPlayerCreature>();
                return _networkCreature;
            }
        }

        internal TextMesh healthBar;
        #endregion

        #region Packet Generation and Reading
        internal Packet CreateConfigPacket() {
            Packet packet = new Packet(Packet.Type.playerData);
            packet.Write(clientId);
            packet.Write(name);

            packet.Write(creatureId);
            packet.Write(height);

            packet.Write(playerPos);
            packet.Write(playerRot);

            return packet;
        }

        internal void ApplyConfigPacket(Packet packet) {
            clientId   = packet.ReadLong();
            name       = packet.ReadString();

            creatureId = packet.ReadString();
            height     = packet.ReadFloat();

            playerPos  = packet.ReadVector3();
            playerRot  = packet.ReadFloat();
        }

        internal Packet CreateEquipmentPacket() {
            Packet packet = new Packet(Packet.Type.playerEquip);
            packet.Write(clientId);

            packet.Write(colors.Length);
            for(int i = 0; i < colors.Length; i++)
                packet.Write(colors[i]);

            packet.Write(equipment.Count);
            foreach(string line in equipment)
                packet.Write(line);

            return packet;
        }

        internal void ApplyEquipmentPacket(Packet p) {
            colors = new Color[p.ReadInt()];
            for(int i = 0; i < colors.Length; i++)
                colors[i] = p.ReadColor();

            int count = p.ReadInt();
            equipment.Clear();
            for(int i = 0; i < count; i++) {
                equipment.Add(p.ReadString());
            }
        }

        internal Packet CreatePosPacket() {
            Packet packet = new Packet(Packet.Type.playerPos);

            packet.Write(clientId);

            packet.Write(handLeftPos);
            packet.Write(handLeftRot);

            packet.Write(handRightPos);
            packet.Write(handRightRot);

            packet.Write(headPos);
            packet.Write(headRot);

            packet.Write(playerPos);
            packet.Write(playerRot);
            packet.Write(playerVel);

            packet.Write(health);

            return packet;
        }

        internal void ApplyPosPacket(Packet packet) {
            clientId = packet.ReadLong();

            handLeftPos = packet.ReadVector3();
            handLeftRot = packet.ReadVector3();

            handRightPos = packet.ReadVector3();
            handRightRot = packet.ReadVector3();

            headPos = packet.ReadVector3();
            headRot = packet.ReadVector3();

            playerPos = packet.ReadVector3();
            playerRot = packet.ReadFloat();
            playerVel = packet.ReadVector3();

            health = packet.ReadFloat();
        }

        internal void ApplyPos(PlayerNetworkData other) {
            playerPos    = other.playerPos;
            playerRot    = other.playerRot;
            handLeftPos  = other.handLeftPos;
            handLeftRot  = other.handLeftRot;
            handRightPos = other.handRightPos;
            handRightRot = other.handRightRot;
            headPos      = other.headPos;
            headRot      = other.headRot;
            playerVel    = other.playerVel;

            if(health != other.health && healthBar != null) {
                healthBar.text = HealthBar.calculateHealthBar(other.health);
            }
            health = other.health;
        }

        internal Packet CreateHealthChangePacket(float change) {
            Packet packet = new Packet(Packet.Type.playerHealthChange);

            packet.Write(clientId);
            packet.Write(change);

            return packet;
        }
        #endregion
    }
}
