using AMP.GameInteraction;
using Netamite.Voice;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.PlayerControl;

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



        [ModOptionCategory("Voice", 3)]
        [ModOptionTooltip("Toggles the ingame voice chat.")]
        [ModOption("Enable VoiceChat", saveValue = true, defaultValueIndex = 0)]
        public static void EnableVoiceChat(bool enable) {
            _EnableVoiceChat = enable;

            if(ModManager.clientSync != null) {
                ModManager.clientSync.UpdateVoiceChatState();
            }
        }

        [ModOptionCategory("Voice", 3)]
        [ModOptionTooltip("Set the recording device for voice chat.")]
        [ModOption("Microphone", saveValue = true, defaultValueIndex = 0, valueSourceName = "RecordingDevices")]
        public static void SetRecordingDevice(int deviceId) {
            _RecordingDevice = deviceId;

            ModManager.clientSync?.voiceClient?.SetInputDevice(deviceId);
        }

        [ModOptionCategory("Voice", 3)]
        [ModOptionTooltip("Sets the minimum volume to ignore background noises.")]
        [ModOption("Minimum volume", saveValue = true, defaultValueIndex = 4, valueSourceName = "CutoffRange")]
        public static void SetMinimumVolume(float val) {
            _RecordingCutoffVolume = val;

            ModManager.clientSync?.voiceClient?.SetRecordingThreshold(val);
        }

        /*
        [ModOptionCategory("Voice", 3)]
        [ModOptionTooltip("Toggles if chat is proxmity based or always on.")]
        [ModOption("Proximity Chat", saveValue = true, defaultValueIndex = 0)]
        public static void EnableProximityChat(bool enable) {
            _EnableProximityChat = enable;
        }
        */



        internal static bool _ShowMenu = false;
        internal static bool _ShowOldMenu = false;

        internal static bool _ShowPlayerNames = true;
        internal static bool _ShowPlayerHealthBars = true;

        internal static bool _EnableVoiceChat = false;
        internal static bool _EnableProximityChat = false;
        internal static int  _RecordingDevice = 0;
        internal static float _RecordingCutoffVolume = 0.04f;

        public override void ScriptLoaded(ThunderRoad.ModManager.ModData modData) {
            new GameObject().AddComponent<ModManager>();
        }


        public static ModOptionInt[] RecordingDevices() {
            Dictionary<int, string> devices = VoiceClient.GetInputDevices();

            ModOptionInt[] deviceOpt = new ModOptionInt[devices.Count];
            for(int i = 0; i < deviceOpt.Length; i++) {
                deviceOpt[i] = new ModOptionInt(devices[i], i);
            }

            return deviceOpt;
        }

        public static ModOptionFloat[] CutoffRange() {
            ModOptionFloat[] vals = new ModOptionFloat[101];
            for(int i = 0; i < vals.Length; i++) {
                float num = i * 0.01f;
                vals[i] = new ModOptionFloat(num.ToString("0.00"), num);
            }
            return vals;
        }
    }
}
