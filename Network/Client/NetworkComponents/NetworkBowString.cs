using AMP.Extension;
using AMP.Network.Packets.Implementation;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    /// <summary>
    /// Syncs how far a bow is drawn. On the side of the other players, the hand that draws the string is placed by the network,
    /// so it doesn't really pull the string and the bow stayed static. Instead the owner sends the draw, and the string gets moved there
    /// with the bows own SetStringTargetRatio, the bend of the bow follows the string by itself. Works for every bow using the games BowString.
    /// Only enabled while the bow is held, a bow nobody holds can't be drawn.
    /// </summary>
    internal class NetworkBowString : MonoBehaviour {
        private const float SEND_INTERVAL    = 1f / 30f;
        private const float MIN_CHANGE       = 0.02f;
        private const float SMOOTHING_TIME   = 0.05f;

        private NetworkItem networkItem;
        private Item item;
        private BowString bowString;

        private float lastSentRatio = 0f;
        private float lastSendTime = 0f;
        private float shownRatio = 0f;

        void Awake() {
            networkItem = GetComponent<NetworkItem>();
            item = GetComponent<Item>();
            bowString = GetComponentInChildren<BowString>();
            
            if(item != null) {
                item.OnGrabEvent += Item_OnGrabEvent;
                item.OnUngrabEvent += Item_OnUngrabEvent;
            }
            enabled = item != null && item.handlers.Count > 0;
        }

        void OnDestroy() {
            if(item != null) {
                item.OnGrabEvent -= Item_OnGrabEvent;
                item.OnUngrabEvent -= Item_OnUngrabEvent;
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand) {
            enabled = true;
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing) {
            // The hand letting go might still be in the list at this point
            foreach(RagdollHand handler in item.handlers) {
                if(handler != ragdollHand) return; // Still held by another hand
            }
            enabled = false;
        }

        void OnDisable() {
            // Not held anymore, the string goes back to rest
            if(networkItem == null || bowString == null || networkItem.itemNetworkData == null) return;

            if(networkItem.IsSending()) {
                if(lastSentRatio != 0f && networkItem.itemNetworkData.networkedId > 0) {
                    new ItemBowStringPacket(networkItem.itemNetworkData.networkedId, 0f).SendToServerUnreliable();
                }
                lastSentRatio = 0f;
            } else {
                networkItem.itemNetworkData.bowPullRatio = 0f;
                shownRatio = 0f;
                bowString.SetStringTargetRatio(0f);
            }
        }

        void Update() {
            if(networkItem == null || bowString == null) return;
            if(networkItem.itemNetworkData == null || networkItem.itemNetworkData.networkedId <= 0) return;

            if(networkItem.IsSending()) {
                float ratio = bowString.currentPullRatio;
                if(Time.unscaledTime - lastSendTime < SEND_INTERVAL) return;
                // Always send the release, so the string doesn't stay drawn on the other side
                if(Mathf.Abs(ratio - lastSentRatio) < MIN_CHANGE && !(ratio == 0f && lastSentRatio != 0f)) return;

                lastSentRatio = ratio;
                lastSendTime = Time.unscaledTime;
                new ItemBowStringPacket(networkItem.itemNetworkData.networkedId, ratio).SendToServerUnreliable();
            } else {
                // Smooth between the packets, then move the string there
                float target = networkItem.itemNetworkData.bowPullRatio;
                shownRatio = Mathf.Lerp(shownRatio, target, 1f - Mathf.Exp(-Time.deltaTime / SMOOTHING_TIME));
                if(target <= 0f && shownRatio < 0.01f) shownRatio = 0f;

                bowString.SetStringTargetRatio(shownRatio);
            }
        }
    }
}
