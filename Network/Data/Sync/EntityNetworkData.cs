using AMP.Network.Client.NetworkComponents;
using AMP.Network.Packets.Implementation;
using System;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class EntityNetworkData : NetworkData {

        internal int networkedId = 0;
        internal int clientsideId = 0;

        internal string type = "";

        internal NetworkEntity networkEntity;
        internal ThunderEntity entity;
        internal Vector3 position;
        internal Vector3 rotation;

        public EntityNetworkData() { }

        public EntityNetworkData(Vector3 position, Vector3 rotation) {
            this.position = position;
            this.rotation = rotation;
        }

        public EntityNetworkData(string type, Vector3 position, Vector3 rotation) : this(position, rotation) {
            this.type = type;
            this.position = position;
            this.rotation = rotation;
        }

        public EntityNetworkData(ThunderEntity entity) : this(entity.Center, entity.RootTransform.eulerAngles) {
            this.entity = entity;
            if(entity is Golem) {
                this.type = "golem";
            } else {
                this.type = "unknown";
            }

            clientsideId = ModManager.clientSync.syncData.currentClientEntityId++;
        }

        internal void Apply(EntitySpawnPacket entitySpawnPacket) {
            this.type = entitySpawnPacket.type;
            this.position = entitySpawnPacket.position;
            this.rotation = entitySpawnPacket.rotation;
        }

        internal void Apply(EntityPositionPacket entityPositionPacket) {
            this.position = entityPositionPacket.position;
            this.rotation = entityPositionPacket.rotation;
        }

        internal void UpdatePositionFromEntity() {
            this.position = entity.transform.position;
            this.rotation = entity.transform.eulerAngles;
        }

        internal void ApplyPositionToEntity() {
            networkEntity.targetPos = this.position;
            networkEntity.targetRot = Quaternion.Euler(this.rotation);
        }
    }
}
