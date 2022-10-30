using AMP.Network.Data.Sync;
using AMP.Network.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using AMP.Extension;

namespace AMP.GameInteraction {
    internal class PlayerEquipment {


        internal static void Read() {
            if(Player.currentCreature == null) return;

            ModManager.clientSync.syncData.myPlayerData.colors[0] = Player.currentCreature.GetColor(Creature.ColorModifier.Hair);
            ModManager.clientSync.syncData.myPlayerData.colors[1] = Player.currentCreature.GetColor(Creature.ColorModifier.HairSecondary);
            ModManager.clientSync.syncData.myPlayerData.colors[2] = Player.currentCreature.GetColor(Creature.ColorModifier.HairSpecular);
            ModManager.clientSync.syncData.myPlayerData.colors[3] = Player.currentCreature.GetColor(Creature.ColorModifier.EyesIris);
            ModManager.clientSync.syncData.myPlayerData.colors[4] = Player.currentCreature.GetColor(Creature.ColorModifier.EyesSclera);
            ModManager.clientSync.syncData.myPlayerData.colors[5] = Player.currentCreature.GetColor(Creature.ColorModifier.Skin);

            ModManager.clientSync.syncData.myPlayerData.equipment = Player.currentCreature.ReadWardrobe();
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
