using AMP.Logging;
using ThunderRoad;

namespace AMP.GameInteraction {
    internal class LevelFunc {

        public static void EnableRespawning() {
            SetRespawning(true);
        }

        public static void DisableRespawning() {
            SetRespawning(false);
        }

        internal static void SetRespawning(bool allow) {
            if(Level.master != null && Level.master.mode != null) {
                SetRespawning(allow, Level.master.mode);
            }

            if(Level.current != null && Level.current.mode != null) {
                SetRespawning(allow, Level.current.mode);
                /*
                LevelLossController levelLossController = Level.current.GetComponent<LevelLossController>();
                if(levelLossController.GetComponent<LevelLossController>() != null) {

                    LevelLossBehaviour levelLossBehaviour;
                    LevelLossBehaviour.loadedLossBehaviours.TryGetValue("Bas.Loss.Death", out levelLossBehaviour);
                    if(levelLossBehaviour != null) {
                        foreach(LevelLossBehaviour.Step step in levelLossBehaviour.actionSteps) {
                            Log.Debug(step.action + " " + step.parameter);
                        }
                    }
                }
                */
            }

        }

        internal static void SetRespawning(bool allow, LevelData.Mode currentMode) {
            if(currentMode == null) return;

            if(currentMode.HasModule<LevelModuleDeath>()) {
                LevelModuleDeath moduleDeath = currentMode.GetModule<LevelModuleDeath>();
                moduleDeath.behaviour = (allow ? LevelModuleDeath.Behaviour.Respawn : LevelModuleDeath.Behaviour.ShowDeathMenu);
            }
        }
    }
}
