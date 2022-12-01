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

            StartAwaitData();
        }

        public override void Disconnect() {
            if(client != null) client.Close();
            client = null;
            endPoint = null;

            onPacket = null;

            base.Disconnect();
        }

        internal override void SendPacket(NetPacket packet) {
            try {
                if(client != null) {
                    byte[] data = packet.GetData(true);
                    client.Send(data, data.Length);
                }
            } catch(Exception e) {
                Log.Err($"Error sending data to {endPoint} via UDP: {e}");
            }
        }

        internal override void AwaitData() {
            while(client != null) {
                try {
                    // Read data
                    byte[] data = client.Receive(ref endPoint);

                    HandleData(data);
                } catch(ObjectDisposedException) { }
            }
        }
    }
}
