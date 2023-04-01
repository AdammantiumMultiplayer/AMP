﻿using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_HEALTH_SET)]
    public class PlayerHealthSetPacket : NetPacket {
        [SyncedVar] public long  playerId;
        [SyncedVar] public float health;

        public PlayerHealthSetPacket() { }

        public PlayerHealthSetPacket(long playerId, float health) {
            this.playerId = playerId;
            this.health   = health;
        }

        public PlayerHealthSetPacket(PlayerNetworkData pnd) 
            : this( playerId: pnd.clientId
                  , health:   pnd.health
                  ){

        }
    }
}
