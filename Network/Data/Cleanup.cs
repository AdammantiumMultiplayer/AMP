using AMP.Network.Packets.Implementation;
using System.Collections.Generic;
using System.Linq;

namespace AMP.Network.Data {
    internal class Cleanup {

        public static void CheckItemLimit(ClientData client) {
            if(ModManager.safeFile.hostingSettings.maxItemsPerPlayer == 0) return;

            KeyValuePair<long, long>[] items = ModManager.serverInstance.item_owner.Where(val => val.Value == client.playerId).ToArray();

            if(items.Count() > ModManager.safeFile.hostingSettings.maxItemsPerPlayer) {
                int to_remove = items.Count() - ModManager.safeFile.hostingSettings.maxItemsPerPlayer;

                items = items.OrderBy(val => val.Key).ToArray();
                for(int i = 0; i < to_remove; i++) {
                    long itemId = items[i].Key;

                    ModManager.serverInstance.OnPacket(ClientData.SERVER, new ItemDespawnPacket(itemId));
                }
            }
        }

        public static void CheckCreatureLimit(ClientData client) {
            if(ModManager.safeFile.hostingSettings.maxCreaturesPerPlayer == 0) return;

            KeyValuePair<long, long>[] creatures = ModManager.serverInstance.creature_owner.Where(val => val.Value == client.playerId).ToArray();

            if(creatures.Count() > ModManager.safeFile.hostingSettings.maxCreaturesPerPlayer) {
                int to_remove = creatures.Count() - ModManager.safeFile.hostingSettings.maxCreaturesPerPlayer;

                creatures = creatures.OrderBy(val => val.Key).ToArray();
                for(int i = 0; i < to_remove; i++) {
                    long creatureId = creatures.First().Key;

                    ModManager.serverInstance.OnPacket(ClientData.SERVER, new CreatureDepawnPacket(creatureId));
                }
            }
        }

    }
}
