using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Threading;
using Ping = System.Net.NetworkInformation.Ping;
using ThunderRoad;
using System.Collections;
using AMP.Network.Client;
using AMP.Network.Helper;
using System.Reflection;
using UnityEngine.SceneManagement;
using System.IO;

namespace AMP {
    public class GUIManager : MonoBehaviour {
        public string ip = "127.0.0.1";
        public int maxPlayers = 4;
        public string port = "26950";
        public string host_port = "26950";
        public int menu = 0;

        public long ping;

        private Rect windowRect = new Rect(Screen.width - 210, Screen.height - 140, 200, 130);

        void pingCallback(object sender, PingCompletedEventArgs e) {
            if (e==null || e.Cancelled || e.Error!=null || e.Reply==null) return;
            ping = e.Reply.RoundtripTime;
        }

        string title = "<color=#fffb00>" + ModManager.MOD_NAME + "</color>";


        private void PopulateWindow(int id) {
            if(ModManager.serverInstance != null) {
                title = $"[ Server { ModManager.MOD_VERSION } | Port: { host_port } ]";

                GUILayout.Label($"Players: {ModManager.serverInstance.connectedClients} / {maxPlayers}");
                //GUILayout.Label("Creatures: " + Creature.all.Count + " (Active: " + Creature.allActive.Count + ")");
                GUILayout.Label($"Items: {ModManager.serverInstance.spawnedItems}\n"
                               +$"Creatures: {ModManager.serverInstance.spawnedCreatures}"
                               );

                #if DEBUG_NETWORK
                GUILayout.Label($"Packets/s S: ↑ { ModManager.serverInstance.packetsSent } | ↓ { ModManager.serverInstance.packetsReceived }\n"
                               +$"                C: ↑ { ModManager.clientSync.packetsSentPerSec } | ↓ { ModManager.clientSync.packetsReceivedPerSec }"
                               );
                #endif
            } else if(ModManager.clientInstance != null) {
                title = $"[ Client { ModManager.MOD_VERSION } @ { ip } ]";

                if(ModManager.clientInstance.isConnected) {
                    #if DEBUG_NETWORK
                    GUILayout.Label($"Packets/s: ↑ { ModManager.clientSync.packetsSentPerSec } | ↓ { ModManager.clientSync.packetsReceivedPerSec }");
                    #endif
                } else {
                    GUILayout.Label("Connecting...");
                }

                if(GUI.Button(new Rect(10, 105, 180, 20), "Disconnect")) {
                    ModManager.StopClient();
                }
            } else {
                if(GUI.Button(new Rect(10, 25, 85, 20), menu == 0 ? "[ Join ]" : "Join")) {
                    menu = 0;
                }
                if(GUI.Button(new Rect(105, 25, 85, 20), menu == 1 ? "[ Host ]" : "Host")) {
                    menu = 1;
                }

                if(menu == 0) {
                    GUI.Label(new Rect(15, 50, 30, 20), "IP:");
                    GUI.Label(new Rect(15, 75, 30, 20), "Port:");

                    ip = GUI.TextField(new Rect(50, 50, 140, 20), ip);
                    port = GUI.TextField(new Rect(50, 75, 140, 20), port);

                    if(GUI.Button(new Rect(10, 100, 180, 20), "Join Server")) {
                        ModManager.JoinServer(ip, int.Parse(port));
                        ModManager.settings.SetOption("join_ip", ip);
                        ModManager.settings.SetOption("join_port", port);
                    }
                } else {
                    GUI.Label(new Rect(15, 50, 30, 20), "Max:");
                    GUI.Label(new Rect(15, 75, 30, 20), "Port:");

                    maxPlayers = (int) GUI.HorizontalSlider(new Rect(53, 55, 110, 20), maxPlayers, 2, 10);
                    GUI.Label(new Rect(175, 50, 30, 20), maxPlayers.ToString());
                    host_port = GUI.TextField(new Rect(50, 75, 140, 20), host_port);

                    if(GUI.Button(new Rect(10, 100, 180, 20), "Start Server")) {
                        ModManager.HostServer(maxPlayers, int.Parse(host_port));
                        ModManager.settings.SetOption("host_max", maxPlayers);
                        ModManager.settings.SetOption("host_port", host_port);
                    }
                }
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }


        private void OnGUI() {
            windowRect = GUI.Window(0, windowRect, PopulateWindow, title);

            #if TEST_BUTTONS
            if(GUI.Button(new Rect(0, 0, 100, 30), "Dump scenes")) {
                string log = "";
                for(int i = 0; i < SceneManager.sceneCount; i++) {
                    Scene scene = SceneManager.GetSceneAt(i);
                    GameObject[] gos = scene.GetRootGameObjects();
            
                    log += "SCENE: " + scene.name;
                    foreach(GameObject go in gos) {
                        log += LogLine(go, "");
                    }
                }
                File.WriteAllText("C:\\Users\\mariu\\Desktop\\log.txt", log);
            }

            if(GUI.Button(new Rect(0, 40, 100, 30), "Spawn")) {
                CreatureData creatureData = Catalog.GetData<CreatureData>("HumanMale");
                if(creatureData != null) {
                    Vector3 position = Player.local.transform.position + (Vector3.right * 2) + Vector3.up;
                    Quaternion rotation = Player.local.transform.rotation;
                    
                    creatureData.brainId = "HumanStatic";
                    creatureData.containerID = "PlayerDefault";
                    creatureData.factionId = -1;
            
                    creatureData.SpawnAsync(position, 0, null, true, null, creature => {
                        Debug.Log("Spawned Dummy");
            
                        creature.maxHealth = 100000;
                        creature.currentHealth = creature.maxHealth;
            
                        creature.isPlayer = false;
                        //creature.enabled = false;
                        //creature.locomotion.enabled = false;
                        creature.animator.enabled = false;
                        //creature.climber.enabled = false;
                        //creature.ragdoll.enabled = false;
                        //foreach(RagdollPart ragdollPart in creature.ragdoll.parts) {
                        //    foreach(HandleRagdoll hr in ragdollPart.handles) hr.enabled = false;
                        //    ragdollPart.sliceAllowed = false;
                        //    ragdollPart.enabled = false;
                        //}
                        //creature.brain.Stop();
                        creature.StopAnimation();
                        //creature.brain.StopAllCoroutines();
                        //creature.locomotion.MoveStop();
                        //creature.animator.speed = 0f;
            
                        Creature.all.Remove(creature);
                        Creature.allActive.Remove(creature);
            
                        //StartCoroutine(moveTest(creature));
                    });
                }
            }
            #endif
        }

        private IEnumerator moveTest(Creature creature) {
            while(true) {
                yield return new WaitForSeconds(2f);
                creature.Teleport(creature.transform.position + Vector3.right, creature.transform.rotation);
            }
        }

        public static string LogLine(GameObject go, string prefix) {
            string compLine = "";
            UnityEngine.Component[] components = go.gameObject.GetComponents(typeof(UnityEngine.Component));
            foreach(UnityEngine.Component component in components) {
                compLine += " " + component.ToString();
            }

            string logLine = prefix + go.name + " <" + go.GetType().Name + "> [" + go.activeInHierarchy + "] " + compLine + "\n";

            prefix += "-";
            for(int i = 0; i < go.transform.childCount; i++) {
                logLine += LogLine(go.transform.GetChild(i).gameObject, prefix);
            }
            return logLine;
        }
    }
}
