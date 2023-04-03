using AMP.Discord;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System.Net;
using System.Text.RegularExpressions;
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

        public override bool ProcessClient(NetamiteClient client) {
            if(clientId <= 0) return false;
            if(clientId == client.ClientId) {
                #if DEBUG_SELF
                        playerDataPacket.playerPos += Vector3.right * 2;
                #else
                return true;
                #endif
            }

            PlayerNetworkData pnd = ModManager.clientSync.syncData.players.GetOrAdd(clientId, new PlayerNetworkData());
            pnd.Apply(this);
            
            Dispatcher.Enqueue(() => {
                Spawner.TrySpawnPlayer(pnd);
            });

            DiscordIntegration.Instance.UpdateActivity();
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            ClientData cd = client.GetData();

            name = Regex.Replace(client.ClientName, @"[^\u0000-\u007F]+", string.Empty);
            cd.playerSync.Apply(this);

            cd.playerSync.clientId = client.ClientId;

            #if DEBUG_SELF
            // Just for debug to see yourself
            server.SendToAll(new PlayerDataPacket(client.playerSync));//, client.playerId);
            #else
            server.SendToAllExcept(new PlayerDataPacket(cd.playerSync), client.ClientId);
            #endif
            return true;
        }
    }
}
