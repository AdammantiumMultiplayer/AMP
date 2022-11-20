using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.SERVER_JOIN)]
    public class EstablishConnectionPacket : NetPacket {
        [SyncedVar] public string name;
        [SyncedVar] public string version;
        [SyncedVar] public string password = "none";

        public EstablishConnectionPacket() { }

        public EstablishConnectionPacket(string name, string version) {
            this.name = name;
            this.version = version;
        }

        public EstablishConnectionPacket(string name, string version, string password) 
            : this(name:    name
                  ,version: version
                  ) {
            this.password = password;
        }
    }
}
