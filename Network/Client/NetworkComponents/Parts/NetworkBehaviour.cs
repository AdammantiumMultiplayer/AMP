using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    internal class NetworkBehaviour : MonoBehaviour {

        public virtual ManagedLoops EnabledManagedLoops => ManagedLoops.Update;

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
    }
}
