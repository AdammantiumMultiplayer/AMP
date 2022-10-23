using AMP.Network.Packets.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
