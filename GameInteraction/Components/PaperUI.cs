using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Packets.Implementation;
using AMP.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThunderRoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AMP.GameInteraction.Components {
    /// <summary>
    /// A note item that the server shows to only this player. It floats without gravity in front of the player in portrait mode,
    /// can be grabbed and moved, and shows the text and the action buttons on the paper itself.
    /// It is never synced to other players (see ClientSync) and despawns when the player walks away from it.
    /// </summary>
    internal class PaperUI : ThunderBehaviour {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update | ManagedLoops.FixedUpdate | ManagedLoops.LateUpdate;

        private const string NOTE_ITEM_ID     = "Note";
        private const float  EYE_LEVEL_OFFSET = 0.15f;  // Paper spawns slightly below eye level
        private const float  CANVAS_SCALE     = 0.001f; // 1 canvas unit = 1mm
        private const float  PADDING          = 25f;    // Margin between the content and the edge of the paper in mm
        private const float  SURFACE_OFFSET   = 0.001f; // Distance of the text above the paper in meters, just enough to not clip into it
        private const float  BUTTON_HEIGHT    = 40f;    // Height of the button row in mm
        private const float  FLOAT_DAMPING    = 4f;     // How fast a released paper comes to rest
        private const float  BUTTON_COOLDOWN  = 3f;     // Buttons unlock again if the server didn't close the paper
        private const int    UI_LAYER         = 5;
        private static readonly Color INK_COLOR = new Color(0.15f, 0.12f, 0.1f);

        // Note.SetText is private in ThunderRoad, so it has to be called through reflection
        private static readonly MethodInfo noteSetText = typeof(Note).GetMethod("SetText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
        
        private static readonly Dictionary<string, PaperUI> active = new Dictionary<string, PaperUI>();

        private PaperUIPacket data;
        private Item item;
        private Note note;
        private Vector3 spawnPoint;
        private Vector3 toPlayer;
        private Rigidbody[] rigidbodies = new Rigidbody[0];
        private readonly List<Button> buttons = new List<Button>();
        private GameObject canvasObj;
        private float buttonsLockedUntil = -1f;
        private bool textCleared = false;
        private bool closing = false;

        // The note bends through bones, which a flat canvas can't follow, so the bones are kept in their flat pose
        private Transform[] bones = new Transform[0];
        private Vector3[] bonePositions = new Vector3[0];
        private Quaternion[] boneRotations = new Quaternion[0];

        // Paper size and axes in its own space, the note mesh is only known at runtime
        private Bounds localBounds;
        private Vector3 paperNormal; // Local axis the paper faces
        private Vector3 paperUp;     // Local axis pointing to the top of the paper (the long side, so it's portrait)

        #region Static API
        internal static void Show(PaperUIPacket packet) {
            if(packet == null || packet.identifier == null) return;
            if(Player.local == null || Player.local.head == null) return;

            // A new paper with the same id replaces the old one, without telling the server it was closed
            Close(packet.identifier, false);

            ItemData itemData = Catalog.GetData<ItemData>(NOTE_ITEM_ID);
            if(itemData == null) {
                Log.Err(Defines.CLIENT, $"Couldn't show paper \"{packet.identifier}\", item {NOTE_ITEM_ID} not found.");
                return;
            }

            Transform head = Player.local.head.transform;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if(forward.sqrMagnitude < 0.001f) forward = head.up; // Looking straight up / down
            forward.Normalize();

            Vector3 position = head.position + forward * packet.distance + Vector3.down * EYE_LEVEL_OFFSET;
            Vector3 toPlayer = -forward;

            itemData.SpawnAsync((item) => {
                if(item == null) return;

                // Has to be the first thing, so the item never gets synced to the server
                PaperUI paperUI = item.gameObject.AddComponent<PaperUI>();

                // Another paper with the same id might have been spawned while this one was loading
                PaperUI old;
                if(active.TryGetValue(packet.identifier, out old) && old != null) old.Despawn(false);
                active[packet.identifier] = paperUI;

                paperUI.Init(packet, item, position, toPlayer);
            }, position, Quaternion.LookRotation(toPlayer, Vector3.up), null, false);
        }

        /// <summary>
        /// Closes the paper with the given id
        /// </summary>
        /// <param name="notifyServer">Tell the server that the paper is gone, only needed if the client closed it by itself</param>
        internal static void Close(string identifier, bool notifyServer) {
            if(identifier == null) return;

            PaperUI paperUI;
            if(active.TryGetValue(identifier, out paperUI)) {
                active.Remove(identifier);
                if(paperUI != null) paperUI.Despawn(notifyServer);
            }
        }

        internal static void CloseAll() {
            foreach(PaperUI paperUI in active.Values.ToList()) {
                if(paperUI != null) paperUI.Despawn(false);
            }
            active.Clear();
        }
        #endregion

        private void Init(PaperUIPacket data, Item item, Vector3 spawnPoint, Vector3 toPlayer) {
            this.data = data;
            this.item = item;
            this.spawnPoint = spawnPoint;
            this.toPlayer = toPlayer;

            item.OnDespawnEvent += Item_OnDespawnEvent;

            // B&S applies gravity itself through the CollisionHandlers, so rigidbody.useGravity alone doesn't do anything.
            // A physic modifier is what gravity spells use, it stays active while the paper is held
            item.SetPhysicModifier(this, 0f);
            item.physicBody.velocity = Vector3.zero;
            item.physicBody.angularVelocity = Vector3.zero;

            // The modifier only reaches the rigidbodies that have a CollisionHandler, so also turn gravity off on all of them directly
            rigidbodies = item.GetComponentsInChildren<Rigidbody>(true);
            DisableGravity();

            // The game bakes the note text along the long side, which would be sideways in portrait mode, so we draw our own
            note = item.GetComponentInChildren<Note>();
            ClearNoteText();

            StoreFlatPose();
            AlignToPlayer();
            CreateCanvas();
        }

        private void ClearNoteText() {
            if(note == null || noteSetText == null) return;

            try {
                noteSetText.Invoke(note, new object[] { "" });
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, e);
            }
        }

        /// <summary>
        /// Remembers the flat pose of all bones, ManagedLateUpdate puts them back into it every frame.
        /// The physics of the bones stays untouched, the grab handles might be on them
        /// </summary>
        private void StoreFlatPose() {
            bones = item.GetComponentsInChildren<SkinnedMeshRenderer>()
                        .SelectMany(renderer => renderer.bones)
                        .Where(bone => bone != null && bone != item.transform)
                        .Distinct()
                        .ToArray();
            bonePositions = bones.Select(bone => bone.localPosition).ToArray();
            boneRotations = bones.Select(bone => bone.localRotation).ToArray();

            // Debug, to see how the note is built if it still bends
            Log.Debug(Defines.CLIENT, $"Paper \"{data.identifier}\": {bones.Length} bones, rigidbodies [{string.Join(", ", rigidbodies.Select(rb => rb.name))}], " +
                                      $"joints [{string.Join(", ", item.GetComponentsInChildren<Joint>(true).Select(j => j.GetType().Name + " on " + j.name))}], " +
                                      $"handles [{string.Join(", ", item.handles.Select(h => h.name + " on " + (h.physicBody != null ? h.physicBody.rigidBody.name : "-")))}].");
        }

        // Runs after animations and grab handling, so the paper is drawn flat every frame
        protected override void ManagedLateUpdate() {
            if(item == null || closing) return;

            for(int i = 0; i < bones.Length; i++) {
                if(bones[i] == null) continue;
                bones[i].localPosition = bonePositions[i];
                bones[i].localRotation = boneRotations[i];
            }
        }

        private void DisableGravity() {
            foreach(Rigidbody rb in rigidbodies) {
                if(rb != null && rb.useGravity) rb.useGravity = false;
            }
            // Custom gravity of the game, gets added at runtime for multipliers other than 0 and 1, so it can't be cached
            foreach(Gravity gravity in item.GetComponentsInChildren<Gravity>()) {
                if(gravity.enabled) gravity.enabled = false;
            }
        }

        // Runs right before the physics step, so nothing can turn gravity back on in between
        protected override void ManagedFixedUpdate() {
            if(item == null || closing) return;

            DisableGravity();
        }

        protected override void ManagedUpdate() {
            if(item == null || closing) return;

            // ItemModuleNote might set its own text after the item data is loaded, so clear it again one frame later
            if(!textCleared) {
                textCleared = true;
                ClearNoteText();
            }

            if(buttonsLockedUntil > 0 && Time.time > buttonsLockedUntil) {
                buttonsLockedUntil = -1f;
                SetButtonsInteractable(true);
            }

            // Keep it floating, held or not, in case something cleared the modifier
            if(!item.HasPhysicModifier(this)) item.SetPhysicModifier(this, 0f);

            // Slow down a released paper, so it stays where it was let go
            if(!item.IsHanded() && !item.isTelekinesisGrabbed) {
                float damping = Mathf.Clamp01(1f - FLOAT_DAMPING * Time.deltaTime);
                item.physicBody.velocity        *= damping;
                item.physicBody.angularVelocity *= damping;
            }

            // Walked away from the paper, so the player doesn't care about it anymore
            if(Player.local != null && Player.local.head != null) {
                Vector3 playerPos = Player.local.head.transform.position;
                if((playerPos - spawnPoint).sqrMagnitude > data.closeDistance * data.closeDistance) {
                    Close(data.identifier, true);
                }
            }
        }

        #region Closing
        private void Despawn(bool notifyServer) {
            if(closing) return;

            Cleanup(notifyServer);

            try {
                if(item != null) item.Despawn();
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, e);
            }
        }

        // Item got removed some other way (level change, put into inventory, ...), let the server know
        private void Item_OnDespawnEvent(EventTime eventTime) {
            if(eventTime == EventTime.OnStart && !closing) Cleanup(ModManager.clientInstance != null);
        }

        private void OnDestroy() {
            if(!closing) Cleanup(ModManager.clientInstance != null);
        }

        /// <summary>
        /// Removes everything that belongs to the paper UI from the item, items are pooled by the game
        /// and a later "Note" from the pool should be a normal synced item again
        /// </summary>
        private void Cleanup(bool notifyServer) {
            closing = true;

            if(data != null) {
                PaperUI current;
                if(active.TryGetValue(data.identifier, out current) && current == this) {
                    active.Remove(data.identifier);
                }

                if(notifyServer) {
                    new PaperUIClosePacket(data.identifier).SendToServerReliable();
                }
            }

            if(item != null) {
                item.OnDespawnEvent -= Item_OnDespawnEvent;
                item.RemovePhysicModifier(this); // Items are pooled, a later note should fall again
            }
            if(canvasObj != null) Destroy(canvasObj);
            Destroy(this);
        }
        #endregion

        #region Layout
        /// <summary>
        /// Rotates the paper so its flat side faces the player and its long side points up (portrait).
        /// The axes come from the renderer bounds, so it doesn't depend on how the note prefab is set up
        /// </summary>
        private void AlignToPlayer() {
            item.transform.rotation = Quaternion.identity;

            // Use the mesh bounds, the renderer bounds of skinned meshes are bigger than the paper itself
            bool found = false;
            foreach(Renderer renderer in item.GetComponentsInChildren<Renderer>()) {
                Mesh mesh = GetMesh(renderer);
                if(mesh == null) continue;

                Bounds b = mesh.bounds;
                for(int i = 0; i < 8; i++) {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 local = item.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if(found) localBounds.Encapsulate(local);
                    else { localBounds = new Bounds(local, Vector3.zero); found = true; }
                }
            }
            if(!found) localBounds = new Bounds(Vector3.zero, new Vector3(0.21f, 0.297f, 0.002f)); // Should not happen, A4 like size

            Vector3 size = localBounds.size;
            int smallest = 0, largest = 0;
            for(int i = 1; i < 3; i++) {
                if(size[i] < size[smallest]) smallest = i;
                if(size[i] > size[largest])  largest  = i;
            }

            paperNormal = Axis(smallest); // Smallest extent is the thickness
            paperUp     = Axis(largest);  // Long side up, so it's portrait

            // Flat side towards the player, long side up
            item.transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(paperNormal, paperUp));
            item.transform.position = spawnPoint - item.transform.rotation * localBounds.center;
        }

        private static Mesh GetMesh(Renderer renderer) {
            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            if(skinned != null) return skinned.sharedMesh;
            if(!(renderer is MeshRenderer)) return null;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static Vector3 Axis(int index) {
            Vector3 axis = Vector3.zero;
            axis[index] = 1f;
            return axis;
        }

        private static float SizeAlong(Vector3 size, Vector3 axis) {
            return Mathf.Abs(Vector3.Dot(size, axis));
        }

        /// <summary>
        /// One canvas lying on the paper, with the text on top and the buttons at the bottom
        /// </summary>
        private void CreateCanvas() {
            Vector3 paperRight = Vector3.Cross(paperUp, paperNormal);
            float width     = SizeAlong(localBounds.size, paperRight) / CANVAS_SCALE;
            float height    = SizeAlong(localBounds.size, paperUp)    / CANVAS_SCALE;
            float thickness = SizeAlong(localBounds.size, paperNormal);

            canvasObj = CreateUIObject("PaperUI", item.transform);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<GraphicRaycaster>().ignoreReversedGraphics = false;

            // Canvas forward has to look away from the reader, so the opposite of the paper normal
            RectTransform rect = (RectTransform) canvasObj.transform;
            rect.localRotation = Quaternion.LookRotation(-paperNormal, paperUp);
            rect.localPosition = localBounds.center + paperNormal * (thickness / 2f + SURFACE_OFFSET);
            rect.localScale    = Vector3.one * CANVAS_SCALE;
            rect.sizeDelta     = new Vector2(width, height);

            VerticalLayoutGroup layout = canvasObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int) PADDING, (int) PADDING, (int) PADDING, (int) PADDING);
            layout.spacing = PADDING / 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI text = CreateText(canvasObj.transform, data.text, INK_COLOR, TextAlignmentOptions.TopLeft, 40);
            text.fontStyle = FontStyles.Normal;
            text.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            if(data.buttonActions.Length > 0) CreateButtonRow(width - 2 * PADDING);

            PointerInputModule.SetUICameraToAllCanvas();
        }

        private void CreateButtonRow(float width) {
            GameObject row = CreateUIObject("Buttons", canvasObj.transform);
            row.AddComponent<LayoutElement>().minHeight = BUTTON_HEIGHT;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Pointer only shows up when it hits a collider tagged like this (same as the mod UI), trigger so it doesn't change the paper physics
            BoxCollider col = row.AddComponent<BoxCollider>();
            col.size = new Vector3(width, BUTTON_HEIGHT, 1);
            col.isTrigger = true;
            row.tag = "PointerActive";

            for(int i = 0; i < data.buttonActions.Length; i++) {
                string action = data.buttonActions[i];
                string label  = i < data.buttonLabels.Length ? data.buttonLabels[i] : action;

                GameObject buttonObj = CreateUIObject("Button_" + action, row.transform);

                // Same look as the buttons in the mod menu
                Button button = buttonObj.AddComponent<Button>();
                button.targetGraphic = IngameModUI.CreateTabBackground(buttonObj);
                button.colors = IngameModUI.tabColor;
                button.onClick.AddListener(() => OnButtonPressed(action));
                buttons.Add(button);

                TextMeshProUGUI text = CreateText(buttonObj.transform, label, IngameModUI.tabTextColor, TextAlignmentOptions.Center, 30);
                // Fill the button, but keep the text off the ornaments on the sides of the button frame
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2( BUTTON_HEIGHT * 0.6f,  2);
                textRect.offsetMax = new Vector2(-BUTTON_HEIGHT * 0.6f, -2);
            }
        }

        private static GameObject CreateUIObject(string name, Transform parent) {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            obj.layer = UI_LAYER;
            return obj;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string content, Color color, TextAlignmentOptions alignment, float maxSize) {
            TextMeshProUGUI text = CreateUIObject("Text", parent).AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8;
            text.fontSizeMax = maxSize;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
            return text;
        }
        #endregion

        #region Buttons
        private void OnButtonPressed(string action) {
            if(closing || data == null) return;

            new ActionButtonPressedPacket(data.identifier, action).SendToServerReliable();

            // No double clicks, the server decides if the paper closes
            SetButtonsInteractable(false);
            buttonsLockedUntil = Time.time + BUTTON_COOLDOWN;
        }

        private void SetButtonsInteractable(bool interactable) {
            foreach(Button button in buttons) {
                if(button != null) button.interactable = interactable;
            }
        }
        #endregion
    }
}
