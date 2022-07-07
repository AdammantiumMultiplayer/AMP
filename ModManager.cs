using UnityEngine;
using AMP.Network.Helper;
using AMP.Network.Client;
using AMP.Threading;
using System;
using System.Reflection;
using AMP.Network.Server;
using AMP.Extension;

namespace AMP {
    class ModManager : MonoBehaviour {
        public static ModManager instance;

        public static Server serverInstance;

        public static Client clientInstance;
        public static ClientSync clientSync;

        public static string MOD_NAME = "AMP v" + MOD_VERSION;
        public static string MOD_VERSION = Assembly.GetExecutingAssembly().GetName().Version.ToString().TrimEnd(new char[] { '.', '0' });
        public static int TICK_RATE = 60;


        void Awake() {
            if (instance != null) {
                Destroy(gameObject);
                return;
            } else {
                instance = this;
                DontDestroyOnLoad(gameObject);
                gameObject.AddComponent<UnityMainThreadDispatcher>();
                gameObject.AddComponent<GUIManager>();

                Debug.Log($"[AMP] {MOD_NAME} has been initialized.");
            }
        }

        float time = 0f;
        void FixedUpdate() {
            time += Time.fixedDeltaTime;
            if(time > 1) {
                time = 0;
                if(serverInstance != null) serverInstance.UpdatePacketCount();
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
            clientInstance = null;
        }

        public static void StopHost() {
            if(serverInstance == null) return;
            serverInstance.Stop();
            serverInstance = null;
        }

    }
}
