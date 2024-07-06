using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents.Parts;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.Threading;
using System;
using System.Linq;
using System.Reflection;
using ThunderRoad;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.LowLevel;
using static AMP.Network.Client.NetworkComponents.NetworkEntity;
using static ThunderRoad.SimpleBreakable;

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
                    if(end.networkEntity != null) continue;

                    Log.Debug(end.position);
                    Log.Debug(entity.RootTransform.position);

                    Log.Debug(end.position.Distance(entity.RootTransform.position));

                    if(end.position.Distance(entity.RootTransform.position) < 300) {
                        end.entity = entity;
                        networkData = end;
                        break;
                    }
                }

                if(networkData == null) {
                    networkData = new EntityNetworkData(entity);
                    new EntitySpawnPacket(networkData).SendToServerReliable();
                    Log.Debug(networkData.position);
                }
            }

            if(networkData == null) return;

            networkData.networkEntity = this;

            if(networkData.clientsideId != 0) {
                ModManager.clientSync.syncData.entities.TryAdd(-networkData.clientsideId, networkData);
                Log.Warn($"new {networkData.type} entity");
            } else {
                Log.Warn($"linked {networkData.type} entity");
                networkData.ApplyPositionToEntity();
            }
            SetupEntity();
        }

        public override void ManagedUpdate() {
            if(networkData.clientsideId > 0) return;

            if(networkData.entity is Golem) {
                if(((Golem)networkData.entity).state == GolemController.State.WakingUp) return;
            }

            base.ManagedUpdate();

            if(networkData.entity is Golem) {
                Golem golem = ((Golem)networkData.entity);
                if(positionVelocity.magnitude > 1f) {
                    golem.SetMove(true);
                } else {
                    golem.SetMove(false);
                }
            }
        }

        void SetupEntity() {
            if(networkData.entity is Golem) {
                SetupGolem();
            }

            if(IsSending()) {
                NetworkComponentManager.SetTickRate(this, UnityEngine.Random.Range(50, 200), ManagedLoops.Update);
            } else {
                NetworkComponentManager.SetTickRate(this, 1, ManagedLoops.Update);
            }
        }

        internal void CheckChanges() {
            if(networkData.entity is Golem) {
                Golem golem = ((Golem)networkData.entity);
                if(golem.currentAbility != null) {
                    if(lastAbility != golem.abilities.IndexOf(golem.currentAbility)) {
                        lastAbility = golem.abilities.IndexOf(golem.currentAbility);

                        new EntityStatePacket(networkData.networkedId, (int) EntityState.GOLEM_STATE, 10, new int[] { lastAbility }).SendToServerReliable();
                        Log.Info(Defines.CLIENT, "Golem used ability " + golem.currentAbility.name);
                    }
                }else if(lastAbility != -1) {

                    if(lastAbility == -2 || UnityEngine.Random.Range(0, 4) == 1) {
                        int playerToAttack = UnityEngine.Random.Range(0, ModManager.clientSync.syncData.players.Count + 1);
                        if(playerToAttack >= ModManager.clientSync.syncData.players.Count) {
                            playerToAttack = ModManager.clientSync.syncData.myPlayerData.clientId;
                        } else {
                            playerToAttack = ModManager.clientSync.syncData.players.Keys.ToArray()[playerToAttack];
                        }

                        new EntityStatePacket(networkData.networkedId, (int)EntityState.GOLEM_STATE, 11, new int[] { playerToAttack }).SendToServerReliable();
                    }
                    lastAbility = -1;
                }
            }
        }

        private int lastAbility = -2;

        internal void HandleEntityStateChange(EntityStatePacket entityState) {
            int state = entityState.state;
            int value = entityState.value;

            switch((EntityState) state) {
                case EntityState.GOLEM_STATE:
                    if(networkData.entity is Golem) {
                        Golem golem = (Golem)networkData.entity;
                        
                        if(value >= 10) {
                            if(value == 10) {
                                if(entityState.vars[0] < golem.abilities.Count) {
                                    Log.Info(Defines.CLIENT, "Making golem use " + golem.abilities[entityState.vars[0]]);

                                    if(golem.headCrystalBody == null) { // Fix, dont know why
                                        golem.headCrystalBody = new GameObject("HeadCrystalBody").AddComponent<Rigidbody>();
                                    }

                                    try {
                                        golem.EndAbility();
                                    }catch(Exception) {
                                        golem.currentAbility = null;
                                    }

                                    golem.UseAbility(entityState.vars[0]);
                                }
                            }else if(value == 11) {
                                if(ModManager.clientSync.syncData.players.ContainsKey(entityState.vars[0])) {
                                    golem.SetAttackTarget(ModManager.clientSync.syncData.players[entityState.vars[0]].creature.transform);
                                } else {
                                    golem.TargetPlayer();
                                }
                            }
                        } else {

                            Golem.State gstate = (Golem.State) value;

                            Log.Warn(gstate);

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
                                Log.Warn(gstate.ToString());
                                RampageType rampageType = RampageType.Melee;
                                if(entityState.vars.Length > 0) {
                                    rampageType = (RampageType) entityState.vars[0];
                                }
                                Log.Warn(rampageType);
                                golem.Rampage(rampageType);
                            } else if(gstate == GolemController.State.Defeated) {
                                golem.Defeat();
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
                    }
                    break;
            }
        }

        #region Golem Stuff
        void SetupGolem() {
            Golem golem = (Golem) networkData.entity;

            golem.OnGolemStateChange += (GolemController.State newState) => {
                if(newState != GolemController.State.WakingUp && networkData.clientsideId == 0) return;
                if(newState == GolemController.State.Rampage) return;

                new EntityStatePacket(networkData.networkedId, (int) EntityState.GOLEM_STATE, (int) newState).SendToServerReliable();
            };

            golem.OnGolemRampage += () => {
                if(networkData.clientsideId == 0) return;

                Dispatcher.Enqueue(() => { // Needs to be next frame so we know what kinda rampage we dealing with
                    if(golem.currentAbility != null) {
                        new EntityStatePacket(networkData.networkedId, (int) EntityState.GOLEM_STATE, (int) GolemController.State.Rampage, new int[] { (int) golem.currentAbility.rampageType }).SendToServerReliable();
                    } else {
                        new EntityStatePacket(networkData.networkedId, (int) EntityState.GOLEM_STATE, (int) GolemController.State.Rampage).SendToServerReliable();
                    }
                });
            };

            if(networkData.clientsideId == 0) {
                UpdateManager.RemoveBehaviour(golem);
                //BrainModuleGolem.local?.OnBrainStop();
                golem.navMeshAgent.enabled = false;
            }

            characterController = golem.characterController;
        }
        #endregion
    }
}
