using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    internal class NetworkBehaviour : MonoBehaviour {

        public virtual ManagedLoops EnabledManagedLoops => ManagedLoops.Update;
        private int fixedIndex = -1;
        private int updateIndex = -1;
        private int lateUpdateIndex = -1;

        public void OnEnable() {
            NetworkComponentManager.AddBehaviour(this);
            ManagedOnEnable();
        }

        public void OnDisable() {
            NetworkComponentManager.RemoveBehaviour(this);
            ManagedOnDisable();
        }

        public virtual void ManagedOnEnable() {
        }

        public virtual void ManagedOnDisable() {
        }

        public virtual void ManagedFixedUpdate() {
        }

        public virtual void ManagedUpdate() {
        }

        public virtual void ManagedLateUpdate() {
        }

        internal void SetIndex(ManagedLoops loops, int index) {
            switch(loops) {
                case ManagedLoops.FixedUpdate:
                    fixedIndex = index;
                    break;
                case ManagedLoops.Update:
                    updateIndex = index;
                    break;
                case ManagedLoops.LateUpdate:
                    lateUpdateIndex = index;
                    break;
                case ManagedLoops.FixedUpdate | ManagedLoops.Update:
                    break;
            }
        }

        internal int GetIndex(ManagedLoops loops) {
            switch(loops) {
                case ManagedLoops.FixedUpdate: return fixedIndex;
                case ManagedLoops.Update: return updateIndex;
                case ManagedLoops.LateUpdate: return lateUpdateIndex;
                default: return -1;
            };
        }
    }
}
