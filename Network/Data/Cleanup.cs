using AMP.Network.Packets.Implementation;
using Netamite.Server.Data;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace AMP.Network.Data {
    internal class Cleanup {

        public static void CheckItemLimit(ClientInformation client) {
            if(ModManager.safeFile.hostingSettings.maxItemsPerPlayer == 0) return;

            KeyValuePair<int, int>[] items = ModManager.serverInstance.item_owner.Where(val => val.Value == client.ClientId).ToArray();

            if(items.Count() > ModManager.safeFile.hostingSettings.maxItemsPerPlayer) {
                int to_remove = items.Count() - ModManager.safeFile.hostingSettings.maxItemsPerPlayer;

                items = items.OrderBy(val => val.Key).ToArray();
                for(int i = 0; i < to_remove; i++) {
                    int itemId = items[i].Key;

                    new ItemDespawnPacket(itemId).ProcessServer(ModManager.serverInstance.netamiteServer, ClientData.SERVER);
                }
            }
        }

        public static void CheckCreatureLimit(ClientInformation client, bool onlyDead = true) {
            if(ModManager.safeFile.hostingSettings.maxCreaturesPerPlayer == 0) return;

            KeyValuePair<int, int>[] creatures = ModManager.serverInstance.creature_owner.Where(val => val.Value == client.ClientId).ToArray();

            if(creatures.Count() > ModManager.safeFile.hostingSettings.maxCreaturesPerPlayer) {
                int to_remove = creatures.Count() - ModManager.safeFile.hostingSettings.maxCreaturesPerPlayer;

                creatures = creatures.OrderBy(val => val.Key).ToArray();
                for(int i = 0; i < to_remove; i++) {
                    int creatureId = creatures[i].Key;

                    if(onlyDead && ModManager.serverInstance.creatures[creatureId].health > 0) continue;

                    new CreatureDepawnPacket(creatureId).ProcessServer(ModManager.serverInstance.netamiteServer, ClientData.SERVER);
                }
            }

            if(onlyDead) CheckCreatureLimit(client, false);
        }

    }
}
