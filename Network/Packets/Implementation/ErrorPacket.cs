using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ERROR)]
    public class ErrorPacket : NetPacket {
        [SyncedVar] public string message;

        public ErrorPacket(string error_message) {
            this.message = error_message;
        }
    }
}
