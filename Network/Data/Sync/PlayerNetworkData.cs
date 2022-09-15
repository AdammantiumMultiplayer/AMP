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

            packet.Write((byte) colors.Length);
            for(byte i = 0; i < colors.Length; i++)
                packet.Write(colors[i]);

            packet.Write((byte) equipment.Count);
            foreach(string line in equipment)
                packet.Write(line);

            return packet;
        }

        internal void ApplyEquipmentPacket(Packet p) {
            colors = new Color[p.ReadByte()];
            for(int i = 0; i < colors.Length; i++)
                colors[i] = p.ReadColor();

            byte count = p.ReadByte();
            equipment.Clear();
            for(byte i = 0; i < count; i++) {
                equipment.Add(p.ReadString());
            }
        }

        internal Packet CreatePosPacket() {
            Packet packet = new Packet(Packet.Type.playerPos);

            packet.Write(clientId);

            packet.WriteLP(handLeftPos);
            packet.WriteLP(handLeftRot);

            packet.WriteLP(handRightPos);
            packet.WriteLP(handRightRot);

            packet.WriteLP(headPos);
            packet.WriteLP(headRot);

            packet.Write(playerPos);
            packet.WriteLP(playerRot);
            packet.WriteLP(playerVel);

            packet.Write(health);

            return packet;
        }

        internal void ApplyPosPacket(Packet packet) {
            clientId = packet.ReadLong();

            handLeftPos = packet.ReadVector3LP();
            handLeftRot = packet.ReadVector3LP();

            handRightPos = packet.ReadVector3LP();
            handRightRot = packet.ReadVector3LP();

            headPos = packet.ReadVector3LP();
            headRot = packet.ReadVector3LP();

            playerPos = packet.ReadVector3();
            playerRot = packet.ReadFloatLP();
            playerVel = packet.ReadVector3LP();

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
