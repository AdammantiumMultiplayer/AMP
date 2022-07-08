using AMP.Network.Data;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class EventHandler : MonoBehaviour {

        void Start() {

            EventManager.onLevelLoad += (levelData, eventTime) => {
                if(eventTime == EventTime.OnStart) {
                    if(ModManager.clientInstance == null) return;

                    ModManager.clientInstance.tcp.SendPacket(PacketWriter.LoadLevel(levelData.name));
                }
            };

            EventManager.onCreatureSpawn += (creature) => {
                if(ModManager.clientInstance == null) return;
                if(ModManager.clientSync == null) return;
                if(!creature.pooled) return;

                foreach(CreatureSync cs in ModManager.clientSync.syncData.creatures.Values) {
                    if(cs.clientsideCreature == creature) return; // If creature already exists, just exit
                }


                int currentCreatureId = ModManager.clientSync.syncData.currentClientCreatureId++;
                CreatureSync creatureSync = new CreatureSync() {
                    clientsideCreature = creature,
                    clientsideId = currentCreatureId,

                    creatureId = creature.creatureId,
                    containerID = creature.container.containerID,
                    factionId = creature.factionId,
                };

                ModManager.clientInstance.tcp.SendPacket(creatureSync.CreateSpawnPacket());
                ModManager.clientSync.syncData.creatures.Add(-currentCreatureId, creatureSync);

                creature.OnDamageEvent += (collisionInstance) => {
                    if(creatureSync.health > 0) {
                        ModManager.clientInstance.udp.SendPacket(creatureSync.CreateHealthPacket());
                    } else {
                        ModManager.clientInstance.tcp.SendPacket(creatureSync.CreateHealthPacket());
                    }
                };

                Debug.Log($"[Client] Creature {creature.creatureId} has been spawned.");
            };
        }
    }
}
