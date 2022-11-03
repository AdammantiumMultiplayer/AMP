using AMP.Logging;
using AMP.Network.Helper;
using AMP.Network.Packets;
using System;
using System.Net;
using System.Net.Sockets;

namespace AMP.Network.Connection {
    internal class UdpSocket : NetSocket {
        internal IPEndPoint endPoint;
        private UdpClient client;

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
            client = null;
            endPoint = null;
        }

        internal new void SendPacket(NetPacket packet) {
            if(packet == null) return;
            //packet.WriteLength();
            base.SendPacket(packet);
            try {
                if(client != null) {
                    byte[] data = packet.GetData();
                    client.Send(data, data.Length);
                }
            } catch(Exception e) {
                Log.Err($"Error sending data to {endPoint} via UDP: {e}");
            }
        }

        private void ReceiveCallback(IAsyncResult _result) {
            if(client == null) return;
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
    }
}
