using AMP.Logging;
using AMP.Network.Helper;
using AMP.Network.Packets;
using AMP.Threading;
using System;
using System.Net;
using System.Net.Sockets;

namespace AMP.Network.Connection {
    internal class UdpSocket : NetSocket {
        internal IPEndPoint endPoint;
        private UdpClient client;

        internal int packetsSent = 0;
        internal int packetsReceived = 0;

        internal UdpSocket(IPEndPoint endPoint) {
            this.endPoint = endPoint;
        }

        internal UdpSocket(string ip, int port) {
            endPoint = new IPEndPoint(IPAddress.Parse(NetworkUtil.GetIP(ip)), port);
        }

        internal void Connect(int localPort) {
            client = new UdpClient(localPort);

            client.Connect(endPoint);

            client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        internal void Disconnect() {
            if(client != null) client.Close();
            endPoint = null;
        }

        internal void SendPacket(NetPacket packet) {
            if(packet == null) return;
            //packet.WriteLength();
            try {
                if(client != null) {
                    byte[] data = packet.GetData();
                    client.Send(data, data.Length);
                    packetsSent++;
                }
            } catch(Exception e) {
                Log.Err($"Error sending data to {endPoint} via UDP: {e}");
            }
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
                Log.Err("Failed to receive data with udp, " + e);
                Disconnect();
            }
        }


        internal int GetPacketsSent() {
            int i = packetsSent;
            packetsSent = 0;
            return i;
        }

        internal int GetPacketsReceived() {
            int i = packetsReceived;
            packetsReceived = 0;
            return i;
        }
    }
}
