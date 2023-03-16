using ThunderRoad;

namespace AMP.Extension {
    internal static class CollisionInstanceExtension {

        public static bool IsDoneByCreature(this CollisionInstance collisionInstance, Creature creature) {
            if(creature == null) return false;

            if((bool) collisionInstance.sourceColliderGroup) {
                if((bool) collisionInstance.sourceColliderGroup.collisionHandler.item?.lastHandler?.creature == creature) {
                    return true;
                }

                if((bool) collisionInstance.sourceColliderGroup.collisionHandler.ragdollPart?.ragdoll.creature.lastInteractionCreature == creature) {
                    return true;
                }
            } else {
                if((bool)collisionInstance.casterHand?.mana.creature == creature) {
                    return true;
                }

                if((bool)collisionInstance.targetColliderGroup?.collisionHandler.ragdollPart?.ragdoll.creature.lastInteractionCreature == creature) {
                    return true;
                }
            }

            return false;
        }

    }
}
