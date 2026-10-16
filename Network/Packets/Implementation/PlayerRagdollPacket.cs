using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(false, (byte) PacketType.PLAYER_RAGDOLL)]
    public class PlayerRagdollPacket : AMPPacket {
        [SyncedVar]       public long         timestamp; // This Timestamp is the client timestamp including the server time offset, so its basically the server time
        [SyncedVar]       public int          playerId;
        [SyncedVar]       public Vector3      position;
        [SyncedVar(true)] public float        rotationY;
        [SyncedVar(true)] public Vector3      velocity;
        [SyncedVar(true)] public float        rotationYVel;
        [SyncedVar(true)] public Vector3[]    ragdollPositions;
        [SyncedVar(true)] public Quaternion[] ragdollRotations;
        [SyncedVar(true)] public Vector3[]    velocities;
        [SyncedVar(true)] public Vector3[]    angularVelocities;

        public PlayerRagdollPacket() { }

        public PlayerRagdollPacket(long timestamp, int playerId, Vector3 position, float rotationY, Vector3 velocity, float rotationYVel, Vector3[] ragdollPositions, Quaternion[] ragdollRotations, Vector3[] velocities, Vector3[] angularVelocities) {
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

            /*
            string msg = "WRITE " + timestamp + "\n";
            foreach(Vector3 pos in ragdollPositions) {
                msg += pos.ToString() + "\n";
            }
            msg += "\n";
            foreach(Quaternion rot in ragdollRotations) {
                msg += rot.ToString() + "\n";
            }
            Log.Debug(msg);
            */
            //Log.Debug(playerId);
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
                position += -Vector3.right * 2;
                /*
                string msg = "READ " + timestamp + "\n";
                foreach(Vector3 pos in ragdollPositions) {
                    msg += pos.ToString() + "\n";
                }
                msg += "\n";
                foreach(Quaternion rot in ragdollRotations) {
                    msg += rot.ToString() + "\n";
                }
                Log.Debug(msg);
                */
                #else
                return true;
                #endif
            }

            AMP.Network.Client.NetworkComponents.NetworkCreature.RagdollDebug("recv" + playerId, $"Received ragdoll of player {playerId}: known {ModManager.clientSync.syncData.players.ContainsKey(playerId)}, {(ragdollPositions != null ? ragdollPositions.Length : -1)} parts, timestamp {timestamp}"); // RAGDOLL-DEBUG
            if(ModManager.clientSync.syncData.players.ContainsKey(playerId)) {
                PlayerNetworkData pnd = ModManager.clientSync.syncData.players[playerId];

                // Skip the packet if we already have a newer one
                if(pnd.lastRagdollTimestamp > timestamp) { AMP.Network.Client.NetworkComponents.NetworkCreature.RagdollDebug("old" + playerId, $"Dropped ragdoll of player {playerId}, timestamp {timestamp} is older than {pnd.lastRagdollTimestamp}"); return true; } // RAGDOLL-DEBUG
                pnd.lastRagdollTimestamp = timestamp;

                // Do our prediction
                float compensationFactor = NetworkData.GetCompensationFactor(timestamp);

                if(ModManager.safeFile.modSettings.ShouldPredict(compensationFactor)) {
                    Vector3[] estimatedRagdollPos = ragdollPositions;
                    Quaternion[] estimatedRagdollRotation = ragdollRotations;
                    Vector3 estimatedPlayerPos = position;
                    float estimatedPlayerRot = rotationY;

                    estimatedPlayerPos = NetworkData.Compensate(estimatedPlayerPos, velocity, compensationFactor);
                    estimatedPlayerRot = NetworkData.Compensate(estimatedPlayerRot, rotationYVel * Mathf.Rad2Deg, compensationFactor); // Angular velocity is in rad/s, the rotation in degrees
                    for(int i = 0; i < estimatedRagdollPos.Length && i < velocities.Length; i++) {
                        // The positions are relative to the root, which already gets compensated above, so only use the parts own movement
                        estimatedRagdollPos[i] = NetworkData.Compensate(estimatedRagdollPos[i], velocities[i] - velocity, compensationFactor);
                    }
                    for(int i = 0; i < estimatedRagdollRotation.Length && i < angularVelocities.Length; i++) {
                        estimatedRagdollRotation[i] = NetworkData.Compensate(estimatedRagdollRotation[i], angularVelocities[i], compensationFactor);
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

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            if(client.ClientId != playerId) return true;

            client.player.Apply(this);

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
