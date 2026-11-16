#if AMP
using AMP.Overlay;
using ThunderRoad;
#if STEAM  
using Steamworks;
using Netamite.Steam.Server;
using Netamite.Steam.Integration;
using SteamClient = Netamite.Steam.Client.SteamClient;
#endif
using AMP.Data;
using AMP.Extension;
#endif
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using AMP.SupportFunctions;

using AMP.Logging;
using ThunderRoadVRKBSharedData;
using AMP.Network;
using AMP.Threading;
using AMP.Network.Data.Sync;
using AMP.Resources;
using AMP.GameInteraction;
using AMP.Network.Packets.Implementation;

namespace AMP.UI {
    // Shared look and building blocks: tab bars, colors, keys, keyboard, input fields, buttons
    public partial class IngameModUI {

        // Muted warm-neutral tints let the button artwork provide most of the color.
        internal static ColorBlock tabColor = new ColorBlock() {
            normalColor      = new Color(0.72f, 0.69f, 0.64f, 1f),
            highlightedColor = new Color(0.82f, 0.79f, 0.74f, 1f),
            pressedColor     = new Color(0.88f, 0.84f, 0.77f, 1f),
            selectedColor    = new Color(0.88f, 0.84f, 0.77f, 1f),
            disabledColor    = new Color(0.55f, 0.52f, 0.48f, 0.9f),
            colorMultiplier  = 1,
            fadeDuration     = 0.15f
        };

        // A stronger amber tint keeps the selected tab distinct from the neutral normal state.
        internal static ColorBlock tabSelectedColor = new ColorBlock() {
            normalColor      = new Color(0.78f, 0.67f, 0.5f, 1f),
            highlightedColor = new Color(0.89f, 0.75f, 0.53f, 1f),
            pressedColor     = new Color(0.96f, 0.7f, 0.4f, 1f),
            selectedColor    = new Color(0.96f, 0.7f, 0.4f, 1f),
            disabledColor    = new Color(0.55f, 0.52f, 0.48f, 0.9f),
            colorMultiplier  = 1,
            fadeDuration     = 0.15f
        };

        // Dark brown text, reads well on the orange
        internal static Color tabTextColor = new Color(0.2f, 0.09f, 0.03f);

        // Sub tabs use softer warm neutrals so they remain visually secondary to the main tabs.
        // The selected one gets the golden tint of the selected main tab, connecting the two.
        static ColorBlock subTabColor = new ColorBlock() {
            normalColor      = new Color(0.72f, 0.69f, 0.64f, 1f),
            highlightedColor = new Color(0.82f, 0.79f, 0.74f, 1f),
            pressedColor     = new Color(0.88f, 0.84f, 0.77f, 1f),
            selectedColor    = new Color(0.88f, 0.84f, 0.77f, 1f),
            disabledColor    = new Color(0.55f, 0.52f, 0.48f, 0.9f),
            colorMultiplier  = 1,
            fadeDuration     = 0.15f
        };

        // Height of the tab bar, the frame of the tab background gets scaled to it
        const float tabHeight = 70f;
        const float subTabHeight = 45f;
        const int   subTabInset  = 160; // Space on the left and right of the sub tab bar, so it sits nested under the main tabs
        private HashSet<Toggle> subTabs = new HashSet<Toggle>();

        private ColorBlock GetTabColors(Toggle toggle, bool selected) {
            if(selected) return tabSelectedColor;
            return subTabs.Contains(toggle) ? subTabColor : tabColor;
        }

        // Tab of the second row, see CreateTab
        private Toggle CreateSubTab(RectTransform bar, ToggleGroup group, string name, string label, params Page[] pages) {
            Toggle toggle = CreateTab(bar, group, name, label, 30, pages);
            subTabs.Add(toggle);
            toggle.colors = subTabColor;
            toggle.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
            return toggle;
        }

        // Second tab bar, nested below the main tabs
        private RectTransform CreateSubTabBar(string name) {
            GameObject gobj = CreateObject(name);
            gobj.transform.SetParent(transform);
            RectTransform bar = gobj.AddComponent<RectTransform>();
            HorizontalLayoutGroup hlg = gobj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(subTabInset, subTabInset, 0, 0);
            bar.anchorMin = new Vector2(0, 1);
            bar.anchorMax = Vector2.one;
            bar.sizeDelta = new Vector2(0, subTabHeight);
            bar.localPosition = new Vector3(0, 325 - tabHeight / 2 - 8 - subTabHeight / 2, 0); // Just below the main tabs
            return bar;
        }

        private ToggleGroup tabGroup;
        private ToggleGroup joinTabGroup;
        private ToggleGroup hostTabGroup;
        private Dictionary<Toggle, Page[]> tabs = new Dictionary<Toggle, Page[]>();

        /// <param name="bar">Tab bar the tab gets added to</param>
        /// <param name="group">Toggle group of the tab bar</param>
        /// <param name="pages">Pages that belong to the tab, the first one gets shown when clicking it</param>
        private Toggle CreateTab(RectTransform bar, ToggleGroup group, string name, string label, float fontSize, params Page[] pages) {
            GameObject gobj = CreateObject(name);
            gobj.transform.SetParent(bar.transform);
            gobj.AddComponent<RectTransform>();
            Toggle toggle = gobj.AddComponent<Toggle>();
            toggle.targetGraphic = CreateTabBackground(gobj);
            toggle.colors = tabColor;
            toggle.isOn = false;
            toggle.group = group;
            toggle.onValueChanged.AddListener((value) => {
                if(value) {
                    if(!pages.Contains(currentPage)) ShowPage(pages[0]);
                } else if(!group.AnyTogglesOn() && pages.Contains(currentPage)) {
                    toggle.SetIsOnWithoutNotify(true); // Clicked the tab of the open page again, keep it selected
                }
                toggle.colors = GetTabColors(toggle, toggle.isOn);
            });

            GameObject text = CreateObject("Text");
            text.transform.SetParent(toggle.transform, false);
            // Stretch over the whole tab, so the text area always matches the tab size
            RectTransform textRect = text.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI tabText = text.AddComponent<TextMeshProUGUI>();
            tabText.text = label;
            tabText.color = tabTextColor;
            tabText.fontSize = fontSize;
            tabText.fontStyle = FontStyles.Bold;
            tabText.alignment = TextAlignmentOptions.Center;

            tabs.Add(toggle, pages);
            return toggle;
        }

        // Selects the tab the page belongs to, or none if it doesn't belong to one
        private void UpdateTabSelection(Page page) {
            foreach(KeyValuePair<Toggle, Page[]> tab in tabs) {
                bool selected = tab.Value.Contains(page);
                tab.Key.SetIsOnWithoutNotify(selected);
                tab.Key.colors = GetTabColors(tab.Key, selected); // The tint can only darken the texture, so switch the tints
            }
        }

        /// <summary>
        /// Scales the frame of the tab background to the height of the button, whenever its size changes.
        /// The side ornaments go over the full height of the image, so without it they get squashed or stretched.
        /// </summary>
        private class TabFrameScaler : UnityEngine.EventSystems.UIBehaviour {
            protected override void OnEnable() {
                base.OnEnable();
                UpdateScale();
            }

            protected override void OnRectTransformDimensionsChange() {
                base.OnRectTransformDimensionsChange();
                UpdateScale();
            }

            private void UpdateScale() {
                Image image = GetComponent<Image>();
                float height = ((RectTransform) transform).rect.height;
                if(image == null || image.sprite == null || height < 1f) return;

                float multiplier = image.sprite.rect.height / height;
                if(!Mathf.Approximately(image.pixelsPerUnitMultiplier, multiplier)) image.pixelsPerUnitMultiplier = multiplier;
            }
        }

        /// <summary>
        /// Plain colors are hard to read on the orange tab background, this returns a darker version of them
        /// </summary>
        private static Color ReadableOnTab(Color color) {
            if(color == Color.red)   return keyCancelColor;
            if(color == Color.green) return keyConfirmColor;
            if(color == Color.blue)  return new Color(0.08f, 0.18f, 0.55f);
            if(color == Color.black) return tabTextColor;
            return color;
        }

        private static Sprite tabBackground;
        /// <summary>
        /// Background for the menu tabs (AMPResources.button), 9-sliced so the frame doesn't get stretched, only the white middle
        /// </summary>
        private static Sprite TabBackground {
            get {
                if(tabBackground == null) tabBackground = LoadSlicedSprite(AMPResources.button, new Vector4(60, 9, 55, 9)); // The frame around the white middle
                return tabBackground;
            }
        }

        /// <summary>
        /// Loads an image as a sprite. The transparent rows above and below the visible part get cut off.
        /// </summary>
        /// <param name="border">9-slice border in pixels (left, bottom, right, top)</param>
        private static Sprite LoadSlicedSprite(byte[] data, Vector4 border) {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if(data == null || !tex.LoadImage(data)) {
                Log.Err(Defines.AMP, "Couldn't load a UI image.");
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp;

            // Find the visible rows
            Color32[] pixels = tex.GetPixels32();
            int minY = tex.height, maxY = -1;
            for(int y = 0; y < tex.height; y++) {
                for(int x = 0; x < tex.width; x++) {
                    if(pixels[y * tex.width + x].a > 16) {
                        if(y < minY) minY = y;
                        maxY = y;
                        break;
                    }
                }
            }
            if(maxY < minY) { minY = 0; maxY = tex.height - 1; } // Fully transparent, just use everything

            Rect rect = new Rect(0, minY, tex.width, maxY - minY + 1);
            return Sprite.Create(tex, rect, Vector2.one / 2, 100f, 0, SpriteMeshType.FullRect, border);
        }

        // Keys of the keypad and keyboard (AMPResources.keyboardButton). Its face is mid grey, so the tints are lighter than the tab ones to end up orange
        static ColorBlock keyColor = new ColorBlock() {
            normalColor      = new Color(1f,    0.75f, 0.45f, 1f),
            highlightedColor = new Color(1f,    0.85f, 0.6f,  1f),
            pressedColor     = new Color(1f,    0.95f, 0.75f, 1f),
            selectedColor    = new Color(1f,    0.75f, 0.45f, 1f), // Same as normal, a key shouldn't stay lit after typing
            disabledColor    = new Color(0.6f,  0.55f, 0.5f,  0.9f),
            colorMultiplier  = 1,
            fadeDuration     = 0.1f
        };
        static Color keyTextColor    = new Color(0.2f,  0.09f, 0.03f); // Same as the tabs
        static Color keyCancelColor  = new Color(0.65f, 0.05f, 0.05f); // Darker than plain red / green, so they read on the orange
        static Color keyConfirmColor = new Color(0.1f,  0.4f,  0.08f);

        private static Sprite keyBackground;
        private static Sprite KeyBackground {
            get {
                if(keyBackground == null) keyBackground = LoadSlicedSprite(AMPResources.keyboardButton, new Vector4(1, 1, 1, 1)); // 1 pixel bevel around the face
                return keyBackground;
            }
        }

        private static Image CreateKeyBackground(GameObject gobj) {
            Image image = gobj.AddComponent<Image>();
            image.sprite = KeyBackground;
            if(image.sprite != null) {
                image.type = Image.Type.Sliced; // Only the face stretches, the bevel stays thin on every key size
                image.pixelsPerUnitMultiplier = 0.4f; // Bevel about 2.5 units wide
                image.color = Color.white;
            } else {
                image.color = new Color(1, 1, 1, 0.6f); // Resource missing, look like before
            }
            return image;
        }

        internal static Image CreateTabBackground(GameObject gobj) {
            Image image = gobj.AddComponent<Image>();
            image.sprite = TabBackground;
            if(image.sprite != null) {
                image.type = Image.Type.Sliced; // Keeps the frame from getting stretched
                gobj.AddComponent<TabFrameScaler>(); // Scales the frame to the button height, otherwise the side ornaments get squashed
                image.color = Color.white;
            } else {
                image.color = new Color(1, 1, 1, 0.6f); // Resource missing, look like before
            }
            return image;
        }

        static ColorBlock buttonColor = new ColorBlock() {
            normalColor = new Color(1, 1, 1, 1f),
            highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f),
            pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f),
            selectedColor = new Color(1f, 0.749f, 0.345f, 1f),
            disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f),
            colorMultiplier = 1f,
            fadeDuration = 0.25f
        };
        
        private ScrollRect CreateScrollRect(string name, bool gridLayout) {
            GameObject gScrollRect = CreateObject(name);
            gScrollRect.transform.SetParent(transform);
            RectTransform rScrollRect = gScrollRect.AddComponent<RectTransform>();
            rScrollRect.anchorMin = Vector2.zero;
            rScrollRect.anchorMax = Vector2.one;
            rScrollRect.sizeDelta = new Vector2(-650, -80);
            rScrollRect.localPosition = new Vector3(-320f, -35, 0);
            ScrollRect scrollRect = gScrollRect.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject gViewport = CreateObject("ViewPort");
            gViewport.transform.SetParent(gScrollRect.transform);
            gViewport.AddComponent<Mask>().showMaskGraphic = false;
            gViewport.AddComponent<Image>();
            RectTransform rViewport = gViewport.GetComponent<RectTransform>();
            rViewport.anchorMin = Vector2.zero;
            rViewport.anchorMax = Vector2.one;
            rViewport.sizeDelta = new Vector2(0, 0);
            rViewport.localPosition = new Vector3(0, 0, 0);

            scrollRect.viewport = rViewport;


            GameObject gContent = CreateObject("Content");
            gContent.transform.SetParent(gViewport.transform);

            if(gridLayout) {
                GridLayoutGroup glg = gContent.AddComponent<GridLayoutGroup>();
                glg.cellSize = new Vector2(400, 75);
                glg.padding = new RectOffset(10, 10, 10, 10);
                glg.spacing = new Vector2(5, 5);
                glg.childAlignment = TextAnchor.UpperCenter;
            } else {
                VerticalLayoutGroup vlg = gContent.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = false;
                //vlg.padding = new RectOffset(10, 10, 10, 10);
                vlg.spacing = 2;
            }

            RectTransform rContent = gContent.GetComponent<RectTransform>();
            rContent.anchorMin = new Vector2(0, 1);
            rContent.anchorMax = Vector2.one;
            rContent.sizeDelta = new Vector2(0, 0);
            rContent.localPosition = new Vector3(0, 0, 0);
            rContent.pivot = new Vector2(0.5f, 1f);

            ContentSizeFitter csf = gContent.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = rContent;


            GameObject gScrollBarV = CreateObject("ScrollbarV");
            Scrollbar scrollBarV = gScrollBarV.AddComponent<Scrollbar>();
            scrollBarV.direction = Scrollbar.Direction.BottomToTop;
            gScrollBarV.transform.SetParent(gScrollRect.transform);
            RectTransform rScrollBarV = gScrollBarV.GetComponent<RectTransform>();
            rScrollBarV.anchorMin = new Vector2(1, 0);
            rScrollBarV.anchorMax = Vector2.one;
            rScrollBarV.pivot = new Vector2(1, 0.5f);
            rScrollBarV.anchoredPosition = new Vector2(10, 0);
            rScrollBarV.sizeDelta = new Vector2(8, 0);

            GameObject gScrollBarVSlidingArea = CreateObject("ScrollbarVSlidingArea");
            gScrollBarVSlidingArea.transform.SetParent(gScrollBarV.transform);
            RectTransform rScrollBarVSlidingArea = gScrollBarVSlidingArea.AddComponent<RectTransform>();
            rScrollBarVSlidingArea.anchorMin = Vector2.zero;
            rScrollBarVSlidingArea.anchorMax = Vector2.one;
            rScrollBarVSlidingArea.sizeDelta = Vector2.zero;
            rScrollBarVSlidingArea.anchoredPosition = Vector2.zero;


            GameObject gScrollBarVHandle = CreateObject("ScrollbarVHandle");
            gScrollBarVHandle.transform.SetParent(gScrollBarVSlidingArea.transform);
            RectTransform rScrollBarVHandle = gScrollBarVHandle.AddComponent<RectTransform>();
            rScrollBarVHandle.sizeDelta = new Vector2(0, 0);
            rScrollBarVHandle.anchorMin = Vector2.zero;
            rScrollBarVHandle.anchorMax = Vector2.one;
            rScrollBarVHandle.anchoredPosition = Vector2.zero;


            scrollBarV.targetGraphic = gScrollBarVHandle.AddComponent<Image>();
            scrollBarV.targetGraphic.color = Color.gray;

            scrollBarV.handleRect = gScrollBarVHandle.GetComponent<RectTransform>();

            scrollRect.verticalScrollbar = scrollBarV;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            return scrollRect;
        }
        // Label with a clickable field below it, laid out like the fields of the ip join
        private TextMeshProUGUI CreateInputField(RectTransform parent, string name, string label, float x, out Image field) {
            GameObject gobj = CreateObject(name + "InfoLabel");
            gobj.transform.SetParent(parent);
            TextMeshProUGUI textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = label;
            textMesh.fontSize = 30;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Center;
            RectTransform rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);
            rect.localPosition = new Vector3(x, 200, 0);

            gobj = CreateObject("Current" + name);
            gobj.transform.SetParent(parent);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = new Vector3(x, 140, 0);
            field = gobj.AddComponent<Image>();
            field.color = new Color(1, 1, 1, 0.6f);
            gobj.AddComponent<Button>();

            gobj = CreateObject("Current" + name + "Text");
            gobj.transform.SetParent(rect.transform);
            rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 75);
            rect.localPosition = Vector3.zero;
            TextMeshProUGUI value = gobj.AddComponent<TextMeshProUGUI>();
            value.enableAutoSizing = true;
            value.color = Color.black;
            value.alignment = TextAlignmentOptions.Center;
            return value;
        }

        // Black border and a brighter background on the field the keyboard types into
        private static void SetFieldSelected(Image field, bool selected) {
            if(field == null) return;
            UnityEngine.UI.Outline outline = field.GetComponent<UnityEngine.UI.Outline>();
            if(outline == null) {
                outline = field.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(4, 4);
                outline.useGraphicAlpha = false; // Fully black, even though the field is see-through
            }
            outline.enabled = selected;
            field.color = new Color(1, 1, 1, selected ? 0.95f : 0.6f);
        }

        private Button CreateKey(RectTransform parent, string name, string label, Vector2 size, Vector3 position, Color textColor, UnityEngine.Events.UnityAction onClick) {
            GameObject gobj = CreateObject(name);
            gobj.transform.SetParent(parent);
            RectTransform rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.localPosition = position;
            Button btn = gobj.AddComponent<Button>();
            btn.targetGraphic = CreateKeyBackground(gobj);
            btn.colors = keyColor;

            GameObject text = CreateObject("Text");
            text.transform.SetParent(btn.transform, false);
            TextMeshProUGUI btnText = text.AddComponent<TextMeshProUGUI>();
            btnText.text = label;
            btnText.fontSize = Mathf.Min(80, size.y * 0.8f);
            btnText.fontStyle = FontStyles.Bold;
            btnText.color = textColor;
            btnText.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(onClick);
            return btn;
        }

        static readonly char[] keyboardKeys = {
            '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
            'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p',
            'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'z',
            'x', 'c', 'v', 'b', 'n', 'm', '-', '=', '[', ']',
            '/', '\\', ';', '\'', ',', '.', '`'
        };
        static readonly char[] shiftedKeyboardKeys = {
            '!', '@', '#', '$', '%', '^', '&', '*', '(', ')',
            'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',
            'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'Z',
            'X', 'C', 'V', 'B', 'N', 'M', '_', '+', '{', '}',
            '?', '|', ':', '"', '<', '>', '~'
        };

        // Keyboard with a shift key, placed like the one of the ip join
        private void CreateKeyboard(RectTransform parent, Action<char> onKey) {
            RectTransform[] panels = new RectTransform[2];
            char[][] layouts = { keyboardKeys, shiftedKeyboardKeys };
            for(int i = 0; i < layouts.Length; i++) {
                GameObject gobj = CreateObject("KeyboardPanel");
                gobj.transform.SetParent(parent);
                panels[i] = gobj.AddComponent<RectTransform>();
                panels[i].sizeDelta = new Vector2(850, 425);
                panels[i].localPosition = new Vector3(-120, -150, 0);
                GridLayoutGroup layout = gobj.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(80, 80);
                layout.spacing = new Vector2(5, 5);

                foreach(char key in layouts[i]) {
                    char mykey = key;
                    Button btn = CreateKey(panels[i], "Key" + mykey, mykey.ToString(), new Vector2(80, 80), Vector3.zero, keyTextColor, () => onKey(mykey));
                    btn.GetComponentInChildren<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
                    btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 60;
                }
            }
            panels[1].gameObject.SetActive(false);

            Button shift = CreateKey(parent, "Shift", "Shift", new Vector2(225, 75), new Vector3(190, -320, 0), keyTextColor, () => {
                bool shifted = !panels[1].gameObject.activeSelf;
                panels[0].gameObject.SetActive(!shifted);
                panels[1].gameObject.SetActive(shifted);
            });
            shift.GetComponentInChildren<TextMeshProUGUI>().fontSize = 60;
        }

        private GameObject CreateObject(string obj) {
            GameObject go = new GameObject(obj);
            go.transform.parent = transform;
            go.transform.localScale = Vector3.one;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localPosition = Vector3.zero;
            return go;
        }
        
        private Button CreateButton(string obj, Color color, Vector2 size, Texture2D icon = null) {
            GameObject go = new GameObject(obj);
            go.transform.parent = transform;
            go.transform.localScale = Vector3.one;
            go.transform.localEulerAngles = Vector3.zero;
            go.transform.localPosition = Vector3.zero;

            RectTransform rt = go.AddComponent<RectTransform>();
            Button btn = go.AddComponent<Button>();
            rt.sizeDelta = size;
            
            btn.targetGraphic = CreateTabBackground(go);
            btn.colors = tabColor;
            
            
            GameObject txtObj = CreateObject("Text");
            RectTransform rtText = txtObj.AddComponent<RectTransform>();
            rtText.sizeDelta = size;
            rtText.localPosition = Vector2.zero;
            TextMeshProUGUI btnText = txtObj.AddComponent<TextMeshProUGUI>();
            btnText.color = ReadableOnTab(color);
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.fontWeight = FontWeight.Bold;
            btnText.fontSize = 40;
            btnText.text = obj;
            txtObj.transform.SetParent(btn.transform);
            
            if(icon != null) {
                float min = Math.Min(size.x, size.y);
                
                GameObject iconObj = CreateObject("Text");
                RectTransform rtIcon = iconObj.AddComponent<RectTransform>();
                rtIcon.sizeDelta = Vector2.one * min * 1.4f;
                rtIcon.localPosition = new Vector2(0, 0.4f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height), Vector2.one / 2);
                iconImg.color = ReadableOnTab(color);
                
                btnText.fontSize = 20;
                btnText.fontStyle = FontStyles.Bold;
                rtText.localPosition = new Vector2(0, size.y * -0.8f);

                iconObj.transform.SetParent(btn.transform);
            }
            
            
            return btn;
        }

        // Toggle showing "Enabled" / "Disabled", for the options of the hosting pages
        private Toggle CreateEnabledToggle(RectTransform parent, string name, Vector3 position, bool value, UnityEngine.Events.UnityAction<bool> onChange) {
            GameObject gobj = CreateObject(name);
            gobj.transform.SetParent(parent);
            RectTransform rect = gobj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 80);
            rect.localPosition = position;
            Toggle toggle = gobj.AddComponent<Toggle>();
            toggle.targetGraphic = CreateTabBackground(gobj);

            GameObject text = CreateObject("Text");
            text.transform.SetParent(toggle.transform, false);
            rect = text.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI toggleText = text.AddComponent<TextMeshProUGUI>();
            toggleText.fontSize = 40;
            toggleText.fontStyle = FontStyles.Bold;
            toggleText.alignment = TextAlignmentOptions.Center;

            UnityEngine.Events.UnityAction<bool> updateDisplay = (isOn) => {
                toggle.colors = isOn ? tabSelectedColor : tabColor; // The tint can only darken the texture, so switch the tints
                toggleText.text  = isOn ? "Enabled" : "Disabled";
                toggleText.color = isOn ? keyConfirmColor : keyCancelColor;
            };
            toggle.isOn = value;
            updateDisplay(value);
            toggle.onValueChanged.AddListener(updateDisplay);
            toggle.onValueChanged.AddListener(onChange);
            return toggle;
        }

        // Right aligned label on the left of a row of the hosting page
        private void CreateRowLabel(RectTransform parent, string label, float y) {
            GameObject gobj = CreateObject(label + "Label");
            gobj.transform.SetParent(parent);
            TextMeshProUGUI textMesh = gobj.AddComponent<TextMeshProUGUI>();
            textMesh.text = label;
            textMesh.fontSize = 40;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.color = Color.black;
            textMesh.alignment = TextAlignmentOptions.Right;
            RectTransform rect = gobj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 80);
            rect.localPosition = new Vector3(-310, y, 0);
        }
    }
}
