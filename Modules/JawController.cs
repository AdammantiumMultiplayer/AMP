using System;
using ThunderRoad;
using UnityEngine;

namespace AMP.Modules {
    internal class JawController : MonoBehaviour {
        public Creature creature { get; private set; }

        [Range(0, 1)]
        public float targetWeight;
        public float currentWeight { get; private set; }
        
        // Weight per second the jaw moves towards targetWeight, <= 0 snaps instantly
        public float speed = 10f;
        // Defaults to creature.jawMaxRotation (0, -30, 0 unless the prefab overrides it)
        public Vector3 maxRotation;
        
        private Transform jawAnimBone;
        private Quaternion jawOrgLocalRotation;
        private BrainModuleSpeak speak;
        private bool speakMovedMouth;

        public static JawController Get(Creature creature) {
            JawController jaw = creature.gameObject.GetComponent<JawController>();
            if (jaw != null) return jaw;

            if (creature.jaw == null) {
                Debug.LogWarning($"[FPS] {creature.name} has no jaw");
                return null;
            }
            Ragdoll.Bone bone = creature.ragdoll.GetBone(creature.jaw);
            if (bone?.animation == null) {
                Debug.LogWarning($"[FPS] {creature.name} has no jaw animation bone");
                return null;
            }

            jaw = creature.gameObject.AddComponent<JawController>();
            jaw.creature = creature;
            jaw.maxRotation = creature.jawMaxRotation;
            jaw.jawAnimBone = bone.animation;

            // The speak module also writes the jaw in LateUpdate, turn it off so they don't fight
            jaw.speak = creature.brain?.instance?.GetModule<BrainModuleSpeak>(false);
            if (jaw.speak != null) {
                jaw.speakMovedMouth = jaw.speak.moveMouth;
                jaw.speak.moveMouth = false;
                jaw.jawOrgLocalRotation = jaw.speak.jawOrgLocalRotation;
            } else {
                jaw.jawOrgLocalRotation = bone.animation.localRotation;
            }
            return jaw;
        }

        public static void Release(Creature creature) {
            JawController jaw = creature.gameObject.GetComponent<JawController>();
            if (jaw != null) Destroy(jaw);
        }

        void LateUpdate() {
            if (jawAnimBone == null) return;

            currentWeight = speed <= 0 ? targetWeight : Mathf.MoveTowards(currentWeight, targetWeight, Time.deltaTime * speed);
            jawAnimBone.localRotation = Quaternion.Lerp(jawOrgLocalRotation, jawOrgLocalRotation * Quaternion.Euler(maxRotation), currentWeight);
        }
        
        void OnDestroy() {
            if (jawAnimBone != null) jawAnimBone.localRotation = jawOrgLocalRotation;
            if (speak != null) speak.moveMouth = speakMovedMouth;
        }
    }
}
