using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_EQUIPMENT)]
    public class PlayerEquipmentPacket : NetPacket {
        [SyncedVar] public long     playerId;
        [SyncedVar] public Color[]  colors;
        [SyncedVar] public string[] equipment;
    }
}
