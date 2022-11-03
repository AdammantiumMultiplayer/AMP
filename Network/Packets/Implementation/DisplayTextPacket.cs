using AMP.Network.Packets.Attributes;
using AMP.SupportFunctions;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.DISPLAY_TEXT)]
    public class DisplayTextPacket : NetPacket {
        [SyncedVar]       public string  identifier     = RandomGenerator.RandomString(10);
        [SyncedVar]       public string  text           = "";
        [SyncedVar]       public Vector3 position       = Vector3.zero;
        [SyncedVar(true)] public Vector3 rotation       = Vector3.zero;
        [SyncedVar]       public bool    alwaysToPlayer = false;
        [SyncedVar]       public bool    relativeToHead = false;
        [SyncedVar]       public float   displayTime    = 5f;

        public DisplayTextPacket() { }


        public DisplayTextPacket(string text, Vector3 position, Vector3 rotation) {
            this.text = text;
            this.position = position;
            this.rotation = rotation;
        }

        public DisplayTextPacket(string text, Vector3 position, Vector3 rotation, bool relativeToHead)
            : this( text: text
                  , position: position
                  , rotation: rotation
                  ) {
            this.relativeToHead = relativeToHead;
        }

        public DisplayTextPacket(string identifier, string text, Vector3 position, Vector3 rotation, bool relativeToHead) 
            : this( text: text
                  , position: position
                  , rotation: rotation
                  , relativeToHead: relativeToHead
                  ) {
            this.identifier = identifier;
        }

        public DisplayTextPacket(string text, Vector3 position, bool relativeToHead)
            : this( text: text
                  , position: position
                  , rotation: Vector3.zero
                  ) {
            this.relativeToHead = relativeToHead;
        }

        public DisplayTextPacket(string identifier, string text, Vector3 position, Vector3 rotation, bool alwaysToPlayer, bool relativeToHead, float displayTime)
            : this( identifier: identifier
                  , text: text
                  , position: position
                  , rotation: rotation
                  , relativeToHead: relativeToHead
                  ) {
            this.relativeToHead = relativeToHead;
            this.displayTime = displayTime;
        }
    }
}
