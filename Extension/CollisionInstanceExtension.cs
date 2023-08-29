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

    }
}
