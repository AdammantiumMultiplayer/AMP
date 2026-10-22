using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.GameInteraction {
    /// <summary>
    /// Runs merged spells of other players on our side, for their visuals (beams, zaps, charge effect).
    ///
    /// The merge code of the game expects the hands to belong to the local player, it uses the controllers of the hands (haptics)
    /// and the joints to the body (PlayerLink) without checking for them, so it throws on other players. While it runs, the hands get
    /// a stand-in PlayerHand. It's never active and its controller hand isn't one of the PlayerControl hands, so nothing on our side
    /// is affected (no rumble, no joint changes).
    /// </summary>
    internal static class RemoteSpellMerge {

        #region Stand-in hand
        private static PlayerHand standInHand;
        private static PlayerHand StandInHand {
            get {
                if(standInHand == null) {
                    GameObject obj = new GameObject("AMP_RemoteSpellHand");
                    obj.SetActive(false); // Never active, so the components don't run Awake / OnEnable and don't register anywhere
                    UnityEngine.Object.DontDestroyOnLoad(obj);

                    standInHand = obj.AddComponent<PlayerHand>();
                    standInHand.controlHand = new PlayerControl.Hand(Side.Right);
                    standInHand.link = obj.AddComponent<PlayerLink>();
                }
                return standInHand;
            }
        }

        private static readonly HashSet<string> errorLogged = new HashSet<string>();

        /// <summary>
        /// Runs the action while the hands of the mana have the stand-in hand, and catches errors (logged once per spell and step)
        /// </summary>
        private static void WithStandInHands(Mana mana, SpellMergeData merge, string step, Action action) {
            RagdollHand left  = mana.casterLeft  != null ? mana.casterLeft.ragdollHand  : null;
            RagdollHand right = mana.casterRight != null ? mana.casterRight.ragdollHand : null;
            bool setLeft  = left  != null && left.playerHand  == null;
            bool setRight = right != null && right.playerHand == null;

            try {
                if(setLeft)  left.playerHand  = StandInHand;
                if(setRight) right.playerHand = StandInHand;

                action();
            } catch(Exception e) {
                string id = merge != null ? merge.id : "?";
                if(errorLogged.Add(id + step)) Log.Err(Defines.CLIENT, $"{step} of the merged spell {id} failed: {e}");
            } finally {
                if(setLeft)  left.playerHand  = null;
                if(setRight) right.playerHand = null;
            }
        }
        #endregion

        // The lifecycle is the same as in the game (Mana.UpdateMerge / Mana.UnloadMerge): A merge stays loaded after it ended,
        // until the spells of the hands change. So everything a merge starts (gravity bubble, effects, ...) runs as long as it does there,
        // without code for specific spells.

        /// <summary>
        /// Starts the merge of the spells in both hands. Loads it, or reuses the loaded one if it's still for the same spells.
        /// </summary>
        /// <returns>True if a merge is active</returns>
        internal static bool StartMerge(Mana mana) {
            if(mana.mergeInstance != null && mana.mergeActive) return true;

            string leftId  = mana.casterLeft?.spellInstance?.id;
            string rightId = mana.casterRight?.spellInstance?.id;
            if(string.IsNullOrEmpty(leftId) || string.IsNullOrEmpty(rightId)) return false;

            // Loaded merge for other spells, unload it like the game does when the spells change
            if(mana.mergeInstance != null && !IsMergeOf(mana.mergeInstance, leftId, rightId)) UnloadMerge(mana);

            if(mana.mergeInstance == null) {
                SpellMergeData mergeData = FindMergeData(leftId, rightId);
                if(mergeData == null) {
                    Log.Err(Defines.CLIENT, $"Unable to merge the spells {leftId} and {rightId}, no merge found.");
                    return false;
                }

                // Same as the game loads it (Mana.TryLoadMerge)
                mana.mergeData = mergeData;
                mana.mergeInstance = mergeData.Clone() as SpellMergeData;
                mana.mergeInstance.Load(mana);
                mana.mergeCastLoaded = true;
            }

            mana.mergeActive = true;
            // Normally Mana.UpdateMerge sets it once the hands are close enough (that loop is disabled for other players).
            // The merge only charges and updates its charge effect while completed, and the caster only charges it when it is
            mana.mergeCompleted = true;

            // Spawns the charge effect, and the merge specific parts (like the beam of the lightning merge, which Update needs)
            SpellMergeData merge = mana.mergeInstance;
            WithStandInHands(mana, merge, "Merge", () => merge.Merge(true));

            // Like when merging, the hands stop their own spells
            mana.casterLeft.Fire(false);
            mana.casterRight.Fire(false);
            return true;
        }

        /// <summary>
        /// Runs the merge for one frame while it's active, the charge stays the one we got synced (it would charge by itself otherwise)
        /// </summary>
        internal static void UpdateMerge(Mana mana) {
            SpellMergeData merge = mana.mergeInstance;
            if(merge == null || !mana.mergeActive) return;

            float syncedCharge = merge.currentCharge;
            WithStandInHands(mana, merge, "Update", () => merge.Update());
            merge.currentCharge = syncedCharge;
        }

        /// <summary>
        /// Ends the merge like the game does (Merge(false)), it stays loaded. For the gravity merge that's what opens the bubble.
        /// </summary>
        internal static void StopMerge(Mana mana) {
            SpellMergeData merge = mana.mergeInstance;
            if(merge == null || !mana.mergeActive) return;

            WithStandInHands(mana, merge, "Merge end", () => merge.Merge(false));
            mana.mergeActive = false;
            mana.mergeCompleted = false;
        }

        /// <summary>
        /// Unloads the merge, like the game does when the spells of the hands change (Mana.UnloadMerge).
        /// Needs to happen before loading another spell or despawning the player, the game would try it without the stand-in hand and fail.
        /// </summary>
        internal static void UnloadMerge(Mana mana) {
            if(mana == null) return;
            SpellMergeData merge = mana.mergeInstance;
            if(merge == null) return;

            StopMerge(mana);

            // Take it off the mana first, so the game doesn't try to unload it again
            mana.mergeInstance = null;
            mana.mergeCastLoaded = false;

            WithStandInHands(mana, merge, "Unload", () => merge.Unload());

            // In case the unload failed, at least get rid of the charge effect
            EffectInstance chargeEffect = merge.GetFieldValue<EffectInstance>("chargeEffect");
            if(chargeEffect != null && chargeEffect.isPlaying) {
                chargeEffect.End();
                chargeEffect.SetParent(null);
            }
        }

        private static bool IsMergeOf(SpellMergeData merge, string leftId, string rightId) {
            return (merge.leftSpellId == leftId && merge.rightSpellId == rightId)
                || (merge.leftSpellId == rightId && merge.rightSpellId == leftId);
        }

        private static SpellMergeData FindMergeData(string leftId, string rightId) {
            Func<SpellMergeData, bool> matches = merge => IsMergeOf(merge, leftId, rightId);

            // All merges the game knows, the caster might have unlocked ones we don't have
            try {
                SpellMergeData merge = Catalog.GetDataList<SpellMergeData>().FirstOrDefault(matches);
                if(merge != null) return merge;
            } catch(Exception e) {
                Log.Warn(Defines.CLIENT, $"Couldn't search the catalog for spell merges: {e.Message}");
            }

            // Fallback, the merges of our own player
            if(Player.currentCreature != null && Player.currentCreature.container != null) {
                foreach(ContainerContent content in Player.currentCreature.container.contents) {
                    SpellContent spellContent = content as SpellContent;
                    SpellMergeData merge = spellContent != null ? spellContent.data as SpellMergeData : null;
                    if(merge != null && matches(merge)) return merge;
                }
            }
            return null;
        }
    }
}
