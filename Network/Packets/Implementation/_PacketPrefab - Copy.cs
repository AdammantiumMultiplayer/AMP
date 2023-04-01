using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.UNKNOWN)]
    public class PacketPrefab : NetPacket {
        [SyncedVar]       public float fp;
        [SyncedVar(true)] public float lp;

        public PacketPrefab() { }
    }
}
