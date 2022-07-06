using AMP.Network.Data;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client {
    public class ClientSync : MonoBehaviour {
        public SyncData syncData = new SyncData();

        void Start () {
            if(!ModManager.clientInstance.isConnected) {
                Destroy(this);
                return;
            }

        }

        float time = 0f;
        void FixedUpdate() {
            if(!ModManager.clientInstance.isConnected) {
                Destroy(this);
                return;
            }
            if(ModManager.clientInstance.myClientId <= 0) return;

            if(syncData.myPlayerData == null) syncData.myPlayerData = new PlayerSync();
            if(Player.local != null && Player.currentCreature != null) {
                if(syncData.myPlayerData.creature == null) {
                    syncData.myPlayerData.creature = Player.currentCreature;

                    syncData.myPlayerData.clientId = ModManager.clientInstance.myClientId;

                    syncData.myPlayerData.height = Player.currentCreature.GetHeight();
                    syncData.myPlayerData.creatureId = Player.currentCreature.creatureId;

                    syncData.myPlayerData.playerPos = Player.local.transform.position;
                    syncData.myPlayerData.playerRot = Player.local.transform.eulerAngles.y;

                    ModManager.clientInstance.tcp.SendPacket(syncData.myPlayerData.CreateConfigPacket());
                }

                syncData.myPlayerData.handLeftPos  = Player.currentCreature.handLeft.transform.position;
                syncData.myPlayerData.handLeftRot  = Player.currentCreature.handLeft.transform.eulerAngles;

                syncData.myPlayerData.handRightPos = Player.currentCreature.handRight.transform.position;
                syncData.myPlayerData.handRightRot = Player.currentCreature.handRight.transform.eulerAngles;

                syncData.myPlayerData.headRot      = Player.currentCreature.ragdoll.headPart.transform.eulerAngles;

                syncData.myPlayerData.playerPos    = Player.local.transform.position;
                syncData.myPlayerData.playerRot    = Player.local.transform.eulerAngles.y;
            }

            time += Time.fixedDeltaTime;
            if(time > 1f) {
                CheckUnsynchedItems();
                time = 0f;
            }

            SendMyPos(); // Some way to reduce?
            SendMovedItems();
        }

        private int currentClientItemId = 1;

        /// <summary>
        /// Checking if the player has any unsynched items that the server needs to know about
        /// </summary>
        private void CheckUnsynchedItems() {
            // Get all items that only the client is seeing
            List<Item> client_only_items = Item.allActive.Where(item => syncData.serverItems.All(item2 => !item.Equals(item2))).ToList();
            // Get all items that are not synched
            List<Item> unsynced_items = client_only_items.Where(item => syncData.clientItems.All(item2 => !item.Equals(item2))).ToList();

            //Debug.Log("client_only_items: " + client_only_items.Count);
            //Debug.Log("unsynced_items: " + client_only_items.Count);

            foreach(Item item in unsynced_items) {
                if(item.data.type != ThunderRoad.ItemData.Type.Prop && item.data.type != ThunderRoad.ItemData.Type.Body && item.data.type != ThunderRoad.ItemData.Type.Spell) {
                    currentClientItemId++;

                    ItemSync itemSync = new ItemSync() {
                        dataId = item.data.id,
                        clientsideItem = item,
                        clientsideId = currentClientItemId,
                        position = item.transform.position,
                        rotation = item.transform.eulerAngles
                    };
                    ModManager.clientInstance.tcp.SendPacket(itemSync.CreateSpawnPacket());

                    syncData.clientItems.Add(item);
                    syncData.itemDataMapping.Add(-currentClientItemId, itemSync);

                    Debug.Log("[Client] Found new item " + item.data.id + " - Trying to spawn...");
                } else {
                    // Despawn all props until better syncing system, so we dont spam the other clients
                    item.Despawn();
                }
            }

            // Get all despawned items
            List<Item> despawned = client_only_items.Where(item => Item.allActive.All(item2 => !item.Equals(item2))).ToList();
            foreach(Item item in despawned) {
                Debug.Log("[Client] Item " + item.data.id + " is despawned.");
                client_only_items.Remove(item);
            }
        }

        public void SendMyPos() {
            ModManager.clientInstance.udp.SendPacket(syncData.myPlayerData.CreatePosPacket());
        }

        public void SendMovedItems() {

        }

        public void SpawnPlayer(int clientId) {
            PlayerSync playerSync = ModManager.clientSync.syncData.players[clientId];

            if(playerSync.creature != null || playerSync.isSpawning) return;

            CreatureData creatureData = Catalog.GetData<CreatureData>(playerSync.creatureId);
            if(creatureData != null) {
                playerSync.isSpawning = true;
                Vector3 position = playerSync.playerPos;
                Quaternion rotation = Quaternion.Euler(0, playerSync.playerRot, 0);

                creatureData.brainId = "HumanStatic";
                creatureData.containerID = "PlayerDefault";
                creatureData.factionId = 0;

                creatureData.SpawnAsync(position, rotation, null, false, null, creature => {
                    Debug.Log("[Client] Spawned Character for Player " + playerSync.clientId);

                    playerSync.creature = creature;
                    //spawnedPlayer.leftHand = creature.handLeft.transform;
                    //spawnedPlayer.rightHand = creature.handRight.transform;
                    //spawnedPlayer.head = creature.ragdoll.headPart.transform;

                    creature.maxHealth = 100000;
                    creature.currentHealth = creature.maxHealth;

                    creature.isPlayer = false;
                    creature.enabled = false;
                    creature.locomotion.enabled = false;
                    creature.animator.enabled = false;
                    creature.ragdoll.enabled = false;
                    foreach(RagdollPart ragdollPart in creature.ragdoll.parts) {
                        foreach(HandleRagdoll hr in ragdollPart.handles) hr.enabled = false;
                        ragdollPart.sliceAllowed = false;
                        ragdollPart.enabled = false;
                    }
                    creature.brain.Stop();
                    creature.StopAnimation();
                    creature.brain.StopAllCoroutines();
                    creature.locomotion.MoveStop();
                    creature.animator.speed = 0f;

                    GameObject.DontDestroyOnLoad(creature);

                    Creature.all.Remove(creature);
                    Creature.allActive.Remove(creature);

                    playerSync.isSpawning = false;
                });
            }
        }

        internal void MovePlayer(int clientId, PlayerSync newPlayerSync) {
            PlayerSync playerSync = ModManager.clientSync.syncData.players[clientId];

            if(playerSync != null && playerSync.creature != null) {
                playerSync.playerPos = newPlayerSync.playerPos;
                playerSync.playerRot = newPlayerSync.playerRot;

                playerSync.creature.transform.position = playerSync.playerPos;
                playerSync.creature.transform.eulerAngles = new Vector3(0, playerSync.playerRot, 0);
            }
        }
    }
}
