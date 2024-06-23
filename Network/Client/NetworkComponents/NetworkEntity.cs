using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using System;
using System.Reflection;
using ThunderRoad;

namespace AMP.Network.Client.NetworkComponents {
    internal class NetworkEntity : NetworkPositionRotation {

        public enum EntityState {
            GOLEM_STATE
        }

        public EntityNetworkData networkData;

        void Start() {
            ThunderEntity entity = GetComponent<ThunderEntity>();
            if(entity == null) { 
                Destroy(this);
                return;
            }

            networkData = null;

            if(entity is Golem) {
                foreach(EntityNetworkData end in ModManager.clientSync.syncData.entities.Values) {
                    if(end.entity != null) continue;
                    if(end.type != "golem") continue;

                    if(end.position.Distance(entity.Center) < 5) {
                        end.entity = entity;
                        networkData = end;
                        break;
                    }
                }
                if(networkData == null) {
                    networkData = new EntityNetworkData(entity);
                    new EntitySpawnPacket(networkData).SendToServerReliable();
                }
            }

            if(networkData == null) return;

            networkData.networkEntity = this;

            if(networkData.clientsideId != 0) {
                ModManager.clientSync.syncData.entities.TryAdd(-networkData.clientsideId, networkData);
                Log.Warn($"new {networkData.type} entity");
            } else {
                ModManager.clientSync.syncData.entities.TryAdd(networkData.networkedId, networkData);
                Log.Warn($"linked {networkData.type} entity");
            }
            SetupEntity();
        }

        public override void ManagedUpdate() {
            if(networkData.clientsideId > 0) return;

            base.ManagedUpdate();
        }

        void SetupEntity() {
            if(networkData.entity is Golem) {
                SetupGolem();
            }
        }


        internal void HandleEntityStateChange(int state, int value) {
            Log.Warn("EntityStatePacket " + (EntityState) state);

            switch((EntityState) state) {
                case EntityState.GOLEM_STATE:
                    if(networkData.entity is Golem) {
                        Golem golem = (Golem)networkData.entity;

                        Golem.State gstate = (Golem.State)value;

                        if(gstate != GolemController.State.Stunned) {
                            if(golem.isStunned) {
                                golem.StopStun();
                            }
                        }

                        if(gstate == GolemController.State.WakingUp) {
                            golem.SetAwake(true);
                        }else if(gstate == GolemController.State.Stunned) {
                            golem.Stun(30);
                        } else if(gstate == GolemController.State.Rampage) {
                            golem.Rampage();
                        } else if(gstate == GolemController.State.Dead) {
                            golem.Kill();
                        } else {
                            MethodInfo mInfoMethod = typeof(Golem).GetMethod(
                                                        "ChangeState",
                                                        BindingFlags.Instance | BindingFlags.NonPublic,
                                                        Type.DefaultBinder,
                                                        new[] { typeof(Golem.State) },
                                                        null);

                            mInfoMethod.Invoke(golem, new object[] { (Golem.State) value });
                        }
                    }
                    break;
            }
        }

        #region Golem Stuff
        void SetupGolem() {
            Golem golem = (Golem) networkData.entity;

            golem.OnGolemStateChange += (GolemController.State newState) => {
                if(newState != GolemController.State.WakingUp || networkData.clientsideId == 0) return;

                new EntityStatePacket(networkData.networkedId, (int) EntityState.GOLEM_STATE, (int) newState).SendToServerReliable();
            };

            if(networkData.clientsideId == 0) {
                UpdateManager.RemoveBehaviour(golem);
            }

        }
        #endregion
    }
}
