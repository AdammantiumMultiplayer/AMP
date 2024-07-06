using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    internal class NetworkPosition : NetworkBehaviour {
        internal Vector3 targetPos;
        internal Vector3 positionVelocity;

        internal Rigidbody bodyToUpdate = null;
        internal CharacterController characterController;

        internal float SMOOTHING_TIME {
            get { return Config.MOVEMENT_DELTA_TIME; }
        }

        public override void ManagedUpdate() {
            if(characterController != null) characterController.Move(Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, SMOOTHING_TIME) - transform.position);
            else if(bodyToUpdate != null) bodyToUpdate.position = Vector3.SmoothDamp(bodyToUpdate.position, targetPos, ref positionVelocity, SMOOTHING_TIME);
            else transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, SMOOTHING_TIME);
        }

        internal virtual bool IsSending() { return false; }
    }
}
