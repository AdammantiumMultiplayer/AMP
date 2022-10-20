﻿using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_SET)]
    public class PlayerHealthSetPacket {
        [SyncedVar] public long  playerId;
        [SyncedVar] public float health;

        public PlayerHealthSetPacket(long playerId, float health) {
            this.playerId = playerId;
            this.health   = health;
        }
    }
}
