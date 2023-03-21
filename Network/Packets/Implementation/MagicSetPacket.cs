using AMP.Datatypes;
using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.MAGIC_SET)]
    internal class MagicSetPacket : NetPacket {
        [SyncedVar] public string magicId;
        [SyncedVar] public byte   handIndex;
        [SyncedVar] public long   casterNetworkId;
        [SyncedVar] public byte   casterType;

        public MagicSetPacket() { }
        
        public MagicSetPacket(string magicId, byte handIndex, long casterNetworkId, ItemHolderType casterType) {
            this.magicId         = magicId;
            this.handIndex       = handIndex;
            this.casterNetworkId = casterNetworkId;
            this.casterType      = (byte) casterType;
        }
    }
}
