using AMP.Network.Data;
using AMP.Network.Helper;
using System.Net;
using System.Threading;
using UnityEngine;

namespace AMP.Network.Client {
    public class Client {
        internal bool isConnected = false;
        private string ip;
        private int port;

        public int id;
        public TcpSocket tcp;
        public UdpSocket udp;

        public Client(string address, int port) {
            this.ip = NetworkUtil.GetIP(address);
            this.port = port;
        }

        internal void Disconnect() {
            isConnected = false;
            if(tcp != null) tcp.Disconnect();
            if(udp != null) udp.Disconnect();
            Debug.Log("[Client] Disconnected.");
        }

        internal void Connect() {
            Debug.Log($"[Client] Connecting to {ip}:{port}...");
            tcp = new TcpSocket(ip, port);
            tcp.onPacket += OnPacket;
            udp = new UdpSocket(ip, port);
            udp.onPacket += OnPacket;

            isConnected = tcp.client.Connected;
            if(!isConnected) {
                Debug.Log("[Client] Connection failed. Check ip address and ports.");
                Disconnect();
            }
        }

        void OnPacket(Packet p) {
            int type = p.ReadInt();

            Debug.Log("[Client] Packet " + type);

            switch(type) {
                case (int) Packet.Type.welcome:
                    id = p.ReadInt();

                    udp.Connect(((IPEndPoint)tcp.client.Client.LocalEndPoint).Port);

                    Debug.Log("[Client] Assigned id " + id);
                    udp.SendData(PacketWriter.Welcome(id));

                    // TODO: Remove, just a test if packets are send
                    //Thread test = new Thread(() => {
                    //    for(int i = 0; i < 10000; i++) {
                    //        //Thread.Sleep(1000);
                    //        udp.SendData(PacketWriter.Message("MEEP"));
                    //    }
                    //});
                    //test.Start();

                    break;

                case (int) Packet.Type.message:
                    Debug.Log("[Client] Message: " + p.ReadString());
                    break;

                case (int) Packet.Type.error:
                    Debug.Log("[Client] Error: " + p.ReadString());
                    break;

                default: break;
            }
        }
    }
}
