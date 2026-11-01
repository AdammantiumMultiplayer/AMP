using AMP.Network.Client.NetworkComponents;
using ThunderRoad;

namespace AMP.Extension {
    internal static class CollisionInstanceExtension {

        public static bool IsDoneByCreature(this CollisionInstance collisionInstance, Creature creature) {
            if(creature == null) return false;

            if(collisionInstance.sourceColliderGroup) {
                if(collisionInstance.sourceColliderGroup.collisionHandler.item?.lastHandler?.creature == creature) {
                    return true;
                }

                if(collisionInstance.sourceColliderGroup.collisionHandler.ragdollPart?.ragdoll.creature.lastInteractionCreature == creature) {
                    return true;
                }
            } else {
                if(collisionInstance.casterHand?.mana.creature == creature) {
                    return true;
                }

                if(collisionInstance.targetColliderGroup?.collisionHandler.ragdollPart?.ragdoll.creature.lastInteractionCreature == creature) {
                    return true;
                }
            }

            return false;
        }

        public static bool IsDoneByAnyCreature(this CollisionInstance collisionInstance) {
            if(collisionInstance.sourceColliderGroup) {
                if(collisionInstance.sourceColliderGroup.collisionHandler.item?.lastHandler?.creature != null) {
                    return true;
                }

                if(collisionInstance.sourceColliderGroup.collisionHandler.ragdollPart?.ragdoll.creature.lastInteractionCreature != null) {
                    return true;
                }
            } else {
                if(collisionInstance.casterHand?.mana.creature != null) {
                    return true;
                }

                if(collisionInstance.targetColliderGroup?.collisionHandler.ragdollPart?.ragdoll.creature.lastInteractionCreature != null) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the collision is caused by something this client is responsible for:
        /// The local player, or a creature this client owns (so creatures we control can do damage to other creatures)
        /// </summary>
        public static bool IsDoneByLocalAuthority(this CollisionInstance collisionInstance) {
            if(collisionInstance == null) return false;

            if(collisionInstance.sourceColliderGroup) {
                if(IsLocallyControlled(collisionInstance.sourceColliderGroup.collisionHandler.item?.lastHandler?.creature)) {
                    return true;
                }

                Creature sourceCreature = collisionInstance.sourceColliderGroup.collisionHandler.ragdollPart?.ragdoll.creature;
                if(IsLocallyControlled(sourceCreature)) { // Hit with the body (punch, kick)
                    return true;
                }
                if(IsLocallyControlled(sourceCreature?.lastInteractionCreature)) {
                    return true;
                }
            } else {
                if (IsLocallyControlled(collisionInstance.casterHand?.mana.creature)) {
                    return true;
                }

                if(IsLocallyControlled(collisionInstance.targetColliderGroup?.collisionHandler.ragdollPart?.ragdoll.creature.lastInteractionCreature)) {
                    return true;
                }
            }

            return false;
        }

        // Local player, or a networked creature this client is the owner of
        private static bool IsLocallyControlled(Creature creature) {
            if(creature == null) return false;
            if(creature == Player.currentCreature) return true;

            NetworkCreature networkCreature = creature.GetComponent<NetworkCreature>();
            return networkCreature != null && !(networkCreature is NetworkPlayerCreature) && networkCreature.IsSending();
        }

        /// <summary>
        /// Checks if the damage comes from a spell of another player (like the lightning spray). We only run their spells for the visuals,
        /// the caster reports the hits of its own spells, so this damage shouldn't be reported or applied a second time.
        /// </summary>
        public static bool IsDoneByRemotePlayerSpell(this CollisionInstance collisionInstance) {
            if(collisionInstance == null || collisionInstance.casterHand == null || collisionInstance.casterHand.mana == null) return false;

            Creature caster = collisionInstance.casterHand.mana.creature;
            if(caster == null) return false;

            NetworkPlayerCreature networkPlayer = caster.GetComponent<NetworkPlayerCreature>();
            return networkPlayer != null && !networkPlayer.IsSending();
        }

        public static bool IsDoneByAnyProjectile(this CollisionInstance collisionInstance) {
            if (collisionInstance.sourceColliderGroup) {
                if (collisionInstance.sourceColliderGroup.collisionHandler.item?.GetComponentInChildren<ItemMagicProjectile>(true) != null) {
                    return true;
                }
            }
            return false;
        }

    }
}
