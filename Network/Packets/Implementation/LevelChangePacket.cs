using AMP.Network.Packets.Attributes;
using System.Collections.Generic;
using ThunderRoad.AI.Get;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.LEVEL_CHANGE)]
    public class LevelChangePacket : NetPacket {
        [SyncedVar] public string   levelName;
        [SyncedVar] public string   mode;
        [SyncedVar] public string[] options;

        public Dictionary<string, string> option_dict {
            get {
                Dictionary<string, string> result = new Dictionary<string, string>();

                int i = 0;
                while(i < options.Length) {
                    result.Add(options[i++], options[i++]);
                }

                return result;
            }
        }

        public LevelChangePacket(string levelName, string mode) {
            this.levelName = levelName;
            this.mode      = mode;
            this.options   = new string[0];
        }

        public LevelChangePacket(string levelName, string mode, string[] options) : this(levelName, mode) {
            this.options   = options;
        }

        public LevelChangePacket(string levelName, string mode, Dictionary<string, string> options) : this(levelName, mode) {
            this.options = new string[options.Count * 2];
            int i = 0;
            foreach(KeyValuePair<string, string> kvp in options) {
                this.options[i++] = kvp.Key;
                this.options[i++] = kvp.Value;
            }
        }
    }
}
