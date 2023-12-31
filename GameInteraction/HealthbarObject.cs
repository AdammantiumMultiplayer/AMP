using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AMP.GameInteraction {
    internal class HealthbarObject : MonoBehaviour {

        private int clientId = 0;
        private string displayName = "Unnamed";
        private float health = 1;

        private Image healthBar;
        private TMP_Text nameTag;

        private bool showHealthBar = true;
        private bool showNameTag = true;

        private static Dictionary<int, bool> showHealthBarDict = new Dictionary<int, bool>();
        private static Dictionary<int, bool> showNameTagDict = new Dictionary<int, bool>();

        private static Sprite empty;

        private static bool GetHealthBarVisibility(int clientId) {
            if(showHealthBarDict.ContainsKey(clientId)) {
                return showHealthBarDict[clientId];
            }
            if(showHealthBarDict.ContainsKey(-1)) {
                return showHealthBarDict[-1];
            }

            return ModLoader._ShowPlayerHealthBars;
        }

        private static bool GetNameTagVisibility(int clientId) {
            if(showNameTagDict.ContainsKey(clientId)) {
                return showNameTagDict[clientId];
            }
            if(showNameTagDict.ContainsKey(-1)) {
                return showNameTagDict[-1];
            }

            return ModLoader._ShowPlayerNames;
        }

        void Start() {
            if(empty == null) {
                Texture2D tex2d = new Texture2D(1, 1);
                tex2d.SetPixel(0, 0, Color.white);
                empty = Sprite.Create(tex2d, new Rect(0, 0, 1, 1), Vector2.zero);
            }

            NetworkPlayerCreature npc = GetComponentInParent<NetworkPlayerCreature>();
            if(npc != null) {
                clientId = npc.playerNetworkData.clientId;
            }
            
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 300);
            gameObject.transform.localScale = Vector3.one * 0.001f;

            GameObject gobj = new GameObject("HealthBarParent");
            gobj.transform.parent = transform;
            healthBar = gobj.AddComponent<Image>();
            healthBar.transform.localPosition = Vector3.zero;
            healthBar.transform.localScale = Vector3.one;
            healthBar.color = Color.red;
            healthBar.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 25);
            healthBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);

            gobj = new GameObject("HealthBar");
            gobj.transform.parent = healthBar.transform;
            healthBar = gobj.AddComponent<Image>();
            healthBar.transform.localPosition = Vector3.zero;
            healthBar.transform.localScale = Vector3.one;
            healthBar.color = Color.green;
            healthBar.type = Image.Type.Filled;
            healthBar.fillMethod = Image.FillMethod.Horizontal;
            healthBar.sprite = empty;
            healthBar.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 25);
            healthBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            healthBar.fillAmount = health;

            gobj = new GameObject("NameTag");
            gobj.transform.parent = transform;
            nameTag = gobj.AddComponent<TextMeshPro>();
            nameTag.transform.localPosition = Vector3.zero;
            nameTag.transform.localScale = Vector3.one;
            nameTag.enableAutoSizing = true;
            nameTag.fontSizeMax = 1500;
            nameTag.fontSizeMin = 500;
            nameTag.alignment = TextAlignmentOptions.Center;
            nameTag.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 150);
            nameTag.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 75);

            UpdateDisplay();
            SetText(displayName);
        }

        public void SetText(string displayName) {
            this.displayName = displayName;
            if(nameTag != null) nameTag.text = this.displayName;
        }

        public void SetHealth(float health) {
            this.health = health;
            if(showHealthBar) {
                StopCoroutine(UpdateHealth());
                StartCoroutine(UpdateHealth());
            } else {
                healthBar.fillAmount = health;
            }
        }

        private float speed = 0f;
        private IEnumerator UpdateHealth() {
            while(healthBar.fillAmount != health) {
                healthBar.fillAmount = Mathf.SmoothDamp(healthBar.fillAmount, health, ref speed, 0.5f);
                yield return new WaitForEndOfFrame();
            }
            healthBar.fillAmount = health;
        }

        public static void SetHealthBarVisible(int clientId, bool visible) {
            if(showHealthBarDict.ContainsKey(clientId))
                showHealthBarDict[clientId] = visible;
            else
                showHealthBarDict.Add(clientId, visible);

            UpdateAll();
        }

        public static void SetNameTagVisible(int clientId, bool visible) {
            if(showNameTagDict.ContainsKey(clientId))
                showNameTagDict[clientId] = visible;
            else
                showNameTagDict.Add(clientId, visible);

            UpdateAll();
        }

        public static void UpdateAll() {
            if(ModManager.clientSync == null) return;
            foreach(PlayerNetworkData pnd in ModManager.clientSync.syncData.players.Values) {
                if(pnd.networkCreature != null && pnd.networkCreature.healthBar != null) {
                    pnd.networkCreature.healthBar.UpdateDisplay();
                }
            }
        }

        public void SetHealthBarVisible(bool visible) {
            if(showHealthBarDict.ContainsKey(clientId))
                showHealthBarDict[clientId] = visible;
            else
                showHealthBarDict.Add(clientId, visible);

            UpdateDisplay();
        }
        public void SetNameVisible(bool visible) {
            if(showNameTagDict.ContainsKey(clientId))
                showNameTagDict[clientId] = visible;
            else
                showNameTagDict.Add(clientId, visible);

            UpdateDisplay();
        }

        private void RefreshValues() {
            showHealthBar = GetHealthBarVisibility(clientId);
            showNameTag = GetNameTagVisibility(clientId);
        }

        private void UpdateDisplay() {
            RefreshValues();

            if(healthBar != null) healthBar.transform.parent.gameObject.SetActive(showHealthBar);
            if(nameTag != null) nameTag.gameObject.SetActive(showNameTag);
        }
    }
}
