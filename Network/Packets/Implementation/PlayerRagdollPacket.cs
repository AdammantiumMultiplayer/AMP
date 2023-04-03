﻿using AMP.Extension;
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
    [PacketDefinition(false, (byte) PacketType.PLAYER_RAGDOLL)]
    public class PlayerRagdollPacket : NetPacket {
        [SyncedVar]       public long         timestamp; // This Timestamp is the client timestamp including the server time offset, so its basically the server time
        [SyncedVar]       public long         playerId;
        [SyncedVar]       public Vector3      position;
        [SyncedVar(true)] public float        rotationY;
        [SyncedVar(true)] public Vector3      velocity;
        [SyncedVar(true)] public float        rotationYVel;
        [SyncedVar(true)] public Vector3[]    ragdollPositions;
        [SyncedVar(true)] public Quaternion[] ragdollRotations;
        [SyncedVar(true)] public Vector3[]    velocities;
        [SyncedVar(true)] public Vector3[]    angularVelocities;

        public PlayerRagdollPacket() { }

        public PlayerRagdollPacket(long timestamp, long playerId, Vector3 position, float rotationY, Vector3 velocity, float rotationYVel, Vector3[] ragdollPositions, Quaternion[] ragdollRotations, Vector3[] velocities, Vector3[] angularVelocities) {
            this.timestamp    = timestamp;
            this.playerId     = playerId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.velocity     = velocity;
            this.rotationYVel = rotationYVel;
            this.ragdollPositions  = ragdollPositions;
            this.ragdollRotations  = ragdollRotations;
            this.velocities        = velocities;
            this.angularVelocities = angularVelocities;
        }

        public PlayerRagdollPacket(PlayerNetworkData pnd)
            : this( timestamp: pnd.dataTimestamp
                  , playerId:  pnd.clientId
                  , position:  pnd.position
                  , rotationY: pnd.rotationY
                  , velocity:  pnd.velocity
                  , rotationYVel:      pnd.rotationYVel
                  , ragdollPositions:  pnd.ragdollPositions
                  , ragdollRotations:  pnd.ragdollRotations
                  , velocities:        pnd.ragdollVelocity
                  , angularVelocities: pnd.ragdollAngularVelocity
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(playerId == client.ClientId) {
                #if DEBUG_SELF
                playerRagdollPacket.position += -Vector3.right * 2;
                #else
                return true;
                #endif
            }

            if(ModManager.clientSync.syncData.players.ContainsKey(playerId)) {
                PlayerNetworkData pnd = ModManager.clientSync.syncData.players[playerId];

                // Do our prediction
                float compensationFactor = NetworkData.GetCompensationFactor(timestamp);

                if(compensationFactor > 0.03f) {
                    Vector3[] estimatedRagdollPos = ragdollPositions;
                    Quaternion[] estimatedRagdollRotation = ragdollRotations;
                    Vector3 estimatedPlayerPos = position;
                    float estimatedPlayerRot = rotationY;

                    estimatedPlayerPos += velocity * compensationFactor;
                    estimatedPlayerRot += rotationYVel * compensationFactor;
                    for(int i = 0; i < estimatedRagdollPos.Length; i++) {
                        estimatedRagdollPos[i] += velocities[i] * compensationFactor;
                    }
                    for(int i = 0; i < estimatedRagdollRotation.Length; i++) {
                        estimatedRagdollRotation[i].eulerAngles += angularVelocities[i] * compensationFactor;
                    }
                    position = estimatedPlayerPos;
                    rotationY = estimatedPlayerRot;
                    ragdollPositions = estimatedRagdollPos;
                    ragdollRotations = estimatedRagdollRotation;
                }

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
            if(client.ClientId != playerId) return true;

            cd.playerSync.Apply(this);

            #if DEBUG_SELF
            // Just for debug to see yourself
            server.SendToAll(this);
            #else
            server.SendToAllExcept(this, client.ClientId);
            #endif
            return true;
        }
    }
}
