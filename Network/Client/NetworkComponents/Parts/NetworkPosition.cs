using AMP.Data;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    internal class NetworkPosition : NetworkBehaviour {
        internal Vector3 targetPos;
        internal Vector3 positionVelocity;

        public override void ManagedUpdate() {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, Config.MOVEMENT_DELTA_TIME);
        }

        internal virtual bool IsSending() { return false; }
    }
}
