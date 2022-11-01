using System;

namespace AMP.Network.Packets {
    [AttributeUsage(AttributeTargets.Class)]
    internal class PacketDefinition : Attribute {
        public byte packetType = 0;

        public PacketDefinition(byte packetType) {
            this.packetType = packetType;
        }
    }
}
