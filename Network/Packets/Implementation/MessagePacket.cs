using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MESSAGE)]
    public class MessagePacket : NetPacket {
        [SyncedVar] public string message = "";

        public MessagePacket() { }

        public MessagePacket(string message) {
            this.message = message;
        }
    }
}
