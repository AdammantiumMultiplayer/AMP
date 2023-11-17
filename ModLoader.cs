using AMP.Logging;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class ModLoader : ThunderScript {

        [ModOptionCategory("UI", 0)]
        [ModOptionOrder(1)]
        [ModOptionTooltip("Toggles the serverlist UI.")]
        [ModOption("Show Ingame Menu", saveValue = false)]
        public static void ShowMenu(bool show) {
            _ShowMenu = show;

            ModManager.instance?.UpdateIngameMenu();
        }

        [ModOptionCategory("UI", 0)]
        [ModOptionOrder(2)]
        [ModOptionTooltip("Toggles the old in game window menu.")]
        [ModOption("Show Old Menu", saveValue = true)]
        public static void ShowOldMenu(bool show) {
            _ShowOldMenu = show;

            ModManager.instance?.UpdateOnScreenMenu();
        }

        internal static bool _ShowMenu = false;
        internal static bool _ShowOldMenu = false;

        public override void ScriptLoaded(ThunderRoad.ModManager.ModData modData) {
            new GameObject().AddComponent<ModManager>();
        }
    }
}
