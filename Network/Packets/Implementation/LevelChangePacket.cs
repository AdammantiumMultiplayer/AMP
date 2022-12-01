using AMP.Network.Packets.Attributes;
using System.Collections.Generic;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.DO_LEVEL_CHANGE)]
    public class LevelChangePacket : NetPacket {
        [SyncedVar] public string    level;
        [SyncedVar] public string    mode;
        [SyncedVar] public string[]  options = new string[0];
        [SyncedVar] public EventTime eventTime = EventTime.OnEnd;

        public Dictionary<string, string> option_dict {
            get {
                Dictionary<string, string> result = new Dictionary<string, string>();

                if(options == null) return result;

                int i = 0;
                while(i < options.Length) {
                    result.Add(options[i++], options[i++]);
                }

                return result;
            }
        }

        public LevelChangePacket() { }

        public LevelChangePacket(string level, string mode) {
            this.level     = level;
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

        public LevelChangePacket(string levelName, string mode, Dictionary<string, string> options, EventTime eventTime) : this(levelName, mode, options) {
            this.eventTime = eventTime;
        }
    }
}
