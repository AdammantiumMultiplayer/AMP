using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.SERVER_JOIN)]
    internal class EstablishConnectionPacket : NetPacket {
        [SyncedVar] public string name;

        public EstablishConnectionPacket() { }

        public EstablishConnectionPacket(string name) {
            this.name = name;
        }
    }
}
