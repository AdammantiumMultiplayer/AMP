using AMP.GameInteraction;
using AMP.Logging;
using AMP.UI;
using Netamite.Unity.Voice;
using ThunderRoad;
using UnityEngine;
using UnityEngine.UI;

namespace AMP {
    public class ModLoader : ThunderScript {
        

        [ModOptionCategory("Display", 1)]
        [ModOptionOrder(3)]
        [ModOption("Player Nametag", saveValue = true)]
        public static void ShowPlayerNames(bool show = true) {
            _ShowPlayerNames = show;

            HealthbarObject.UpdateAll();
        }
        
        [ModOptionCategory("Display", 1)]
        [ModOptionOrder(4)]
        [ModOption("Player Healthbar", saveValue = true)]
        public static void ShowPlayerHealthBars(bool show = true) {
            _ShowPlayerHealthBars = show;

            HealthbarObject.UpdateAll();
        }
        
        
        
        [ModOptionCategory("Performance", 2)]
        [ModOptionOrder(5)]
        [ModOptionTooltip("Toggles the clientside prediction to reduce latency but requires more performance.")]
        [ModOption("Clientside Prediction", saveValue = true)]
        public static bool ClientsidePrediction = false;


        [ModOptionCategory("Voice Chat [Powered by SIPSorcery]", 3)]
        [ModOptionOrder(10)]
        [ModOptionTooltip("Toggles the ingame voice chat.")]
        [ModOption("Enable VoiceChat", saveValue = true)]
        public static void EnableVoiceChat(bool enable = false) {
            _EnableVoiceChat = enable;
            
            ModManager.safeFile.hostingSettings.allowVoiceChat = enable;

            if(ModManager.clientSync != null) {
                ModManager.clientSync.UpdateVoiceChatState();
            }
        }

        [ModOptionCategory("Voice Chat [Powered by SIPSorcery]", 3)]
        [ModOptionOrder(11)]
        [ModOptionTooltip("Toggles if chat is proxmity based or always on.")]
        [ModOption("Proximity Chat", saveValue = true)]
        public static void EnableProximityChat(bool enable = false) {
            _EnableProximityChat = enable;

            if (ModManager.clientSync != null) {
                ModManager.clientSync.StartCoroutine(ModManager.clientSync.UpdateProximityChat());
            }
        }

        [ModOptionCategory("Voice Chat [Powered by SIPSorcery]", 3)]
        [ModOptionOrder(12)]
        [ModOptionTooltip("Set the audio volume for voice chat.")]
        [ModOption("Voice Volume", saveValue = true, valueSourceName = "CutoffRange")]
        [ModOptionSlider(interactionType = ModOption.InteractionType.Slider)]
        public static void SetVolume(float volume = 1.0f) {
            _VoiceChatVolume = volume;

            if(ModManager.clientSync != null) {
                ModManager.clientSync.StartCoroutine(ModManager.clientSync.UpdateProximityChat());
            }
        }

        internal static string currentRecordingDevice = "";
        [ModOptionCategory("Voice Chat [Powered by SIPSorcery]", 3)]
        [ModOptionOrder(13)]
        [ModOptionTooltip("Set the recording device for voice chat.")]
        [ModOption("Microphone", saveValue = true, valueSourceName = "RecordingDevices")]
        public static void SetRecordingDevice(int deviceId = 0) {
            if(Microphone.devices.Length == 0) {
                Debug.LogWarning("[AMP] No microphone devices found!");
                return;
            }
            if(deviceId < 0 || deviceId >= Microphone.devices.Length) {
                Debug.LogWarning("[AMP] Invalid microphone device id!");
                return;
            }
            
            currentRecordingDevice = Microphone.devices[deviceId];
            ModManager.clientSync?.voiceClient?.SetInputDevice(Microphone.devices[deviceId]);
        }

        [ModOptionCategory("Voice Chat [Powered by SIPSorcery]", 3)]
        [ModOptionOrder(14)]
        [ModOptionTooltip("Sets the minimum volume to ignore background noises. Lower values mean that the microphone is more sensitive.")]
        [ModOption("Microphone Sensitivity", saveValue = true, valueSourceName = "CutoffRange")]
        [ModOptionSlider(interactionType = ModOption.InteractionType.Slider)]
        public static void SetMinimumVolume(float val = 0.15f) {
            _RecordingCutoffVolume = val;
            
            ModManager.clientSync?.voiceClient?.SetRecordingThreshold(val);
        }
        
        [ModOptionCategory("Voice Chat [Powered by SIPSorcery]", 3)]
        [ModOptionOrder(15)]
        [ModOption("Test Microphone", saveValue = false)]
        [ModOptionButton()]
        public static void TestMicrophone(bool active) {
            if (active) {
                Transform parent = menu.contentArea.transform.Find("Options Scroll View").Find("Viewport").Find("Options Content").Find("Test Microphone").Find("Options Button").transform;
                
                GameObject gobj = new GameObject("bar");
                gobj.transform.SetParent(parent);
                microphoneTesterParent = gobj.AddComponent<RectTransform>();
                microphoneTesterParent.localScale = Vector2.one;
                microphoneTesterParent.sizeDelta = new Vector2(500, 40);
                microphoneTesterParent.localPosition = new Vector3(0, -40, 0);
                microphoneTesterParent.localEulerAngles = Vector3.zero;
                Image img = gobj.AddComponent<Image>();
                img.color = Color.gray;
                
                gobj = new GameObject("fill");
                gobj.transform.SetParent(microphoneTesterParent.transform);
                microphoneTesterBar = gobj.AddComponent<RectTransform>();
                microphoneTesterBar.localScale = Vector2.one;
                microphoneTesterBar.sizeDelta = new Vector2(0, 40);
                microphoneTesterBar.localPosition = new Vector3(-250, 0, 0);
                microphoneTesterBar.localEulerAngles = Vector3.zero;
                microphoneTesterBarImage = gobj.AddComponent<Image>();
                microphoneTesterBarImage.color = Color.red;

                gobj = new GameObject("threshold");
                gobj.transform.SetParent(microphoneTesterParent.transform);
                microphoneTesterThreshold = gobj.AddComponent<RectTransform>();
                microphoneTesterThreshold.localScale = Vector2.one;
                microphoneTesterThreshold.sizeDelta = new Vector2(5, 40);
                microphoneTesterThreshold.localPosition = new Vector3(-250 + (_RecordingCutoffVolume * 500), 0, 0);
                microphoneTesterThreshold.localEulerAngles = Vector3.zero;
                img = gobj.AddComponent<Image>();
                img.color = Color.black;

                vr.SetDevice(currentRecordingDevice);
                vr.Start();
            } else {
                if (microphoneTesterParent != null) {
                    microphoneTesterThreshold = null;
                    microphoneTesterBar = null;
                    microphoneTesterBarImage = null;
                    GameObject.Destroy(microphoneTesterParent.gameObject);
                    microphoneTesterParent = null;
                }
                
                if (vr != null) {
                    vr.Stop();
                }
            }
        }


        internal static bool _ShowMenu = false;
        internal static bool _ShowOldMenu = false;

        internal static bool _ShowPlayerNames = true;
        internal static bool _ShowPlayerHealthBars = true;

        internal static bool _EnableVoiceChat = false;
        internal static bool _EnableProximityChat = false;
        internal static float _RecordingCutoffVolume = 0.04f;
        internal static float _VoiceChatVolume = 1f;

        internal static VoiceReader vr;
        
        private bool _setupMenu = false;
        private IngameModUI _ingameUI = null;
        private static UIModsMenu.ModMenu menu = null;
        private static RectTransform microphoneTesterParent;
        private static RectTransform microphoneTesterThreshold;
        private static RectTransform microphoneTesterBar;
        private static Image microphoneTesterBarImage;

        public override void ScriptLoaded(ThunderRoad.ModManager.ModData modData) {
            base.ScriptLoaded(modData);
            ModManager modManager = new GameObject().AddComponent<ModManager>();
            //parent it under the B&S gamemanager so it doesnt  get destroyed
            modManager.transform.SetParent(ThunderRoad.GameManager.local.transform);
            
            GameObject obj = new GameObject("VoiceReader");
            vr = obj.AddComponent<VoiceReader>();
            vr.Initialize(null);
            vr.CalculateTotalVolumePeak = true;
            vr.Stop();
            
            UIModsMenu.OnModMenuOpened += OnModMenuOpened;
            UIModsMenu.OnModMenuClosed += OnModMenuClosed;
            EventManager.OnToggleOptionsMenu += OnToggleOptionsMenu;
        }


        void OnToggleOptionsMenu(bool isVisible) {
            if(isVisible) {
                if (_ingameUI) {
                    _ingameUI.UpdateConnectionScreen();
                }
            }
        }
        private void OnModMenuClosed(UIModsMenu.ModMenu menu) {
            if(menu.modData != ModData) return;
            vr.Stop();
        }
        
        private void OnModMenuOpened(UIModsMenu.ModMenu menu) {
            if(menu.modData != ModData) return;

            ModLoader.menu = menu;

            if (!_setupMenu || _ingameUI == null) {
                //get the modDatas menu
                var contentArea = menu.contentArea.OptionsListGroup;
                var contentAreaRect = contentArea.GetComponent<RectTransform>();
                //under this rectTransform, add a new gameobject, that has the IngameModUI component
                GameObject ingameUIObj = new GameObject("IngameUI", typeof(RectTransform));
                ingameUIObj.transform.SetParent(contentAreaRect);
                ingameUIObj.transform.SetSiblingIndex(0);
                Debug.Log($"Added gameobject for server browser to mod options menu");
                _ingameUI = ingameUIObj.AddComponent<IngameModUI>();
                Debug.Log($"Added IngameModUI to mod options menu");
                
                _setupMenu = true;
            } else {
                if(_ingameUI)
                    _ingameUI.UpdateConnectionScreen();
            }
        }


        public static ModOptionInt[] RecordingDevices() {
            ModOptionInt[] modOptionIntArray = new ModOptionInt[Microphone.devices.Length];
            for (int index = 0; index < modOptionIntArray.Length; ++index)
                modOptionIntArray[index] = new ModOptionInt(Microphone.devices[index], index);
            return modOptionIntArray;
        }

        public static ModOptionFloat[] CutoffRange() {
            ModOptionFloat[] vals = new ModOptionFloat[101];
            for(int i = 0; i < vals.Length; i++) {
                float num = i * 0.01f;
                vals[i] = new ModOptionFloat(num.ToString("0.00"), num);
            }
            return vals;
        }

        override public void ScriptUpdate() {
            //if(vr.currentVolume > 0) Log.Warn(vr.currentVolume);
            if(microphoneTesterBar != null) {
                microphoneTesterBarImage.color = vr.currentVolume > _RecordingCutoffVolume ? new Color(0.64f, 1, 0.47f) : new Color(0.98f, 0.33f, 0.17f);

                float size = vr.currentVolume * 500;
                microphoneTesterBar.localPosition = new Vector3(-250 + (size / 2), 0, 0);
                microphoneTesterBar.sizeDelta = new Vector2(size, 40);
            }
        }
    }
}
