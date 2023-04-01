﻿using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PREPARE_LEVEL_CHANGE)]
    public class PrepareLevelChangePacket : NetPacket {
        [SyncedVar] public string username;
        [SyncedVar] public string level;
        [SyncedVar] public string mode;

        public PrepareLevelChangePacket() { }

        public PrepareLevelChangePacket(string username, string level, string mode) {
            this.username = username;
            this.level = level;
            this.mode = mode;
        }
    }
}
