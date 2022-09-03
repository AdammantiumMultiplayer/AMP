using AMP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents.Parts {
    public class NetworkPositionRotation : NetworkPosition {
        public Quaternion targetRot;

        protected override void ManagedUpdate() {
            if(IsOwning()) return;
            base.ManagedUpdate();

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6);
        }
    }
}
