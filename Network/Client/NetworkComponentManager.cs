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

        // Copies of the loops as plain arrays, enumerating the ConcurrentDictionary (with LINQ) every frame allocates and is slow.
        // They get rebuilt when a loop changed (behaviour added / removed or tick rate changed).
        private static int version = 0; // Changed through Interlocked, the loops can get changed from the network thread
        private class LoopCache {
            public int version = -1;
            public KeyValuePair<NetworkBehaviour, int>[] entries = new KeyValuePair<NetworkBehaviour, int>[0];
        }
        private static readonly LoopCache fixedUpdateCache = new LoopCache();
        private static readonly LoopCache updateCache      = new LoopCache();
        private static readonly LoopCache lateUpdateCache  = new LoopCache();

        private static KeyValuePair<NetworkBehaviour, int>[] GetEntries(ConcurrentDictionary<NetworkBehaviour, int> loop, LoopCache cache) {
            int current = System.Threading.Volatile.Read(ref version);
            if(cache.version != current) {
                cache.entries = loop.ToArray();
                cache.version = current;
            }
            return cache.entries;
        }

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
            int current;
            if(dict.TryGetValue(behaviour, out current) && current == modulo) return; // Nothing changed, keep the cache
            dict[behaviour] = modulo;
            System.Threading.Interlocked.Increment(ref version);
        }

        public static int GetTickRate(NetworkBehaviour behaviour, ManagedLoops loop) {
            var dict = GetLoop(loop);
            if(dict.ContainsKey(behaviour)) {
                return dict[behaviour];
            }
            return -1;
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
                if(GetLoop(loop).TryAdd(behaviour, 1)) System.Threading.Interlocked.Increment(ref version);
            }
        }

        private static void RemoveFromLoop(NetworkBehaviour behaviour, ManagedLoops loop) {
            if(GetLoop(loop).TryRemove(behaviour, out _)) System.Threading.Interlocked.Increment(ref version);
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

            foreach(KeyValuePair<NetworkBehaviour, int> networkBehaviour in GetEntries(FixedUpdateLoop, fixedUpdateCache)) {
                if(fixedFrameCount % networkBehaviour.Value != 0) continue;
                try {
                    networkBehaviour.Key.ManagedFixedUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(networkBehaviour.Key, $"Exception in Fixed Update Loop: {arg}");
                }
            }
        }

        public void Update() {
            frameCount = Time.frameCount;

            foreach(KeyValuePair<NetworkBehaviour, int> networkBehaviour in GetEntries(UpdateLoop, updateCache)) {
                if(frameCount % networkBehaviour.Value != 0) continue;
                try {
                    networkBehaviour.Key.ManagedUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(networkBehaviour.Key, $"Exception in Update Loop: {arg}");
                }
            }
        }

        public void LateUpdate() {
            foreach(KeyValuePair<NetworkBehaviour, int> networkBehaviour in GetEntries(LateUpdateLoop, lateUpdateCache)) {
                if(frameCount % networkBehaviour.Value != 0) continue;
                try {
                    networkBehaviour.Key.ManagedLateUpdate();
                } catch(Exception arg) {
                    Debug.LogErrorFormat(networkBehaviour.Key, $"Exception in Late Update Loop: {arg}");
                }
            }
        }
    }
}
