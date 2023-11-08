using AMP.Network.Client.NetworkComponents.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    internal class NetworkComponentManager : MonoBehaviour {
        public static NetworkComponentManager Local;

        private static int fixedUpdateCount = 0;

        private static int updateCount = 0;

        private static int lateUpdateCount = 0;

        public static int frameCount;

        public static int fixedFrameCount;

        public static List<NetworkBehaviour> FixedUpdateLoop { get; } = new List<NetworkBehaviour>(2048);
        public static List<NetworkBehaviour> UpdateLoop { get; } = new List<NetworkBehaviour>(2048);
        public static List<NetworkBehaviour> LateUpdateLoop { get; } = new List<NetworkBehaviour>(2048);

        public static void AddBehaviour(NetworkBehaviour behaviour) {
            AddToLoop(behaviour, ManagedLoops.FixedUpdate);
            AddToLoop(behaviour, ManagedLoops.Update);
            AddToLoop(behaviour, ManagedLoops.LateUpdate);
        }

        public static void RemoveBehaviour(NetworkBehaviour behaviour) {
            RemoveFromLoop(behaviour, ManagedLoops.FixedUpdate);
            RemoveFromLoop(behaviour, ManagedLoops.Update);
            RemoveFromLoop(behaviour, ManagedLoops.LateUpdate);
        }

        private static List<NetworkBehaviour> GetLoop(ManagedLoops loop) {
            switch(loop) {
                case ManagedLoops.FixedUpdate: return FixedUpdateLoop;
                case ManagedLoops.Update:      return UpdateLoop;
                case ManagedLoops.LateUpdate:  return LateUpdateLoop;
                default: return null;
            };
        }

        private static void AddToLoop(NetworkBehaviour behaviour, ManagedLoops loop) {
            if(behaviour.EnabledManagedLoops.HasFlagNoGC(loop)) {
                GetLoop(loop).Add(behaviour);
                int num = GetCount(loop) + 1;
                behaviour.SetIndex(loop, num - 1);
                SetCount(loop, num);
            }
        }

        private static void RemoveFromLoop(NetworkBehaviour behaviour, ManagedLoops loop) {
            if(behaviour.EnabledManagedLoops.HasFlagNoGC(loop)) {
                int index = behaviour.GetIndex(loop);
                behaviour.SetIndex(loop, -1);
                int count = GetCount(loop);
                if(index != -1 && index < count) {
                    List<NetworkBehaviour> loop2 = GetLoop(loop);
                    loop2[count - 1].SetIndex(loop, index);
                    loop2.RemoveAtIgnoreOrder(index, count);
                    SetCount(loop, count - 1);
                }
            }
        }


        private static void SetCount(ManagedLoops loop, int index) {
            switch(loop) {
                case ManagedLoops.FixedUpdate:
                    fixedUpdateCount = index;
                    break;
                case ManagedLoops.Update:
                    updateCount = index;
                    break;
                case ManagedLoops.LateUpdate:
                    lateUpdateCount = index;
                    break;
                case ManagedLoops.FixedUpdate | ManagedLoops.Update:
                    break;
            }
        }

        private static int GetCount(ManagedLoops loop) {
            switch(loop) {
                case ManagedLoops.FixedUpdate: return fixedUpdateCount;
                case ManagedLoops.Update:      return updateCount;
                case ManagedLoops.LateUpdate:  return lateUpdateCount;
                default: return -1;
            };
        }

        public void Awake() {
            if(Local != null) {
                UnityEngine.Object.Destroy(this);
            } else {
                Local = this;
            }
        }

        public void FixedUpdate() {
            fixedFrameCount++;
            List<NetworkBehaviour> fixedUpdateLoop = FixedUpdateLoop;
            for(int i = 0; i < fixedUpdateCount; i++) {
                try {
                    fixedUpdateLoop[i].ManagedFixedUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(fixedUpdateLoop[i], $"Exception in FixedUpdate Loop: {arg}");
                }
            }
        }

        public void Update() {
            frameCount = Time.frameCount;
            List<NetworkBehaviour> updateLoop = UpdateLoop;
            for(int i = 0; i < updateCount; i++) {
                try {
                    updateLoop[i].ManagedUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(updateLoop[i], $"Exception in Update Loop: {arg}");
                }
            }
        }

        public void LateUpdate() {
            List<NetworkBehaviour> lateUpdateLoop = LateUpdateLoop;
            for(int i = 0; i < lateUpdateCount; i++) {
                try {
                    lateUpdateLoop[i].ManagedLateUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(lateUpdateLoop[i], $"Exception in LateUpdate Loop: {arg}");
                }
            }
        }
    }
}
