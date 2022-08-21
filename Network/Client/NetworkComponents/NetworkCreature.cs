using AMP.Data;
using AMP.Logging;
using AMP.Network.Data.Sync;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    public class NetworkCreature : ThunderBehaviour {

        protected static float MOVEMENT_TIME = 1.05f; // 1.05 to compensate for lag

        protected Creature creature;

        public Vector3 targetPos;

        private Vector3 _velocity;
        public Vector3 velocity {
            get { return _velocity; }
            set {
                _velocity = value;
                //speed = Mathf.Max(5f, _velocity.magnitude);
            }
        }
        //public float speed = 5f;
        private Vector3 currentVelocity;

        void Awake () {
            OnAwake();
        }

        protected void OnAwake() {
            creature = GetComponent<Creature>();

            creature.locomotion.rb.drag = 0;
            creature.locomotion.rb.angularDrag = 0;
        }

        protected override ManagedLoops ManagedLoops => ManagedLoops.FixedUpdate | ManagedLoops.Update;

        protected override void ManagedFixedUpdate() {
            if(!creature.enabled) UpdateLocomotionAnimation();
        }

        protected override void ManagedUpdate() {
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, MOVEMENT_TIME / Config.TICK_RATE); 

            creature.locomotion.rb.velocity = velocity;
            creature.locomotion.velocity = velocity;
        }

        private void UpdateLocomotionAnimation() {
            if(creature.currentLocomotion.isGrounded && creature.currentLocomotion.horizontalSpeed + Mathf.Abs(creature.currentLocomotion.angularSpeed) > creature.stationaryVelocityThreshold) {
                Vector3 vector = creature.transform.InverseTransformDirection(creature.currentLocomotion.velocity);
                creature.animator.SetFloat(Creature.hashStrafe, vector.x * (1f / creature.transform.lossyScale.x), creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashTurn, creature.currentLocomotion.angularSpeed * (1f / creature.transform.lossyScale.y) * creature.turnAnimSpeed, creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashSpeed, vector.z * (1f / creature.transform.lossyScale.z), creature.animationDampTime, Time.fixedDeltaTime);
            } else {
                creature.animator.SetFloat(Creature.hashStrafe, 0f, creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashTurn, 0f, creature.animationDampTime, Time.fixedDeltaTime);
                creature.animator.SetFloat(Creature.hashSpeed, 0f, creature.animationDampTime, Time.fixedDeltaTime);
            }
        }

        protected override void ManagedOnDisable() {
            Destroy(this);
        }
    }
}
