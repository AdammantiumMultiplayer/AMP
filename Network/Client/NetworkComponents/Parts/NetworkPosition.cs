using AMP.Data;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    internal class NetworkPosition : NetworkBehaviour {
        internal Vector3 targetPos;
        internal Vector3 positionVelocity;

        internal Rigidbody bodyToUpdate = null;

        public override void ManagedUpdate() {
            if(bodyToUpdate == null ) transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, Config.MOVEMENT_DELTA_TIME);
            else bodyToUpdate.position = Vector3.SmoothDamp(bodyToUpdate.position, targetPos, ref positionVelocity, Config.MOVEMENT_DELTA_TIME);
        }

        internal virtual bool IsSending() { return false; }
    }
}
