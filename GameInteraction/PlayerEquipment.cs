using AMP.Extension;
using AMP.Network.Data.Sync;
using ThunderRoad;
using UnityEngine;

namespace AMP.GameInteraction {
    internal class CreatureEquipment {

        internal static void Read(PlayerNetworkData pnd) {
            if(Player.currentCreature == null) return;

            Read(Player.currentCreature, ref pnd.colors, ref pnd.equipment);
        }

        internal static void Apply(PlayerNetworkData pnd) {
            if(pnd == null) return;
            if(pnd.creature == null) return;

            Apply(pnd.creature, pnd.colors, pnd.equipment);
        }

        internal static void Read(Creature creature, ref Color[] colors, ref string[] wardrobe) {
            if(creature == null) return;

            colors = creature.ReadColors();
            wardrobe = creature.ReadWardrobe();
        }

        internal static void Apply(Creature creature, Color[] colors, string[] wardrobe) {
            if(creature == null) return;

            creature.ApplyColors(colors);
            creature.ApplyWardrobe(wardrobe);
        }

    }
}
