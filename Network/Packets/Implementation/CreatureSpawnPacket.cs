using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_SPAWN)]
    public class CreatureSpawnPacket : NetPacket {
        [SyncedVar]       public long     creatureId;
        [SyncedVar]       public long     clientsideId;
        [SyncedVar]       public string   type;
        [SyncedVar]       public string   container;
        [SyncedVar]       public byte     factionId;
        [SyncedVar]       public Vector3  position;
        [SyncedVar(true)] public float    rotationY;
        [SyncedVar]       public float    health;
        [SyncedVar]       public float    maxHealth;
        [SyncedVar(true)] public float    height;
        [SyncedVar]       public string[] equipment = new string[0];
        [SyncedVar]       public Color[]  colors    = new Color[0];

        public CreatureSpawnPacket() { }
        
        public CreatureSpawnPacket(long creatureId, long clientsideId, string type, string container, byte factionId, Vector3 position, float rotationY, float health, float maxHealth, float height, string[] equipment, Color[] colors) {
            this.creatureId   = creatureId;
            this.clientsideId = clientsideId;
            this.type         = type;
            this.container    = container;
            this.factionId    = factionId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.health       = health;
            this.maxHealth    = maxHealth;
            this.height       = height;
            this.equipment    = equipment;
            this.colors       = colors;
        }

        public CreatureSpawnPacket(CreatureNetworkData cnd)
            : this( creatureId:   cnd.networkedId
                  , clientsideId: cnd.clientsideId
                  , type:         cnd.creatureType
                  , container:    cnd.containerID
                  , factionId:    cnd.factionId
                  , position:     cnd.position
                  , rotationY:    cnd.rotationY
                  , health:       cnd.health
                  , maxHealth:    cnd.maxHealth
                  , height:       cnd.height
                  , equipment:    cnd.equipment
                  , colors:       cnd.colors
                  ){

        }
    }
}
