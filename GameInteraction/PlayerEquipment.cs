using AMP.Extension;
using AMP.Network.Data.Sync;
using ThunderRoad;

namespace AMP.GameInteraction {
    internal class PlayerEquipment {


        internal static void Read(PlayerNetworkData pnd) {
            if(Player.currentCreature == null) return;

            pnd.colors[0] = Player.currentCreature.GetColor(Creature.ColorModifier.Hair);
            pnd.colors[1] = Player.currentCreature.GetColor(Creature.ColorModifier.HairSecondary);
            pnd.colors[2] = Player.currentCreature.GetColor(Creature.ColorModifier.HairSpecular);
            pnd.colors[3] = Player.currentCreature.GetColor(Creature.ColorModifier.EyesIris);
            pnd.colors[4] = Player.currentCreature.GetColor(Creature.ColorModifier.EyesSclera);
            pnd.colors[5] = Player.currentCreature.GetColor(Creature.ColorModifier.Skin);

            pnd.equipment = Player.currentCreature.ReadWardrobe();
        }

        internal static void Update(PlayerNetworkData pnd) {
            if(pnd == null) return;
            if(pnd.creature == null) return;

            pnd.creature.SetColor(pnd.colors[0], Creature.ColorModifier.Hair);
            pnd.creature.SetColor(pnd.colors[1], Creature.ColorModifier.HairSecondary);
            pnd.creature.SetColor(pnd.colors[2], Creature.ColorModifier.HairSpecular);
            pnd.creature.SetColor(pnd.colors[3], Creature.ColorModifier.EyesIris);
            pnd.creature.SetColor(pnd.colors[4], Creature.ColorModifier.EyesSclera);
            pnd.creature.SetColor(pnd.colors[5], Creature.ColorModifier.Skin, true);

            pnd.creature.ApplyWardrobe(pnd.equipment);
        }

    }
}
