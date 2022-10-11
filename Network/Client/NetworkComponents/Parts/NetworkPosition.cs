using AMP.Data;
using AMP.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    internal class NetworkPosition : ThunderBehaviour {
        internal Vector3 targetPos;
        protected Vector3 positionVelocity;

        protected override ManagedLoops ManagedLoops => ManagedLoops.Update;

        protected override void ManagedUpdate() {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, Config.MOVEMENT_DELTA_TIME);
        }

        internal virtual bool IsSending() { return false; }
    }
}
