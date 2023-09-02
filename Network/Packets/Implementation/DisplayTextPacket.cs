using AMP.GameInteraction.Components;
using AMP.SupportFunctions;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.DISPLAY_TEXT)]
    public class DisplayTextPacket : AMPPacket {
        [SyncedVar]       public string  identifier     = RandomGenerator.RandomString(10);
        [SyncedVar]       public string  text           = "";
        [SyncedVar]       public Color   textColor      = Color.white;
        [SyncedVar]       public Vector3 position       = Vector3.zero;
        [SyncedVar(true)] public Vector3 rotation       = Vector3.zero;
        [SyncedVar]       public bool    lookAtPlayer   = false;
        [SyncedVar]       public bool    relativeToPlayer = false;
        [SyncedVar]       public float   displayTime    = 10f;
        [SyncedVar]       public float   fadeTime       = 0.5f;
        [SyncedVar]       public int     textSize       = 500;

        public DisplayTextPacket() { }


        public DisplayTextPacket(string text, Vector3 position, Vector3 rotation) {
            this.text = text;
            this.position = position;
            this.rotation = rotation;
        }

        public DisplayTextPacket(string text, Vector3 position, Vector3 rotation, bool relativeToPlayer)
            : this( text: text
                  , position: position
                  , rotation: rotation
                  ) {
            this.relativeToPlayer = relativeToPlayer;
        }

        public DisplayTextPacket(string identifier, string text, Vector3 position, Vector3 rotation, bool relativeToPlayer) 
            : this( text: text
                  , position: position
                  , rotation: rotation
                  , relativeToPlayer: relativeToPlayer
                  ) {
            this.identifier = identifier;
        }

        public DisplayTextPacket(string identifier, string text, Color textColor, Vector3 position, Vector3 rotation, float displayTime) {
            this.identifier = identifier;
            this.text = text;
            this.textColor = textColor;
            this.position = position;
            this.rotation = rotation;
            this.displayTime = displayTime;
        }

        public DisplayTextPacket(string text, Vector3 position, bool relativeToPlayer)
            : this( text: text
                  , position: position
                  , rotation: Vector3.zero
                  ) {
            this.relativeToPlayer = relativeToPlayer;
        }

        public DisplayTextPacket(string identifier, string text, Vector3 position, Vector3 rotation, bool lookAtPlayer, bool relativeToPlayer, float displayTime)
            : this( identifier: identifier
                  , text: text
                  , position: position
                  , rotation: rotation
                  , relativeToPlayer: relativeToPlayer
                  ) {
            this.lookAtPlayer = lookAtPlayer;
            this.displayTime = displayTime;
        }


        public DisplayTextPacket(string text, Vector3 position, bool lookAtPlayer, bool relativeToPlayer, float displayTime) {
            this.text = text;
            this.position = position;
            this.lookAtPlayer = lookAtPlayer;
            this.relativeToPlayer = relativeToPlayer;
            this.displayTime = displayTime;
        }

        public DisplayTextPacket(string identifier, string text, Vector3 position, bool lookAtPlayer, bool relativeToPlayer, float displayTime) {
            this.identifier = identifier;
            this.text = text;
            this.position = position;
            this.lookAtPlayer = lookAtPlayer;
            this.relativeToPlayer = relativeToPlayer;
            this.displayTime = displayTime;
        }

        public DisplayTextPacket(string text, Color textColor, Vector3 position, bool lookAtPlayer, bool relativeToPlayer, float displayTime) {
            this.text = text;
            this.textColor = textColor;
            this.position = position;
            this.lookAtPlayer = lookAtPlayer;
            this.relativeToPlayer = relativeToPlayer;
            this.displayTime = displayTime;
        }

        public DisplayTextPacket(string identifier, string text, Color textColor, Vector3 position, bool lookAtPlayer, bool relativeToPlayer, float displayTime) {
            this.identifier = identifier;
            this.text = text;
            this.textColor = textColor;
            this.position = position;
            this.lookAtPlayer = lookAtPlayer;
            this.relativeToPlayer = relativeToPlayer;
            this.displayTime = displayTime;
        }

        public override bool ProcessClient(NetamiteClient client) {
            Dispatcher.Enqueue(() => {
                TextDisplay.ShowTextDisplay(this);
            });
            return true;
        }
    }
}
