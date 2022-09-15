using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    internal class CreatureNetworkData {
        #region Values
        internal long networkedId = 0;

        internal string creatureId;
        internal string containerID;
        internal int factionId;

        internal Vector3 position;
        internal Vector3 rotation;
        //public Vector3 velocity;

        internal Vector3[] ragdollParts;

        internal bool loaded = false;

        internal bool isSpawning = false;
        internal long clientsideId = 0;
        internal Creature clientsideCreature;
        private NetworkCreature _networkCreature;
        internal NetworkCreature networkCreature {
            get {
                if(_networkCreature == null) _networkCreature = clientsideCreature.GetComponent<NetworkCreature>();
                return _networkCreature;
            }
        }

        internal long clientTarget = 0;

        internal float maxHealth = 100;
        internal float health = 100;

        internal float height = 2f;

        internal List<string> equipment = new List<string>();

        internal float lastUpdate = 0;
        #endregion

        #region Packet Generation and Reading
        internal Packet CreateSpawnPacket() {
            Packet packet = new Packet(Packet.Type.creatureSpawn);

            packet.Write(networkedId);
            packet.Write(clientsideId);
            packet.Write(creatureId);
            packet.Write(containerID);
            packet.Write(factionId);
            packet.Write(position);
            packet.WriteLP(rotation);
            packet.WriteLP(health);
            packet.WriteLP(maxHealth);
            packet.WriteLP(height);

            packet.Write((byte) equipment.Count);
            foreach(string line in equipment)
                packet.Write(line);

            return packet;
        }

        internal void ApplySpawnPacket(Packet p) {
            networkedId  = p.ReadLong();
            clientsideId = p.ReadLong();
            creatureId   = p.ReadString();
            containerID  = p.ReadString();
            factionId    = p.ReadInt();
            position     = p.ReadVector3();
            rotation     = p.ReadVector3LP();
            health       = p.ReadFloatLP();
            maxHealth    = p.ReadFloatLP();
            height       = p.ReadFloatLP();

            byte count = p.ReadByte();
            equipment.Clear();
            for(byte i = 0; i < count; i++) {
                equipment.Add(p.ReadString());
            }
        }

        internal Packet CreatePosPacket() {
            Packet packet = new Packet(Packet.Type.creaturePos);

            packet.Write(networkedId);
            packet.Write(position);
            packet.WriteLP(rotation);
            //packet.Write(velocity);

            return packet;
        }

        internal void ApplyPosPacket(Packet packet) {
            if(isSpawning) return;
            position = packet.ReadVector3();
            rotation = packet.ReadVector3LP();
            //velocity = packet.ReadVector3();
        }

        internal void ApplyPositionToCreature() {
            if(clientsideCreature == null) return;
            if(clientsideCreature.isKilled) return;

            clientsideCreature.transform.eulerAngles = rotation;
            //clientsideCreature.transform.position = position;

            networkCreature.targetPos = position;
            //networkCreature.velocity = velocity;
            //clientsideCreature.locomotion.rb.velocity = velocity;
            //clientsideCreature.locomotion.velocity = velocity;

            PositionChanged();
        }


        internal Packet CreateRagdollPacket() {
            Packet packet = new Packet(Packet.Type.creatureRagdoll);

            packet.Write(networkedId);

            packet.Write((byte) ragdollParts.Length);
            for(byte i = 0; i < ragdollParts.Length; i++) {
                if(i == 0) {
                    packet.Write(ragdollParts[i]);
                } else {
                    Vector3 offset = ragdollParts[i];
                    if(i % 2 == 0) offset -= ragdollParts[0]; // Remove offset only to positions, they are at the even indexes
                    packet.WriteLP(offset);
                }
            }

            return packet;
        }

        internal void ApplyRagdollPacket(Packet p) {
            byte count = p.ReadByte();
            if(count == 0) {
                ragdollParts = null;
            } else {
                ragdollParts = new Vector3[count];
                for(byte i = 0; i < count; i++) {
                    if(i == 0) {
                        ragdollParts[i] = p.ReadVector3();
                    } else {
                        ragdollParts[i] = p.ReadVector3LP();
                        if(i % 2 == 0) ragdollParts[i] += ragdollParts[0]; // Add offset only to positions, they are at the even indexes
                    }
                }
            }

            PositionChanged();
        }

        internal Packet CreateHealthPacket() {
            Packet packet = new Packet(Packet.Type.creatureHealth);

            packet.Write(networkedId);
            packet.WriteLP(health);

            return packet;
        }


        internal Packet CreateHealthChangePacket(float change) {
            Packet packet = new Packet(Packet.Type.creatureHealthChange);

            packet.Write(networkedId);
            packet.WriteLP(change);

            return packet;
        }

        internal void ApplyHealthPacket(Packet packet) {
            health = packet.ReadFloatLP();
        }

        internal void ApplyHealthChange(float change) {
            health += change;
        }

        internal void ApplyHealthToCreature() {
            if(clientsideCreature != null) {
                clientsideCreature.currentHealth = health;

                //Log.Debug($"Creature {clientsideCreature.creatureId} is now at health {health}.");

                if(health <= 0) {
                    clientsideCreature.Kill();
                }
            }
        }

        internal Packet CreateDespawnPacket() {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.creatureDespawn);
                packet.Write(networkedId);
                return packet;
            }
            return null;
        }

        internal Packet CreateSlicePacket(RagdollPart.Type part) {
            if(networkedId > 0) {
                Packet packet = new Packet(Packet.Type.creatureSlice);
                packet.Write(networkedId);
                packet.Write((int) part);
                return packet;
            }
            return null;
        }

        internal void UpdatePositionFromCreature() {
            if(clientsideCreature == null) return;

            if(clientsideCreature.IsRagdolled()) {
                ragdollParts = clientsideCreature.ReadRagdoll();
            } else {
                ragdollParts = null;
                position = clientsideCreature.transform.position;
                rotation = clientsideCreature.transform.eulerAngles;
                //velocity = clientsideCreature.locomotion.velocity;
            }
        }

        internal void PositionChanged() {
            lastUpdate = Time.time;
        }
        #endregion
    }
}
