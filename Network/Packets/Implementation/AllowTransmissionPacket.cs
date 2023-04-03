using AMP.Data;
using AMP.Logging;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.ALLOW_TRANSMISSION)]
    public class AllowTransmissionPacket : NetPacket {
        [SyncedVar] public bool allow;

        public AllowTransmissionPacket() { }

        public AllowTransmissionPacket(bool allow) {
            this.allow = allow;
        }

        public override bool ProcessClient(NetamiteClient client) {
            ModManager.clientInstance.allowTransmission = allow;
            Log.Debug(Defines.CLIENT, $"Transmission is now {(allow ? "en" : "dis")}abled");
            return true;
        }
    }
}
