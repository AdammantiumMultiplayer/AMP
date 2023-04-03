using AMP.Extension;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using System.Net;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(false, (byte)PacketType.PLAYER_POSITION)]
    public class PlayerPositionPacket : NetPacket {
        [SyncedVar]       public long    playerId;

        [SyncedVar(true)] public Vector3 handLeftPos;
        [SyncedVar(true)] public Vector3 handLeftRot;

        [SyncedVar(true)] public Vector3 handRightPos;
        [SyncedVar(true)] public Vector3 handRightRot;

        [SyncedVar(true)] public Vector3 headPos;
        [SyncedVar(true)] public Vector3 headRot;

        [SyncedVar]       public Vector3 position;
        [SyncedVar(true)] public float   rotationY;

        public PlayerPositionPacket() { }

        public PlayerPositionPacket(long playerId, Vector3 handLeftPos, Vector3 handLeftRot, Vector3 handRightPos, Vector3 handRightRot, Vector3 headPos, Vector3 headRot, Vector3 playerPos, float playerRot) {
            this.playerId     = playerId;
            this.handLeftPos  = handLeftPos;
            this.handLeftRot  = handLeftRot;
            this.handRightPos = handRightPos;
            this.handRightRot = handRightRot;
            this.headPos      = headPos;
            this.headRot      = headRot;
            this.position    = playerPos;
            this.rotationY    = playerRot;
        }

        public PlayerPositionPacket(PlayerNetworkData pnd) 
            : this( playerId:     pnd.clientId
                  , handLeftPos:  pnd.handLeftPos
                  , handLeftRot:  pnd.handLeftRot
                  , handRightPos: pnd.handRightPos
                  , handRightRot: pnd.handRightRot
                  , headPos:      pnd.headPos
                  , headRot:      pnd.headRot
                  , playerPos:    pnd.position
                  , playerRot:    pnd.rotationY
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(playerId == client.ClientId) {
                #if DEBUG_SELF
                position     += Vector3.right * 2;
                handLeftPos  += Vector3.right * 2;
                handRightPos += Vector3.right * 2;
                headPos      += Vector3.right * 2;
                #else
                return true;
                #endif
            }

            if(ModManager.clientSync.syncData.players.ContainsKey(playerId)) {
                PlayerNetworkData pnd = ModManager.clientSync.syncData.players[playerId];
                pnd.Apply(this);
                pnd.PositionChanged();

                Dispatcher.Enqueue(() => {
                    ModManager.clientSync.MovePlayer(pnd);
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            ClientData cd = client.GetData();

            if(cd.playerSync == null) return true;

            cd.playerSync.Apply(this);
            cd.playerSync.clientId = client.ClientId;

            #if DEBUG_SELF
            // Just for debug to see yourself
            server.SendToAll(new PlayerPositionPacket(cd.playerSync));//, client.ClientId);
            #else
            server.SendToAllExcept(new PlayerPositionPacket(cd.playerSync), client.ClientId);
            #endif
            return true;
        }
    }
}
