using AMP.Network.Data.Sync;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(false, (byte) PacketType.CREATURE_RAGDOLL)]
    public class CreatureRagdollPacket : NetPacket {
        [SyncedVar]       public long         timestamp; // This Timestamp is the client timestamp including the server time offset, so its basically the server time
        [SyncedVar]       public long         creatureId;
        [SyncedVar]       public Vector3      position;
        [SyncedVar(true)] public float        rotationY;
        [SyncedVar(true)] public float        rotationYVel;
        [SyncedVar(true)] public Vector3      velocity;
        [SyncedVar(true)] public Vector3[]    ragdollPositions;
        [SyncedVar(true)] public Quaternion[] ragdollRotations;
        [SyncedVar(true)] public Vector3[]    velocities;
        [SyncedVar(true)] public Vector3[]    angularVelocities;

        public CreatureRagdollPacket() { }

        public CreatureRagdollPacket(long timestamp, long creatureId, Vector3 position, float rotationY, Vector3 velocity, float rotationYVel, Vector3[] ragdollPositions, Quaternion[] ragdollRotations, Vector3[] velocities, Vector3[] angularVelocities) {
            this.timestamp    = timestamp;
            this.creatureId   = creatureId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.velocity     = velocity;
            this.rotationYVel = rotationYVel;
            this.ragdollPositions  = ragdollPositions;
            this.ragdollRotations  = ragdollRotations;
            this.velocities        = velocities;
            this.angularVelocities = angularVelocities;
        }

        public CreatureRagdollPacket(CreatureNetworkData cnd) 
            : this( timestamp:    cnd.dataTimestamp
                  , creatureId:   cnd.networkedId
                  , position:     cnd.position
                  , rotationY:    cnd.rotationY
                  , velocity:     cnd.velocity
                  , rotationYVel: cnd.rotationYVel
                  , ragdollPositions:  cnd.ragdollPositions
                  , ragdollRotations:  cnd.ragdollRotations
                  , velocities:        cnd.ragdollVelocity
                  , angularVelocities: cnd.ragdollAngularVelocity
                  ) {

        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];
                if(cnd.isSpawning) return true;

                // Do our prediction
                float compensationFactor = NetworkData.GetCompensationFactor(timestamp);

                if(ModManager.safeFile.modSettings.ShouldPredict(compensationFactor)) {
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

                cnd.Apply(this);

                Dispatcher.Enqueue(() => {
                    cnd.ApplyPositionToCreature();
                });
            }
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            if(ModManager.serverInstance.creatures.ContainsKey(creatureId)) {
                if(ModManager.serverInstance.creature_owner[creatureId] != client.ClientId) return true;

                CreatureNetworkData cnd = ModManager.serverInstance.creatures[creatureId];
                cnd.Apply(this);

                server.SendToAllExcept(this, client.ClientId);
            }
            return true;
        }
    }
}
