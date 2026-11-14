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
    /// A note item that the server shows to only this player. It floats without gravity in front of the player,
    /// can be grabbed and moved, and has action buttons below the paper.
    /// It is never synced to other players (see ClientSync) and despawns when the player walks away from it.
    /// </summary>
    internal class PaperUI : ThunderBehaviour {
        public override ManagedLoops EnabledManagedLoops => ManagedLoops.Update | ManagedLoops.FixedUpdate;

        private const string NOTE_ITEM_ID      = "Note";
        private const float  EYE_LEVEL_OFFSET  = 0.15f;  // Paper spawns slightly below eye level
        private const float  BUTTON_MARGIN     = 0.02f;  // Gap between paper and buttons in meters
        private const float  BUTTON_HEIGHT     = 0.05f;  // Height of the button row in meters
        private const float  CANVAS_SCALE      = 0.001f; // 1 canvas unit = 1mm
        private const float  FLOAT_DAMPING     = 4f;     // How fast a released paper comes to rest
        private const float  BUTTON_COOLDOWN   = 3f;     // Buttons unlock again if the server didn't close the paper

        private static readonly Dictionary<string, PaperUI> active = new Dictionary<string, PaperUI>();

        private PaperUIPacket data;
        private Item item;
        private Note note;
        private Vector3 spawnPoint;
        private Vector3 toPlayer;
        private float buttonsLockedUntil = -1f;
        private bool closing = false;
        private bool textRefreshed = false;
        private readonly List<Button> buttons = new List<Button>();
        private Rigidbody[] rigidbodies = new Rigidbody[0];
        private float spawnTime;
        private bool loggedPhysics = false;

        // Note.SetText is private in ThunderRoad, so it has to be called through reflection
        private static readonly MethodInfo noteSetText = typeof(Note).GetMethod("SetText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(string) }, null);

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
            Quaternion rotation = Quaternion.LookRotation(toPlayer, Vector3.up);

            itemData.SpawnAsync((item) => {
                if(item == null) return;

                // Has to be the first thing, so the item never gets synced to the server
                PaperUI paperUI = item.gameObject.AddComponent<PaperUI>();

                // Another paper with the same id might have been spawned while this one was loading
                PaperUI old;
                if(active.TryGetValue(packet.identifier, out old) && old != null) old.Despawn(false);
                active[packet.identifier] = paperUI;

                paperUI.Init(packet, item, position, toPlayer);
            }, position, rotation, null, false);
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

            // B&S applies gravity itself through the CollisionHandlers, so rigidbody.useGravity doesn't do anything.
            // A physic modifier is what gravity spells use, it stays active while the paper is held
            item.SetPhysicModifier(this, 0f);
            item.physicBody.velocity = Vector3.zero;
            item.physicBody.angularVelocity = Vector3.zero;

            // The modifier only reaches the rigidbodies that have a CollisionHandler, so also turn gravity off on all of them directly
            spawnTime = Time.time;
            rigidbodies = item.GetComponentsInChildren<Rigidbody>(true);
            DisableGravity();
            Log.Debug(Defines.CLIENT, $"Paper \"{data.identifier}\" spawned: {rigidbodies.Length} rigidbodies, {item.collisionHandlers.Count} collision handlers, {item.GetComponentsInChildren<Gravity>(true).Length} gravity components.");

            note = item.GetComponentInChildren<Note>();
            AlignToPlayer();

            if(note != null) {
                SetNoteText(data.text);
            } else {
                Log.Err(Defines.CLIENT, $"Item {NOTE_ITEM_ID} has no Note component, can't show the text of paper \"{data.identifier}\".");
            }

            CreateButtons();
        }

        private void SetNoteText(string text) {
            if(noteSetText == null) {
                Log.Err(Defines.CLIENT, "Couldn't find Note.SetText, the text of the paper can't be shown.");
                return;
            }

            try {
                noteSetText.Invoke(note, new object[] { text });
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, e);
            }
        }

        private void DisableGravity() {
            foreach(Rigidbody rb in rigidbodies) {
                if(rb != null && rb.useGravity) rb.useGravity = false;
            }
            // Custom gravity of the game, gets added for multipliers other than 0 and 1
            foreach(Gravity gravity in item.GetComponentsInChildren<Gravity>()) {
                if(gravity.enabled) gravity.enabled = false;
            }
        }

        // Runs right before the physics step, so nothing can turn gravity back on in between
        protected override void ManagedFixedUpdate() {
            if(item == null || closing) return;

            DisableGravity();

            if(!loggedPhysics && Time.time - spawnTime > 1f) { // Debug, check that gravity really stays off
                loggedPhysics = true;
                Log.Debug(Defines.CLIENT, $"Paper \"{data.identifier}\" after 1s: useGravity={item.physicBody.useGravity}, kinematic={item.physicBody.isKinematic}, velocity={item.physicBody.velocity}.");
            }
        }

        protected override void ManagedUpdate() {
            if(item == null || closing) return;

            // ItemModuleNote might set its own text after the item data is loaded, so set ours again one frame later
            if(!textRefreshed) {
                textRefreshed = true;
                if(note != null) SetNoteText(data.text);
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

        private void Despawn(bool notifyServer) {
            if(closing) return;

            Cleanup(notifyServer);

            try {
                if(item != null) {
                    item.Despawn();
                }
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, e);
            }
        }

        // Item got removed some other way (level change, put into inventory, ...), let the server know
        private void Item_OnDespawnEvent(EventTime eventTime) {
            if(eventTime != EventTime.OnStart) return;
            if(closing) return;

            Cleanup(ModManager.clientInstance != null);
        }

        private void OnDestroy() {
            if(closing) return;

            Cleanup(ModManager.clientInstance != null);
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
            if(buttonCanvas != null) Destroy(buttonCanvas);
            Destroy(this);
        }

        #region Layout
        // Size of the paper in its own space, the size of the note mesh is only known at runtime
        private Bounds localBounds;
        private Vector3 paperNormal; // local axis the paper faces
        private Vector3 textRight;   // local axis the text is written along (the long side of the note)
        private Vector3 textUp;      // local axis pointing to the top of the text
        private GameObject buttonCanvas;

        /// <summary>
        /// Rotates the paper so its flat side faces the player and it stands upright,
        /// works out the axes from the renderer bounds, so it doesn't depend on how the note prefab is set up
        /// </summary>
        private void AlignToPlayer() {
            item.transform.rotation = Quaternion.identity;

            localBounds = new Bounds();
            bool first = true;
            foreach(Renderer renderer in item.GetComponentsInChildren<Renderer>()) {
                if(renderer is ParticleSystemRenderer) continue;
                Bounds b = renderer.bounds;
                b.center -= item.transform.position;
                if(first) { localBounds = b; first = false; }
                else localBounds.Encapsulate(b);
            }

            if(first) { // No renderers, should not happen, use a default A4 like size
                localBounds = new Bounds(Vector3.zero, new Vector3(0.21f, 0.297f, 0.002f));
            }

            Vector3 size = localBounds.size;
            Vector3[] axes = { Vector3.right, Vector3.up, Vector3.forward };
            float[] lengths = { size.x, size.y, size.z };
            int[] order = { 0, 1, 2 };
            Array.Sort((float[]) lengths.Clone(), order);

            paperNormal = axes[order[0]]; // Smallest extent is the thickness
            textRight   = axes[order[2]]; // The note writes its text along the long side
            textUp      = Vector3.Cross(textRight, paperNormal);

            // Flat side towards the player, text upright
            item.transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(paperNormal, textUp));
            item.transform.position = spawnPoint - item.transform.rotation * localBounds.center;
        }

        private void CreateButtons() {
            if(data.buttonActions.Length == 0) return;

            int count = Mathf.Min(data.buttonActions.Length, data.buttonLabels.Length > 0 ? data.buttonLabels.Length : data.buttonActions.Length);
            float paperWidth  = Vector3.Scale(localBounds.size, textRight).magnitude;
            float paperHeight = Vector3.Scale(localBounds.size, textUp).magnitude;

            GameObject canvasObj = new GameObject("PaperUIButtons", typeof(RectTransform));
            buttonCanvas = canvasObj;
            canvasObj.transform.SetParent(item.transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = false;

            // Canvas forward has to look away from the reader, so the opposite of the paper normal
            RectTransform rect = (RectTransform) canvasObj.transform;
            rect.localRotation = Quaternion.LookRotation(-paperNormal, textUp);
            rect.localPosition = localBounds.center - textUp * (paperHeight / 2f + BUTTON_MARGIN + BUTTON_HEIGHT / 2f);
            rect.localScale = Vector3.one * CANVAS_SCALE;
            rect.sizeDelta = new Vector2(paperWidth / CANVAS_SCALE, BUTTON_HEIGHT / CANVAS_SCALE);

            HorizontalLayoutGroup layout = canvasObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Pointer only shows up when it hits a collider tagged like this (same as the mod UI), trigger so it doesn't change the paper physics
            BoxCollider col = canvasObj.AddComponent<BoxCollider>();
            col.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 1);
            col.isTrigger = true;
            col.tag = "PointerActive";
            canvasObj.layer = 5;

            for(int i = 0; i < count; i++) {
                string action = data.buttonActions[i];
                string label  = i < data.buttonLabels.Length ? data.buttonLabels[i] : action;

                GameObject buttonObj = new GameObject("Button_" + action, typeof(RectTransform));
                buttonObj.transform.SetParent(canvasObj.transform, false);
                buttonObj.layer = 5;

                // Same look as the buttons in the mod menu
                Button button = buttonObj.AddComponent<Button>();
                button.targetGraphic = IngameModUI.CreateTabBackground(buttonObj);
                button.colors = IngameModUI.tabColor;
                button.onClick.AddListener(() => OnButtonPressed(action));
                buttons.Add(button);

                GameObject textObj = new GameObject("Text", typeof(RectTransform));
                textObj.transform.SetParent(buttonObj.transform, false);
                textObj.layer = 5;
                RectTransform textRect = (RectTransform) textObj.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                // Keep the text off the ornaments on the sides of the button frame
                textRect.offsetMin = new Vector2(BUTTON_HEIGHT / CANVAS_SCALE * 0.6f, 2);
                textRect.offsetMax = new Vector2(-BUTTON_HEIGHT / CANVAS_SCALE * 0.6f, -2);

                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = label;
                text.enableAutoSizing = true;
                text.fontSizeMin = 8;
                text.fontSizeMax = 30;
                text.fontStyle = FontStyles.Bold;
                text.color = IngameModUI.tabTextColor;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
            }

            PointerInputModule.SetUICameraToAllCanvas();
        }

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
