using UnityEngine;
using AMP.Network.Server;
using AMP.Network.Client;
using AMP.Threading;
using System;
using System.Reflection;

namespace AMP {
    class ModManager : MonoBehaviour {
        public static ModManager instance;

        public static Server serverInstance;
        public static Client clientInstance;

        public static string MOD_NAME = "AMP v" + Assembly.GetExecutingAssembly().GetName().Version.ToString().TrimEnd(new char[] { '.', '0' });

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

            if(!clientInstance.isConnected)
                clientInstance = null;
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
