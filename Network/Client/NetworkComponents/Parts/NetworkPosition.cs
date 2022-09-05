using AMP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    public class NetworkPosition : ThunderBehaviour {
        protected static float MOVEMENT_TIME = 1.05f; // 1.05 to compensate for lag
        protected static float MOVEMENT_DELTA_TIME {
            get { return MOVEMENT_TIME / Config.TICK_RATE; }
        }

        public Vector3 targetPos;
        protected Vector3 positionVelocity;

        protected override ManagedLoops ManagedLoops => ManagedLoops.Update;

        protected override void ManagedUpdate() {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, MOVEMENT_DELTA_TIME);
        }

        public virtual bool IsSending() { return false; }
    }
}
