using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_EQUIPMENT)]
    public class PlayerEquipmentPacket : NetPacket {
        [SyncedVar] public long     playerId;
        [SyncedVar] public Color[]  colors;
        [SyncedVar] public string[] equipment;

        public PlayerEquipmentPacket() { }

        public PlayerEquipmentPacket(long playerId, Color[] colors, string[] equipment) {
            this.playerId  = playerId;
            this.colors    = colors;
            this.equipment = equipment;
        }

        public PlayerEquipmentPacket(PlayerNetworkData pnd)
            : this( playerId:  pnd.clientId
                  , colors:    pnd.colors
                  , equipment: pnd.equipment
                  ) {

        }
    }
}
