using AMP.Network.Packets.Attributes;
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
        [SyncedVar(true)] public Vector3  rotation;
        [SyncedVar]       public float    health;
        [SyncedVar]       public float    maxHealth;
        [SyncedVar(true)] public float    height;
        [SyncedVar]       public string[] equipment;

        public CreatureSpawnPacket(long creatureId, long clientsideId, string type, string container, byte factionId, Vector3 position, Vector3 rotation, float health, float maxHealth, float height, string[] equipment) {
            this.creatureId   = creatureId;
            this.clientsideId = clientsideId;
            this.type         = type;
            this.container    = container;
            this.factionId    = factionId;
            this.position     = position;
            this.rotation     = rotation;
            this.health       = health;
            this.maxHealth    = maxHealth;
            this.height       = height;
            this.equipment    = equipment;
        }
    }
}
