using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.LEVEL_CHANGE)]
    public class LevelChangePacket : NetPacket {
        [SyncedVar] public string   levelName;
        [SyncedVar] public string   mode;
        [SyncedVar] public string[] options;

        public LevelChangePacket(string levelName, string mode, string[] options) {
            this.levelName = levelName;
            this.mode      = mode;
            this.options   = options;
        }
    }
}
