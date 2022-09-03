using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using AMP.Network.Helper;
using AMP.SupportFunctions;
using AMP.Threading;
using Chabuk.ManikinMono;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThunderRoad;
using UnityEngine;
using static Chabuk.ManikinMono.ManikinLocations;

namespace AMP.Network.Client {
    public class ClientSync : MonoBehaviour {
        public SyncData syncData = new SyncData();

        void Start () {
            if(!ModManager.clientInstance.nw.isConnected) {
                Destroy(this);
                return;
            }
            StartCoroutine(onUpdateTick());
        }

        public int packetsSentPerSec = 0;
        public int packetsReceivedPerSec = 0;

        float time = 0f;
        void FixedUpdate() {
            if(!ModManager.clientInstance.nw.isConnected) {
                Destroy(this);
                return;
            }
            if(ModManager.clientInstance.myClientId <= 0) return;

            time += Time.fixedDeltaTime;
            if(time > 1f) {
                time = 0f;

                // Packet Stats
                #if DEBUG_NETWORK
                packetsSentPerSec = (ModManager.clientInstance.tcp != null ? ModManager.clientInstance.tcp.GetPacketsSent() : 0)
                                  + (ModManager.clientInstance.udp != null ? ModManager.clientInstance.udp.GetPacketsSent() : 0);
                packetsReceivedPerSec = (ModManager.clientInstance.tcp != null ? ModManager.clientInstance.tcp.GetPacketsReceived() : 0)
                                      + (ModManager.clientInstance.udp != null ? ModManager.clientInstance.udp.GetPacketsReceived() : 0);
                #endif
            }
        }

        // Check player and item position about 60/sec
        IEnumerator onUpdateTick() {
            float time = Time.time;
            while(true) {
                float wait = 1f / Config.TICK_RATE;
                if(wait > Time.time - time) wait -= Time.time - time;
                if(wait > 0) yield return new WaitForSeconds(wait);
                time = Time.time;

                if(ModManager.clientInstance.myClientId <= 0) continue;
                if(!ModManager.clientInstance.readyForTransmitting) continue;
                if(Level.current != null && !Level.current.loaded) continue;

                if(syncData.myPlayerData == null) syncData.myPlayerData = new PlayerNetworkData();
                if(Player.local != null && Player.currentCreature != null) {
                    if(syncData.myPlayerData.creature == null) {
                        syncData.myPlayerData.creature = Player.currentCreature;

                        syncData.myPlayerData.clientId = ModManager.clientInstance.myClientId;
                        syncData.myPlayerData.name = UserData.GetUserName();

                        syncData.myPlayerData.height = Player.currentCreature.GetHeight();
                        syncData.myPlayerData.creatureId = Player.currentCreature.creatureId;

                        syncData.myPlayerData.playerPos = Player.currentCreature.transform.position;
                        syncData.myPlayerData.playerRot = Player.local.head.transform.eulerAngles.y;

                        syncData.myPlayerData.CreateConfigPacket().SendToServerReliable();

                        ReadEquipment();
                        syncData.myPlayerData.CreateEquipmentPacket().SendToServerReliable();

                        Player.currentCreature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();

                        SendMyPos(true);

                        yield return CheckUnsynchedItems(); // Send the item when the player first connected
                    } else {
                        SendMyPos();
                    }
                }
                try {
                    SendMovedItems();
                    SendMovedCreatures();
                }catch(Exception e) {
                    Log.Err($"[Client] Error: {e}");
                }
            }
        }

        internal void Stop() {
            StopAllCoroutines();
            foreach(PlayerNetworkData ps in syncData.players.Values) {
                LeavePlayer(ps);
            }
        }

        /// <summary>
        /// Checking if the player has any unsynched items that the server needs to know about
        /// </summary>
        private IEnumerator CheckUnsynchedItems() {
            // Get all items that are not synched
            List<Item> unsynced_items = Item.allActive.Where(item => syncData.items.All(entry => !item.Equals(entry.Value.clientsideItem))).ToList();

            foreach(Item item in unsynced_items) {
                if(item == null) continue;
                if(item.data == null) continue;

                if(!Config.ignoredTypes.Contains(item.data.type)) {
                    SyncItemIfNotAlready(item);

                    yield return new WaitForEndOfFrame();
                } else {
                    // Despawn all props until better syncing system, so we dont spam the other clients
                    item.Despawn();
                }
            }
        }

        private float lastPosSent = Time.time;
        public void SendMyPos(bool force = false) {
            if(Time.time - lastPosSent > 0.25f) force = true;

            if(Player.currentCreature == null) return;
            if(Player.currentCreature.ragdoll.ik.handLeftTarget == null) return;

            string pos = "init";
            try {
                if(!force) {
                    if(!SyncFunc.hasPlayerMoved()) return;
                }

                pos = "position";
                syncData.myPlayerData.playerPos = Player.currentCreature.transform.position;
                syncData.myPlayerData.playerRot = Player.local.head.transform.eulerAngles.y;
                syncData.myPlayerData.playerVel = Player.local.locomotion.rb.velocity;
                
                pos = "handLeft";
                syncData.myPlayerData.handLeftPos = Player.currentCreature.ragdoll.ik.handLeftTarget.position - syncData.myPlayerData.playerPos;
                syncData.myPlayerData.handLeftRot = Player.currentCreature.ragdoll.ik.handLeftTarget.eulerAngles;

                pos = "handRight";
                syncData.myPlayerData.handRightPos = Player.currentCreature.ragdoll.ik.handRightTarget.position - syncData.myPlayerData.playerPos;
                syncData.myPlayerData.handRightRot = Player.currentCreature.ragdoll.ik.handRightTarget.eulerAngles;

                pos = "head";
                syncData.myPlayerData.headPos = Player.currentCreature.ragdoll.headPart.transform.position;
                syncData.myPlayerData.headRot = Player.currentCreature.ragdoll.headPart.transform.eulerAngles;

                pos = "health";
                if(Player.currentCreature.isKilled)
                    syncData.myPlayerData.health = 0;
                else
                    syncData.myPlayerData.health = Player.currentCreature.currentHealth / Player.currentCreature.maxHealth;

                pos = "send";
                syncData.myPlayerData.CreatePosPacket().SendToServerUnreliable();
            } catch(Exception e) {
                Log.Err($"[Client] Error at {pos}: {e}");
            }
            lastPosSent = Time.time;
        }

        public void SendMovedItems() {
            foreach(KeyValuePair<long, ItemNetworkData> entry in syncData.items) {
                if(entry.Value.clientsideId <= 0 || entry.Value.networkedId <= 0) continue;

                if(SyncFunc.hasItemMoved(entry.Value)) {
                    entry.Value.UpdatePositionFromItem();
                    entry.Value.CreatePosPacket().SendToServerUnreliable();
                }
            }
        }

        public void SendMovedCreatures() {
            foreach(KeyValuePair<long, CreatureNetworkData> entry in syncData.creatures) {
                if(entry.Value.clientsideId <= 0 || entry.Value.networkedId <= 0) continue;

                if(SyncFunc.hasCreatureMoved(entry.Value)) {
                    entry.Value.UpdatePositionFromCreature();
                    entry.Value.CreatePosPacket().SendToServerUnreliable();
                }
            }
        }

        public void LeavePlayer(PlayerNetworkData ps) {
            if(ps == null) return;

            if(ps.creature != null) {
                Destroy(ps.creature.gameObject);
            }
        }

        public void MovePlayer(long clientId, PlayerNetworkData newPlayerSync) {
            PlayerNetworkData playerSync = ModManager.clientSync.syncData.players[clientId];

            if(playerSync != null && playerSync.creature != null) {
                playerSync.ApplyPos(newPlayerSync);

                playerSync.creature.transform.eulerAngles = new Vector3(0, playerSync.playerRot, 0);
                playerSync.networkCreature.targetPos = playerSync.playerPos;
                
                if(playerSync.creature.ragdoll.meshRootBone.transform.position.ApproximatelyMin(playerSync.creature.transform.position, Config.RAGDOLL_TELEPORT_DISTANCE)) {
                    //playerSync.creature.ragdoll.ResetPartsToOrigin();
                    //playerSync.creature.ragdoll.StandUp();
                    //Log.Warn("Too far away");
                }

                playerSync.networkCreature.handLeftTargetPos = playerSync.handLeftPos;
                playerSync.networkCreature.handLeftTargetRot = Quaternion.Euler(playerSync.handLeftRot);

                playerSync.networkCreature.handRightTargetPos = playerSync.handRightPos;
                playerSync.networkCreature.handRightTargetRot = Quaternion.Euler(playerSync.handRightRot);
                
                playerSync.networkCreature.headTargetPos = playerSync.headPos;
                playerSync.networkCreature.headTargetRot = Quaternion.Euler(playerSync.headRot);
            }
        }

        public static void SpawnPlayer(long clientId) {
            PlayerNetworkData playerSync = ModManager.clientSync.syncData.players[clientId];

            if(playerSync.creature != null || playerSync.isSpawning) return;

            CreatureData creatureData = Catalog.GetData<CreatureData>(playerSync.creatureId);
            if(creatureData == null) { // If the client doesnt have the creature, just spawn a HumanMale or HumanFemale (happens when mod is not installed)
                string creatureId = new System.Random().Next(0, 2) == 1 ? "HumanMale" : "HumanFemale";

                Log.Err($"[Client] Couldn't find playermodel for {playerSync.name} ({creatureData.id}), please check you mods. Instead {creatureId} is used now.");
                creatureData = Catalog.GetData<CreatureData>(creatureId);
            }
            if(creatureData != null) {
                playerSync.isSpawning = true;
                Vector3 position = playerSync.playerPos;
                float rotationY = playerSync.playerRot;

                creatureData.containerID = "Empty";

                ModManager.clientSync.StartCoroutine(creatureData.SpawnCoroutine(position, rotationY, ModManager.instance.transform, pooled: false, result: (creature) => {
                    playerSync.creature = creature;

                    creature.factionId = 2; // Should be the Player Layer so wont get ignored by the ai anymore


                    NetworkPlayerCreature networkPlayerCreature = playerSync.StartNetworking();

                    IKControllerFIK ik = creature.GetComponentInChildren<IKControllerFIK>();
                    
                    try {
                        Transform handLeftTarget = new GameObject("HandLeftTarget" + playerSync.clientId).transform;
                        handLeftTarget.parent = creature.transform;
                        #if DEBUG_INFO
                        TextMesh tm = handLeftTarget.gameObject.AddComponent<TextMesh>();
                        tm.text = "L";
                        tm.alignment = TextAlignment.Center;
                        tm.anchor = TextAnchor.MiddleCenter;
                        #endif
                        networkPlayerCreature.handLeftTarget = handLeftTarget;
                        ik.SetHandAnchor(Side.Left, handLeftTarget);
                    }catch(Exception) { Log.Err($"[Err] {clientId} ik target for left hand failed."); }

                    try {
                        Transform handRightTarget = new GameObject("HandRightTarget" + playerSync.clientId).transform;
                        handRightTarget.parent = creature.transform;
                        #if DEBUG_INFO
                        TextMesh tm = handRightTarget.gameObject.AddComponent<TextMesh>();
                        tm.text = "R";
                        tm.alignment = TextAlignment.Center;
                        tm.anchor = TextAnchor.MiddleCenter;
                        #endif
                        networkPlayerCreature.handRightTarget = handRightTarget;
                        ik.SetHandAnchor(Side.Right, handRightTarget);
                    } catch(Exception) { Log.Err($"[Err] {clientId} ik target for right hand failed."); }

                    try {
                        Transform headTarget = new GameObject("HeadTarget" + playerSync.clientId).transform;
                        headTarget.parent = creature.transform;
                        #if DEBUG_INFO
                        TextMesh tm = headTarget.gameObject.AddComponent<TextMesh>();
                        tm.text = "H";
                        tm.alignment = TextAlignment.Center;
                        tm.anchor = TextAnchor.MiddleCenter;
                        #endif
                        networkPlayerCreature.headTarget = headTarget;
                        ik.SetLookAtTarget(headTarget);
                    }catch(Exception) { Log.Err($"[Err] {clientId} ik target for head failed."); }

                    ik.handLeftEnabled = true;
                    ik.handRightEnabled = true;

                    if(GameConfig.showPlayerNames) {
                        Transform playerNameTag = new GameObject("PlayerNameTag" + playerSync.clientId).transform;
                        playerNameTag.parent = creature.transform;
                        playerNameTag.transform.localPosition = new Vector3(0, 2.5f, 0);
                        playerNameTag.transform.localEulerAngles = new Vector3(0, 180, 0);
                        TextMesh textMesh = playerNameTag.gameObject.AddComponent<TextMesh>();
                        textMesh.text = playerSync.name;
                        textMesh.alignment = TextAlignment.Center;
                        textMesh.anchor = TextAnchor.MiddleCenter;
                        textMesh.fontSize = 500;
                        textMesh.characterSize = 0.0025f;
                    }

                    if(GameConfig.showPlayerHealthBars) {
                        Transform playerHealthBar = new GameObject("PlayerHealthBar" + playerSync.clientId).transform;
                        playerHealthBar.parent = creature.transform;
                        playerHealthBar.transform.localPosition = new Vector3(0, 2.375f, 0);
                        playerHealthBar.transform.localEulerAngles = new Vector3(0, 180, 0);
                        TextMesh textMesh = playerHealthBar.gameObject.AddComponent<TextMesh>();
                        textMesh.text = HealthBar.calculateHealthBar(1f);
                        textMesh.alignment = TextAlignment.Center;
                        textMesh.anchor = TextAnchor.MiddleCenter;
                        textMesh.fontSize = 500;
                        textMesh.characterSize = 0.0003f;
                        playerSync.healthBar = textMesh;
                    }


                    creature.gameObject.name = "Player #" + playerSync.clientId;

                    creature.maxHealth = 1000;
                    creature.currentHealth = creature.maxHealth;
                    
                    creature.isPlayer = false;

                    //creature.enabled = false;
                    //creature.locomotion.enabled = false;
                    creature.locomotion.rb.useGravity = false;
                    creature.climber.enabled = false;
                    creature.mana.enabled = false;
                    //creature.animator.enabled = false;
                    creature.ragdoll.enabled = false;
                    creature.ragdoll.SetState(Ragdoll.State.Kinematic);
                    foreach(RagdollPart ragdollPart in creature.ragdoll.parts) {
                        foreach(HandleRagdoll hr in ragdollPart.handles) { Destroy(hr.gameObject); }// hr.enabled = false;
                        ragdollPart.sliceAllowed = false;
                        ragdollPart.DisableCharJointLimit();
                        ragdollPart.enabled = false;
                    }
                    creature.brain.Stop();
                    //creature.StopAnimation();
                    creature.brain.StopAllCoroutines();
                    creature.locomotion.MoveStop();
                    //creature.animator.speed = 0f;

                    if(playerSync.equipment.Count > 0) {
                        UpdateEquipment(playerSync);
                    }

                    creature.SetHeight(playerSync.height);

                    GameObject.DontDestroyOnLoad(creature.gameObject);

                    Creature.all.Remove(creature);
                    Creature.allActive.Remove(creature);

                    //File.WriteAllText("C:\\Users\\mariu\\Desktop\\log.txt", GUIManager.LogLine(creature.gameObject, ""));

                    IEnumerable<KeyValuePair<long, ItemNetworkData>> equipped_items = ModManager.clientSync.syncData.items.Where(entry => entry.Value.holderIsPlayer == true && entry.Value.creatureNetworkId == playerSync.clientId);
                    foreach(KeyValuePair<long, ItemNetworkData> equippedItem in equipped_items) {
                        equippedItem.Value.UpdateHoldState();
                    }

                    playerSync.isSpawning = false;

                    Log.Debug("[Client] Spawned Character for Player " + playerSync.clientId);
                }));

            }
        }

        public void SpawnCreature(CreatureNetworkData creatureSync) {
            if(creatureSync.clientsideCreature != null) return;

            creatureSync.isSpawning = true;
            CreatureData creatureData = Catalog.GetData<CreatureData>(creatureSync.creatureId);
            if(creatureData == null) { // If the client doesnt have the creature, just spawn a HumanMale or HumanFemale (happens when mod is not installed)
                string creatureId = new System.Random().Next(0, 2) == 1 ? "HumanMale" : "HumanFemale";

                Log.Err($"[Client] Couldn't spawn enemy {creatureData.id}, please check you mods. Instead {creatureId} is used now.");
                creatureData = Catalog.GetData<CreatureData>(creatureId);
            }

            if(creatureData != null) {
                Vector3 position = Vector3.one * 100000; // creatureSync.position is Zero at spawn time, and would just spawn them in the middle of the map
                float rotationY = creatureSync.rotation.y;

                creatureData.containerID = "Empty";

                StartCoroutine(creatureData.SpawnCoroutine(position, rotationY, ModManager.instance.transform, pooled: false, result: (creature) => {
                    creatureSync.clientsideCreature = creature;

                    creature.factionId = creatureSync.factionId;

                    creature.maxHealth = creatureSync.maxHealth;
                    creature.currentHealth = creatureSync.maxHealth;

                    creature.ApplyWardrobe(creatureSync.equipment);

                    creature.SetHeight(creatureSync.height);

                    UpdateCreature(creatureSync);

                    creature.transform.position = creatureSync.position;

                    creatureSync.StartNetworking();

                    Creature.all.Remove(creature);
                    Creature.allActive.Remove(creature);

                    creatureSync.isSpawning = false;
                }));
            } else {
                Log.Err($"[Client] Couldn't spawn {creatureSync.creatureId}. #SNHE003");
            }
        }

        public void UpdateCreature(CreatureNetworkData creatureSync) {
            if(creatureSync.clientsideCreature == null) return;

            Creature creature = creatureSync.clientsideCreature;
            
            if(creatureSync.clientsideId > 0) {
                return; // Don't update a creature we have control over
            } else {
                //creature.enabled = false; // TODO: Make it possible to keep it enabled
                creature.locomotion.rb.useGravity = false;
                creature.climber.enabled = false;
                creature.mana.enabled = false;
                creature.ragdoll.enabled = false;
                creature.ragdoll.SetState(Ragdoll.State.Kinematic);
                creature.brain.Stop();
                creature.brain.StopAllCoroutines();
                creature.brain.instance?.Unload();
                creature.brain.instance = null;
                creature.locomotion.MoveStop();

                //if(creatureSync.clientTarget >= 0 && !syncData.players.ContainsKey(creatureSync.clientTarget)) {
                //    // Stop the brain if no target found
                //    creatureSync.clientsideCreature.brain.Stop();
                //} else {
                //    if(creatureSync.clientTarget == 0) return; // Creature is not attacking player
                //
                //    // Restart the brain if its stopped
                //    if(creatureSync.clientsideCreature.brain.instance != null && !creatureSync.clientsideCreature.brain.instance.isActive) creatureSync.clientsideCreature.brain.instance.Start();
                //
                //    creatureSync.clientsideCreature.brain.currentTarget = syncData.players[creatureSync.clientTarget].creature;
                //}
            }
        }

        public void SyncItemIfNotAlready(Item item) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;
            if(!Item.allActive.Contains(item)) return;

            foreach(ItemNetworkData sync in ModManager.clientSync.syncData.items.Values) {
                if(item.Equals(sync.clientsideItem)) {
                    return;
                }
            }

            Log.Debug("[Client] Found new item " + item.data.id + " - Trying to spawn...");

            ModManager.clientSync.syncData.currentClientItemId++;

            ItemNetworkData itemSync = new ItemNetworkData() {
                dataId = item.data.id,
                clientsideItem = item,
                clientsideId = ModManager.clientSync.syncData.currentClientItemId,
                position = item.transform.position,
                rotation = item.transform.eulerAngles
            };

            ModManager.clientSync.syncData.items.Add(-ModManager.clientSync.syncData.currentClientItemId, itemSync);

            itemSync.CreateSpawnPacket().SendToServerReliable();
        }

        public void ReadEquipment() {
            if(Player.currentCreature == null) return;

            syncData.myPlayerData.colors[0] = Player.currentCreature.GetColor(Creature.ColorModifier.Hair);
            syncData.myPlayerData.colors[1] = Player.currentCreature.GetColor(Creature.ColorModifier.HairSecondary);
            syncData.myPlayerData.colors[2] = Player.currentCreature.GetColor(Creature.ColorModifier.HairSpecular);
            syncData.myPlayerData.colors[3] = Player.currentCreature.GetColor(Creature.ColorModifier.EyesIris);
            syncData.myPlayerData.colors[4] = Player.currentCreature.GetColor(Creature.ColorModifier.EyesSclera);
            syncData.myPlayerData.colors[5] = Player.currentCreature.GetColor(Creature.ColorModifier.Skin);

            syncData.myPlayerData.equipment = Player.currentCreature.ReadWardrobe();
        }

        public static void UpdateEquipment(PlayerNetworkData playerSync) {
            if(playerSync == null) return;
            if(playerSync.creature == null) return;

            playerSync.creature.SetColor(playerSync.colors[0], Creature.ColorModifier.Hair);
            playerSync.creature.SetColor(playerSync.colors[1], Creature.ColorModifier.HairSecondary);
            playerSync.creature.SetColor(playerSync.colors[2], Creature.ColorModifier.HairSpecular);
            playerSync.creature.SetColor(playerSync.colors[3], Creature.ColorModifier.EyesIris);
            playerSync.creature.SetColor(playerSync.colors[4], Creature.ColorModifier.EyesSclera);
            playerSync.creature.SetColor(playerSync.colors[5], Creature.ColorModifier.Skin, true);

            playerSync.creature.ApplyWardrobe(playerSync.equipment);
        }

        
    }
}
