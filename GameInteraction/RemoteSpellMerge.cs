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
        private static void WithStandInHands(Mana mana, string step, Action action) {
            RagdollHand left  = mana.casterLeft  != null ? mana.casterLeft.ragdollHand  : null;
            RagdollHand right = mana.casterRight != null ? mana.casterRight.ragdollHand : null;
            bool setLeft  = left  != null && left.playerHand  == null;
            bool setRight = right != null && right.playerHand == null;

            try {
                if(setLeft)  left.playerHand  = StandInHand;
                if(setRight) right.playerHand = StandInHand;

                action();
            } catch(Exception e) {
                string key = (mana.mergeInstance != null ? mana.mergeInstance.id : "?") + step;
                if(errorLogged.Add(key)) Log.Err(Defines.CLIENT, $"{step} of the merged spell {(mana.mergeInstance != null ? mana.mergeInstance.id : "")} failed: {e}");
            } finally {
                if(setLeft)  left.playerHand  = null;
                if(setRight) right.playerHand = null;
            }
        }
        #endregion

        /// <summary>
        /// Loads and starts the merge of the spells in both hands, if it isn't already
        /// </summary>
        /// <returns>True if a merge is active</returns>
        internal static bool StartMerge(Mana mana) {
            if(mana.mergeInstance != null) return true;

            string leftId  = mana.casterLeft?.spellInstance?.id;
            string rightId = mana.casterRight?.spellInstance?.id;
            if(string.IsNullOrEmpty(leftId) || string.IsNullOrEmpty(rightId)) return false;

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
            mana.mergeActive = true;
            // Normally Mana.UpdateMerge sets it once the hands are close enough (that loop is disabled for other players).
            // The merge only charges and updates its charge effect while completed, and the caster only charges it when it is
            mana.mergeCompleted = true;

            // Spawns the charge effect, and the merge specific parts (like the beam of the lightning merge, which Update needs)
            SpellMergeData merge = mana.mergeInstance;
            WithStandInHands(mana, "Merge", () => merge.Merge(true));

            // Like when merging, the hands stop their own spells
            mana.casterLeft.Fire(false);
            mana.casterRight.Fire(false);
            return true;
        }

        /// <summary>
        /// Runs the merge for one frame, the charge stays the one we got synced (it would charge by itself otherwise)
        /// </summary>
        internal static void UpdateMerge(Mana mana) {
            SpellMergeData merge = mana.mergeInstance;
            if(merge == null) return;

            float syncedCharge = merge.currentCharge;
            WithStandInHands(mana, "Update", () => merge.Update());
            merge.currentCharge = syncedCharge;
        }

        /// <summary>
        /// Stops the merge and its effects (beams, charge effect)
        /// </summary>
        internal static void StopMerge(Mana mana) {
            SpellMergeData merge = mana.mergeInstance;
            if(merge == null) return;

            // Same order as the game (Mana.UpdateMerge ends the merge, then it gets unloaded)
            WithStandInHands(mana, "Merge end", () => merge.Merge(false));
            WithStandInHands(mana, "Unload", () => merge.Unload());

            // In case the unload failed, at least get rid of the charge effect
            EffectInstance chargeEffect = merge.GetFieldValue<EffectInstance>("chargeEffect");
            if(chargeEffect != null && chargeEffect.isPlaying) {
                chargeEffect.End();
                chargeEffect.SetParent(null);
            }

            mana.mergeInstance = null;
            mana.mergeActive = false;
            mana.mergeCompleted = false;
            mana.mergeCastLoaded = false;
        }

        private static SpellMergeData FindMergeData(string leftId, string rightId) {
            Func<SpellMergeData, bool> matches = merge => (merge.leftSpellId == leftId && merge.rightSpellId == rightId)
                                                       || (merge.leftSpellId == rightId && merge.rightSpellId == leftId);

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
