using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace AMP.GameInteraction {
    internal class LevelFunc {
        public static void EnableRespawning() {
            SetRespawning(true);
        }

        public static void DisableRespawning() {
            SetRespawning(false);
        }

        private static void SetRespawning(bool allow) {
            foreach(LevelModule lm in Level.current.mode.modules) {
                if(lm is LevelModuleDeath) {
                    ((LevelModuleDeath)lm).behaviour = (allow ? LevelModuleDeath.Behaviour.Respawn : LevelModuleDeath.Behaviour.ReloadLevel);
                }
            }
        }
    }
}
