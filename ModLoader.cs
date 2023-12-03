using AMP.GameInteraction;
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
        [ModOption("Show Old Menu", saveValue = true, defaultValueIndex = 1)]
        public static void ShowOldMenu(bool show) {
            _ShowOldMenu = show;

            ModManager.instance?.UpdateOnScreenMenu();
        }



        [ModOptionCategory("Display", 1)]
        [ModOptionOrder(3)]
        [ModOption("Player Nametag", saveValue = true, defaultValueIndex = 1)]
        public static void ShowPlayerNames(bool show) {
            _ShowPlayerNames = show;

            HealthbarObject.UpdateAll();
        }

        [ModOptionCategory("Display", 1)]
        [ModOptionOrder(4)]
        [ModOption("Player Healthbar", saveValue = true, defaultValueIndex = 1)]
        public static void ShowPlayerHealthBars(bool show) {
            _ShowPlayerHealthBars = show;

            HealthbarObject.UpdateAll();
        }



        [ModOptionCategory("Performance", 2)]
        [ModOptionOrder(5)]
        [ModOptionTooltip("Toggles the clientside prediction to reduce latency but requires more performance.")]
        [ModOption("Clientside Prediction", saveValue = true, defaultValueIndex = 1)]
        public static bool ClientsidePrediction = true;





        internal static bool _ShowMenu = false;
        internal static bool _ShowOldMenu = false;

        internal static bool _ShowPlayerNames = true;
        internal static bool _ShowPlayerHealthBars = true;

        public override void ScriptLoaded(ThunderRoad.ModManager.ModData modData) {
            new GameObject().AddComponent<ModManager>();
        }
    }
}
