using AMP.Logging;
using AMP.Unsafe.Patching;
using System.Reflection;
using ThunderRoad;

namespace AMP.Unsafe {
    internal class Patcher {

        public static void PatchIt() {
            /*
            MethodBase originalMethod = typeof(Creature).GetProperty(
                "state", // Name of the method
                BindingFlags.Instance | BindingFlags.Public
            ).GetMethod;

            MethodBase replaceMethod = typeof(CreaturePatch).GetProperty(
                "patched_state", // Name of the method
                BindingFlags.Instance | BindingFlags.Public
            ).GetMethod;
            */

            MethodBase originalMethod = typeof(Creature).GetMethod(
                "IsEnemy", // Name of the method
                BindingFlags.Instance | BindingFlags.Public
            );

            MethodBase replaceMethod = typeof(CreaturePatch).GetMethod(
                "IsEnemy", // Name of the method
                BindingFlags.Instance | BindingFlags.Public
            );

            Reflection.ReplaceMethod(originalMethod, replaceMethod);

            Log.Debug("PATCHED");
        }
    }
}
