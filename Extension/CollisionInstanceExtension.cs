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

    }
}
