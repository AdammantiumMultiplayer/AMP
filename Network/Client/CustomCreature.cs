using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    public class CustomCreature : ThunderBehaviour {

        Creature creature;

        public bool isPlayer = false;

        void Awake () {
            creature = GetComponent<Creature>();

            creature.locomotion.rb.drag = 0;
            creature.locomotion.rb.angularDrag = 0;
        }

        protected override ManagedLoops ManagedLoops => ManagedLoops.FixedUpdate;

        protected override void ManagedFixedUpdate() {
            UpdateLocomotionAnimation();
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
