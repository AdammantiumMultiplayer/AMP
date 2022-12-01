using AMP.Network.Packets.Attributes;
using System;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PING)]
    public class PingPacket : NetPacket {
        [SyncedVar] public long timestamp = 0;

        public PingPacket() {
            if(timestamp <= 0) {
                timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        public PingPacket(long timestamp) {
            this.timestamp = timestamp;
        }
    }
}
