using AMP.Network.Data.Sync;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    /// <summary>
    /// Debug helper, shows two camera facing shapes above the head of a creature:
    /// Circle = ragdoll state, Square = network ownership
    ///
    /// Circle: Standing = green, Destabilized = yellow, Inert = red, Frozen = cyan,
    ///         Kinematic = blue, NoPhysic = white, Disabled = black, Killed = magenta (overrides the state)
    /// Square: Ours (static) = blue, Ours (soft) = cyan, Other client (static) = red, Other client (soft) = orange,
    ///         Waiting for the server id = yellow, Spawning = white, Not networked = gray
    /// </summary>
    internal class CreatureDebugIndicator : MonoBehaviour {
        private Creature creature;
        private NetworkCreature networkCreature;

        // SpriteRenderers like the microphone icon, their default material works with the games render pipeline (custom shaders showed up pink)
        private SpriteRenderer stateIndicator;
        private SpriteRenderer ownerIndicator;

        private static Sprite circleSprite;
        private static Sprite squareSprite;

        void Awake() {
            creature = GetComponent<Creature>();

            if(circleSprite == null) circleSprite = CreateSprite(true);
            if(squareSprite == null) squareSprite = CreateSprite(false);

            stateIndicator = CreateIndicator(circleSprite, "DebugRagdollState", 0.15f);
            ownerIndicator = CreateIndicator(squareSprite, "DebugOwnership",    0.1f);
        }

        private static Sprite CreateSprite(bool circle) {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            for(int x = 0; x < size; x++) {
                for(int y = 0; y < size; y++) {
                    bool inside = !circle || new Vector2(x + 0.5f - radius, y + 0.5f - radius).magnitude <= radius;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one / 2, size); // 1 unit wide, gets scaled by the GameObject
        }

        private static SpriteRenderer CreateIndicator(Sprite sprite, string name, float size) {
            SpriteRenderer renderer = new GameObject(name).AddComponent<SpriteRenderer>(); // No collider, it shouldn't get in the way of anything
            renderer.sprite = sprite;
            renderer.transform.localScale = Vector3.one * size;
            return renderer;
        }

        // LateUpdate, so the ragdoll is already moved for this frame
        void LateUpdate() {
            if(creature == null) return;

            // No ?. here, it skips Unitys null check for destroyed objects (the head can get sliced off)
            Vector3 position = creature.transform.position + Vector3.up * 2.2f;
            if(creature.ragdoll != null && creature.ragdoll.headPart != null) position = creature.ragdoll.headPart.transform.position + Vector3.up * 0.35f;
            stateIndicator.transform.position = position;
            ownerIndicator.transform.position = position - Vector3.up * 0.15f;

            // Face the viewer, sprites are flat
            Transform view = Camera.main != null ? Camera.main.transform : null;
            if(view == null && Player.local != null && Player.local.head != null) view = Player.local.head.transform;
            if(view != null) {
                stateIndicator.transform.rotation = view.rotation;
                ownerIndicator.transform.rotation = view.rotation;
            }

            stateIndicator.color = GetStateColor();
            ownerIndicator.color = GetOwnerColor();
        }

        private Color GetStateColor() {
            if(creature.isKilled) return Color.magenta;
            if(creature.ragdoll == null) return Color.gray;

            switch(creature.ragdoll.state) {
                case Ragdoll.State.Standing:     return Color.green;
                case Ragdoll.State.Destabilized: return Color.yellow;
                case Ragdoll.State.Inert:        return Color.red;
                case Ragdoll.State.Frozen:       return Color.cyan;
                case Ragdoll.State.Kinematic:    return Color.blue;
                case Ragdoll.State.NoPhysic:     return Color.white;
                case Ragdoll.State.Disabled:     return Color.black;
                default:                         return Color.gray;
            }
        }

        private Color GetOwnerColor() {
            if(networkCreature == null) networkCreature = GetComponent<NetworkCreature>();

            CreatureNetworkData cnd = networkCreature != null ? networkCreature.creatureNetworkData : null;
            if(cnd == null)        return Color.gray;                    // Not networked
            if(cnd.isSpawning)     return Color.white;
            if(cnd.networkedId <= 0) return Color.yellow;                // Spawned by us, server hasn't answered yet

            bool ours = cnd.clientsideId > 0;
            if(ours) return cnd.staticOwner ? Color.blue : Color.cyan;
            return cnd.staticOwner ? Color.red : new Color(1f, 0.5f, 0f); // Orange
        }

        void OnDestroy() {
            if(stateIndicator != null) Destroy(stateIndicator.gameObject);
            if(ownerIndicator != null) Destroy(ownerIndicator.gameObject);
        }
    }
}
