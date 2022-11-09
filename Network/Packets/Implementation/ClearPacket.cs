using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CLEAR_DATA)]
    public class ClearPacket : NetPacket {
        [SyncedVar] public bool clearItems = true;
        [SyncedVar] public bool clearCreatures = true;

        public ClearPacket() { }

        public ClearPacket(bool clearItems, bool clearCreatures) {
            this.clearItems = clearItems;
            this.clearCreatures = clearCreatures;
        }
    }
}
