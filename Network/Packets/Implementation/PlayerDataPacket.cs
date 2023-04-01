using AMP.Network.Data.Sync;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_DATA)]
    public class PlayerDataPacket : NetPacket {
        [SyncedVar]       public int     clientId;
        [SyncedVar]       public string  name;
        [SyncedVar]       public string  creatureId;
        [SyncedVar(true)] public float   height;
        [SyncedVar]       public Vector3 playerPos;
        [SyncedVar(true)] public float   playerRotY;

        public PlayerDataPacket() { }

        public PlayerDataPacket(int clientId, string name, string creatureId, float height, Vector3 playerPos, float playerRotY) {
            this.clientId   = clientId;
            this.name       = name;
            this.creatureId = creatureId;
            this.height     = height;
            this.playerPos  = playerPos;
            this.playerRotY  = playerRotY;
        }

        public PlayerDataPacket(PlayerNetworkData pnd) 
            : this( clientId:   pnd.clientId
                  , name:       pnd.name
                  , creatureId: pnd.creatureId
                  , height:     pnd.height
                  , playerPos:  pnd.position
                  , playerRotY: pnd.rotationY
                  ){

        }
    }
}
