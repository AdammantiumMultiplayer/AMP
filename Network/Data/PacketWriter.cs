using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data {
    public class PacketWriter {
        
        public static Packet Error(string message) {
            Packet packet = new Packet(Packet.Type.error);
            packet.Write(message);
            return packet;
        }

        public static Packet Welcome(int clientId) {
            Packet packet = new Packet(Packet.Type.welcome);
            packet.Write(clientId);
            return packet;
        }

        public static Packet Message(string message) {
            Packet packet = new Packet(Packet.Type.message);
            packet.Write(message);
            return packet;
        }

        public static Packet LoadLevel(string levelName) {
            Packet packet = new Packet(Packet.Type.loadLevel);
            packet.Write(levelName);
            return packet;
        }

        public static Packet SetItemOwnership(int networkId, bool owner) {
            Packet packet = new Packet(Packet.Type.itemOwn);
            packet.Write(networkId);
            packet.Write(owner);
            return packet;
        }

        internal static Packet Disconnect(int playerId, string message) {
            Packet packet = new Packet(Packet.Type.disconnect);
            packet.Write(playerId);
            packet.Write(message);
            return packet;
        }
    }
}
