using AMP.Network.Data;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Network.Helper {
    public class UdpSocket {
        public IPEndPoint endPoint;
        private UdpClient client;

        public Action<Packet> onPacket;

        public UdpSocket(IPEndPoint endPoint) {
            this.endPoint = endPoint;
        }

        public UdpSocket(string ip, int port) {
            endPoint = new IPEndPoint(IPAddress.Parse(NetworkUtil.GetIP(ip)), port);
        }

        public void Connect(int localPort) {
            client = new UdpClient(localPort);

            client.Connect(endPoint);

            client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        public void Disconnect() {
            if(client != null) client.Close();
            endPoint = null;
        }

        public void SendPacket(Packet packet) {
            packet.WriteLength();
            try {
                if(client != null) {
                    client.Send(packet.ToArray(), packet.Length());
                }
            } catch(Exception e) {
                Debug.Log($"Error sending data to {endPoint} via UDP: {e}");
            }
        }

        public void HandleData(Packet packetData) {
            int packetLength = packetData.ReadInt();
            byte[] packetBytes = packetData.ReadBytes(packetLength);

            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                using(Packet packet = new Packet(packetBytes)) {
                    onPacket.Invoke(packet);
                }
            });
        }

        private void ReceiveCallback(IAsyncResult _result) {
            try {
                // Read data
                byte[] array = client.EndReceive(_result, ref this.endPoint);
                client.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
                if(array.Length < 4) {
                    Disconnect();
                    return;
                }
                HandleData(array);
            } catch(Exception e) {
                Debug.LogError("Failed to receive data with udp, " + e);
                Disconnect();
            }
        }

        private void HandleData(byte[] _data) {
            using(Packet packet = new Packet(_data)) {
                // Read length of data
                int length = packet.ReadInt(true);
                // Read rest of data
                _data = packet.ReadBytes(length, true);
            }
            // Run packet handler on main thread
            UnityMainThreadDispatcher.Instance().Enqueue(delegate {
                using(Packet packet = new Packet(_data)) {
                    onPacket.Invoke(packet);
                }
            });
        }
    }
}
