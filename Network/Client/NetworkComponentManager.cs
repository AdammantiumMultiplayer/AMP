using AMP.Network.Client.NetworkComponents.Parts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    internal class NetworkComponentManager : MonoBehaviour {
        public static NetworkComponentManager Local;

        public static int frameCount;
        public static int fixedFrameCount;

        public static ConcurrentDictionary<NetworkBehaviour, int> FixedUpdateLoop { get; } = new ConcurrentDictionary<NetworkBehaviour, int>();
        public static ConcurrentDictionary<NetworkBehaviour, int> UpdateLoop { get; }      = new ConcurrentDictionary<NetworkBehaviour, int>();
        public static ConcurrentDictionary<NetworkBehaviour, int> LateUpdateLoop { get; }  = new ConcurrentDictionary<NetworkBehaviour, int>();

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

        public static void SetTickRate(NetworkBehaviour behaviour, int modulo) {
            SetTickRate(behaviour, modulo, ManagedLoops.FixedUpdate);
            SetTickRate(behaviour, modulo, ManagedLoops.Update);
            SetTickRate(behaviour, modulo, ManagedLoops.LateUpdate);
        }

        public static void SetTickRate(NetworkBehaviour behaviour, int modulo, ManagedLoops loop) {
            if(modulo <= 0) modulo = 1;
            var dict = GetLoop(loop);
            if(dict.ContainsKey(behaviour)) {
                dict[behaviour] = modulo;
            } else {
                dict.TryAdd(behaviour, modulo);
            }
        }

        private static ConcurrentDictionary<NetworkBehaviour, int> GetLoop(ManagedLoops loop) {
            switch(loop) {
                case ManagedLoops.FixedUpdate: return FixedUpdateLoop;
                case ManagedLoops.Update:      return UpdateLoop;
                case ManagedLoops.LateUpdate:  return LateUpdateLoop;
                default: return null;
            };
        }

        private static void AddToLoop(NetworkBehaviour behaviour, ManagedLoops loop) {
            if(behaviour.EnabledManagedLoops.HasFlagNoGC(loop)) {
                GetLoop(loop).TryAdd(behaviour, 1);
            }
        }

        private static void RemoveFromLoop(NetworkBehaviour behaviour, ManagedLoops loop) {
            GetLoop(loop).TryRemove(behaviour, out _);
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
            foreach(KeyValuePair<NetworkBehaviour, int> networkBehaviour in FixedUpdateLoop.AsParallel().Where(nb => fixedFrameCount % nb.Value == 0)) {
                try {
                    networkBehaviour.Key.ManagedFixedUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(networkBehaviour.Key, $"Exception in Fixed Update Loop: {arg}");
                }
            }
        }

        public void Update() {
            frameCount = Time.frameCount;
            foreach(KeyValuePair<NetworkBehaviour, int> networkBehaviour in UpdateLoop.AsParallel().Where(nb => frameCount % nb.Value == 0)) {
                try {
                    networkBehaviour.Key.ManagedUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(networkBehaviour.Key, $"Exception in Update Loop: {arg}");
                }
            }
        }

        public void LateUpdate() {
            frameCount = Time.frameCount;
            foreach(KeyValuePair<NetworkBehaviour, int> networkBehaviour in LateUpdateLoop.AsParallel().Where(nb => frameCount % nb.Value == 0)) {
                try {
                    networkBehaviour.Key.ManagedLateUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(networkBehaviour.Key, $"Exception in Late Update Loop: {arg}");
                }
            }
        }
    }
}
