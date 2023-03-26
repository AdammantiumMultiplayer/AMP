using AMP.Network.Packets.Attributes;
using System;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    

    [PacketDefinition((byte) PacketType.TIME_SYNCHRONIZATION)]
    public class TimeSynchronizationPacket : NetPacket {
        [SyncedVar] public long server_timestamp = 0;

        public TimeSynchronizationPacket() {
            if(server_timestamp <= 0) {
                server_timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        public TimeSynchronizationPacket(long server_timestamp) {
            this.server_timestamp = server_timestamp;
        }
    }
}
