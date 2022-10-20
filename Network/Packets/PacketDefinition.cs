using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Network.Packets {
    [AttributeUsage(AttributeTargets.Class)]
    internal class PacketDefinition : Attribute {
        public byte packetType = 0;

        public PacketDefinition(byte packetType) {
            this.packetType = packetType;
        }
    }
}
