using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_EQUIPMENT)]
    public class PlayerEquipmentPacket : NetPacket {
        [SyncedVar] public long     clientId;
        [SyncedVar] public Color[]  colors;
        [SyncedVar] public string[] equipment;

        public PlayerEquipmentPacket() { }

        public PlayerEquipmentPacket(long clientId, Color[] colors, string[] equipment) {
            this.clientId  = clientId;
            this.colors    = colors;
            this.equipment = equipment;
        }

        public PlayerEquipmentPacket(PlayerNetworkData pnd)
            : this(clientId:  pnd.clientId
                  , colors:    pnd.colors
                  , equipment: pnd.equipment
                  ) {

        }
    }
}
