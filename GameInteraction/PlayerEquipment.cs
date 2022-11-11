using AMP.Extension;
using AMP.Network.Data.Sync;
using ThunderRoad;

namespace AMP.GameInteraction {
    internal class PlayerEquipment {


        internal static void Read(PlayerNetworkData pnd) {
            if(Player.currentCreature == null) return;

            pnd.colors = Player.currentCreature.ReadColors();
            pnd.equipment = Player.currentCreature.ReadWardrobe();
        }

        internal static void Update(PlayerNetworkData pnd) {
            if(pnd == null) return;
            if(pnd.creature == null) return;

            pnd.creature.ApplyColors(pnd.colors);
            pnd.creature.ApplyWardrobe(pnd.equipment);
        }

    }
}
