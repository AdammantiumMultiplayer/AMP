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
            Packet packet = new Packet((int) Packet.Type.error);
            packet.Write(message);
            return packet;
        }

        public static Packet Welcome(int clientId) {
            Packet packet = new Packet((int) Packet.Type.welcome);
            packet.Write(clientId);
            return packet;
        }

        public static Packet Message(string message) {
            Packet packet = new Packet((int)Packet.Type.message);
            packet.Write(message);
            return packet;
        }

        public static Packet PlayerData(int id, string creatureId, float height) {
            Packet packet = new Packet((int)Packet.Type.playerData);
            packet.Write(ThunderRoad.Player.currentCreature.creatureId);
            packet.Write(ThunderRoad.Player.currentCreature.GetHeight());
            return packet;
        }

    }
}
