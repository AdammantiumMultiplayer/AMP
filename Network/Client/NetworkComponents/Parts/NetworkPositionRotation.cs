using AMP.Data;
using AMP.Extension;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    internal class NetworkPositionRotation : NetworkPosition {
        internal Quaternion targetRot;
        internal Vector3 rotationVelocity;

        public override void ManagedUpdate() {
            base.ManagedUpdate();

            transform.rotation = transform.rotation.SmoothDamp(targetRot, ref rotationVelocity, Config.MOVEMENT_DELTA_TIME);
        }
    }
}
