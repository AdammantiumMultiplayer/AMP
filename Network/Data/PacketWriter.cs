using System.Collections.Generic;

namespace AMP.Network.Data {
    public class PacketWriter {
        
        public static Packet Error(string message) {
            Packet packet = new Packet(Packet.Type.error);
            packet.Write(message);
            return packet;
        }

        public static Packet Welcome(long clientId) {
            Packet packet = new Packet(Packet.Type.welcome);
            packet.Write(clientId);
            return packet;
        }

        public static Packet Message(string message) {
            Packet packet = new Packet(Packet.Type.message);
            packet.Write(message);
            return packet;
        }

        public static Packet LoadLevel(string levelName, string mode, Dictionary<string, string> options) {
            Packet packet = new Packet(Packet.Type.loadLevel);
            packet.Write(levelName);
            packet.Write(mode);

            if(options == null) {
                packet.Write(0);
            } else {
                packet.Write(options.Count);
                foreach(KeyValuePair<string, string> entry in options) {
                    packet.Write(entry.Key);
                    packet.Write(entry.Value);
                }
            }

            return packet;
        }

        public static Packet SetItemOwnership(long networkId, bool owner) {
            Packet packet = new Packet(Packet.Type.itemOwn);
            packet.Write(networkId);
            packet.Write(owner);
            return packet;
        }

        public static Packet Disconnect(long playerId, string message) {
            Packet packet = new Packet(Packet.Type.disconnect);
            packet.Write(playerId);
            packet.Write(message);
            return packet;
        }

        public static Packet CreatureAnimation(long creatureId, int stateHash, string clipName) {
            Packet packet = new Packet(Packet.Type.creatureAnimation);
            packet.Write(creatureId);
            packet.Write(stateHash);
            packet.Write(clipName);
            return packet;
        }
    }
}
