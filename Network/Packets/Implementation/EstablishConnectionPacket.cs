using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.SERVER_JOIN)]
    internal class EstablishConnectionPacket : NetPacket {
        [SyncedVar] public string name;
        [SyncedVar] public string version;

        public EstablishConnectionPacket() { }

        public EstablishConnectionPacket(string name, string version) {
            this.name = name;
            this.version = version;
        }
    }
}
