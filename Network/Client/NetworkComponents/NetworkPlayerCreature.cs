using AMP.Data;
using AMP.Logging;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    public class NetworkPlayerCreature : NetworkCreature {

        public Transform handLeftTarget;
        public Transform handRightTarget;
        public Transform headTarget;

        private Vector3 handLeftPos;
        private Quaternion handLeftRot;
        public Quaternion handLeftTargetRot;
        public Vector3 handLeftTargetPos;
        private Vector3 handLeftTargetVel;

        private Vector3 handRightPos;
        private Quaternion handRightRot;
        public Quaternion handRightTargetRot;
        public Vector3 handRightTargetPos;
        private Vector3 handRightTargetVel;

        private Vector3 headPos;
        private Quaternion headRot;
        public Quaternion headTargetRot;
        public Vector3 headTargetPos;
        private Vector3 headTargetVel;

        protected override void ManagedUpdate() {
            base.ManagedUpdate();

            // Rotations
            handLeftRot = Quaternion.Slerp(handLeftRot, handLeftTargetRot, Time.deltaTime * 3);
            handRightRot = Quaternion.Slerp(handRightRot, handRightTargetRot, Time.deltaTime * 3);
            headRot = Quaternion.Slerp(headRot, headTargetRot, Time.deltaTime * 3);

            handLeftTarget.rotation = handLeftRot;
            handRightTarget.rotation = handRightRot;
            headTarget.rotation = headRot;


            // Positions
            handLeftPos = Vector3.SmoothDamp(handLeftPos, handLeftTargetPos, ref handLeftTargetVel, MOVEMENT_TIME / Config.TICK_RATE);
            handRightPos = Vector3.SmoothDamp(handRightPos, handRightTargetPos, ref handRightTargetVel, MOVEMENT_TIME / Config.TICK_RATE);
            headPos = Vector3.SmoothDamp(headPos, headTargetPos, ref headTargetVel, MOVEMENT_TIME / Config.TICK_RATE);
            
            handLeftTarget.position = transform.position + handLeftPos;
            handRightTarget.position = transform.position + handRightPos;
            headTarget.position = headPos;
            headTarget.Translate(Vector3.forward);


            creature.lastInteractionTime = Time.time - 1;
            creature.spawnTime = Time.time - 1;
        }

    }
}
