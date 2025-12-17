using AMP.Data;
using AMP.Logging;
using System;
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
            
            /*
            Log.Debug(Defines.AMP, $"Respawning set to {allow}");
            currentMode.playerDeathAction = allow ? LevelData.Mode.PlayerDeathAction.None : LevelData.Mode.PlayerDeathAction.AskReload;
            */
            /*
            if (currentMode.HasModule<LevelModuleDeath>()) {
                LevelModuleDeath moduleDeath = currentMode.GetModule<LevelModuleDeath>();
                moduleDeath.behaviour = (allow ? LevelModuleDeath.Behaviour.Respawn : LevelModuleDeath.Behaviour.ShowDeathMenu);
            }
            */
        }

        internal static void UpdateBookAvailability() {
            SetBookAvailability(ModManager.clientSync.syncData.enable_spawn_book, ModManager.clientSync.syncData.enable_item_book);
        }

        internal static void SetBookAvailability(bool enable_spawn_book, bool enable_item_book) {
            foreach(UIWaveSpawner component in UnityEngine.Object.FindObjectsOfType<UIWaveSpawner>())
                component.gameObject.SetActive(enable_spawn_book);

            foreach(UIItemSpawner component in UnityEngine.Object.FindObjectsOfType<UIItemSpawner>())
                component.gameObject.SetActive(enable_item_book);
        }

        internal static void DisableCleanup() {
            foreach(WaveSpawner ws in WaveSpawner.instances) {
                ws.cleanBodiesAndItemsOnWaveStart = false;
            }
        }

        internal static void Init() {
            EnableRespawning();
            DisableCleanup();
        }
    }
}
