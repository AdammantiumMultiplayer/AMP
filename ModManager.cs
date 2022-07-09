using UnityEngine;
using AMP.Network.Helper;
using AMP.Network.Client;
using AMP.Threading;
using System;
using System.Reflection;
using AMP.Network.Server;
using AMP.Extension;
using ThunderRoad;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using static ThunderRoad.WaveSpawner;
using System.Collections.Generic;

namespace AMP {
    class ModManager : MonoBehaviour {
        public static ModManager instance;

        public static Server serverInstance;

        public static Client clientInstance;
        public static ClientSync clientSync;

        public static string MOD_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString().TrimEnd(new char[] { '.', '0' });
        public static string MOD_NAME = "AMP v" + MOD_VERSION;
        public static int TICK_RATE = 60;


        void Awake() {
            if (instance != null) {
                Destroy(gameObject);
                return;
            } else {
                instance = this;
                DontDestroyOnLoad(gameObject);

                Initialize();
            }
        }
        
        void Initialize() {
            gameObject.AddComponent<UnityMainThreadDispatcher>();
            gameObject.AddComponent<GUIManager>();
            gameObject.AddComponent<EventHandler>();

            Debug.Log($"<color=#FF8C00>[AMP] {MOD_NAME} has been initialized.</color>");
        }

        float time = 0f;
        void FixedUpdate() {
            time += Time.fixedDeltaTime;
            if(time > 1) {
                time = 0;
                if(serverInstance != null) serverInstance.UpdatePacketCount();

                //foreach(WaveSpawner spawner in WaveSpawner.instances) {
                //    Debug.Log(spawner);
                //    Debug.Log(spawner.creatureQueue.Count);
                //    Debug.Log(spawner.spawnedCreatures.Count);
                //    Debug.Log(Creature.all.Count);
                //    Debug.Log(Creature.allActive.Count);
                //
                //    Type typecontroller = typeof(WaveSpawner);
                //    System.Reflection.FieldInfo finfo = typecontroller.GetField("waveFactionInfos", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);
                //    Dictionary<int, FactionInfo> waveFactionInfos;
                //    if(finfo != null) {
                //        waveFactionInfos = (Dictionary<int, FactionInfo>) finfo.GetValue(spawner);
                //        Debug.Log(waveFactionInfos.Count);
                //        foreach(int i in waveFactionInfos.Keys)
                //            Debug.Log("> " + i);
                //    }
                //
                //    foreach(WaveCreature waveCreature in spawner.creatureQueue) {
                //        Debug.Log(">> " + waveCreature + " > " + waveCreature.data.factionId);
                //        Debug.Log(">> " + waveCreature.creature);
                //        try { Debug.Log(">> " + waveCreature.creature.name + " > " + waveCreature.data.factionId); } catch { }
                //    }
                //
                //    foreach(WaveCreature waveCreature in spawner.spawnedCreatures) {
                //        Debug.Log(">> " + waveCreature + " > " + waveCreature.data.factionId);
                //        Debug.Log(">>> " + waveCreature.creature);
                //        try { Debug.Log(">>> " + waveCreature.creature.name + " > " + waveCreature.creature.data.factionId); } catch { }
                //    }
                //
                //}
            }
        }

        void OnApplicationQuit() {
            Exit();
        }

        void OnDestroy() {
            Exit();
        }

        private void Exit() {
            if(clientInstance != null) {
                StopClient();
            }
            if(serverInstance != null && serverInstance.isRunning) {
                StopHost();
            }
        }

        public static void JoinServer(string address, int port) {
            StopClient();

            clientInstance = new Client(address, port);
            clientInstance.Connect();

            if(!clientInstance.isConnected) {
                clientInstance = null;
            } else {
                if(instance.gameObject.GetComponent<ClientSync>() == null) {
                    clientSync = instance.gameObject.AddComponent<ClientSync>();
                }
            }
        }

        public static void HostServer(int maxPlayers, int port) {
            StopHost();

            serverInstance = new Server(maxPlayers, port);
            serverInstance.Start();

            if(serverInstance.isRunning) {
                JoinServer("127.0.0.1", port);
            } else {
                serverInstance.Stop();
                serverInstance = null;
                throw new Exception("[Server] Server Start failed somehow. #SNHE001");
            }
        }

        internal static void StopClient() {
            if(clientInstance == null) return;
            clientInstance.Disconnect();
            if(clientSync != null) {
                clientSync.Stop();
                Destroy(clientSync);
            }
            clientInstance = null;
        }

        public static void StopHost() {
            if(serverInstance == null) return;
            serverInstance.Stop();
            serverInstance = null;
        }

    }
}
