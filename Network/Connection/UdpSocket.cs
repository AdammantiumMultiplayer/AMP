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

        public override string TYPE => "UDP";

        internal UdpSocket(IPEndPoint endPoint) {
            this.endPoint = endPoint;
        }

        internal UdpSocket(string ip, int port) {
            endPoint = new IPEndPoint(IPAddress.Parse(NetworkUtil.GetIP(ip)), port);
        }

        internal void Connect(int localPort) {
            client = new UdpClient(localPort);

            client.Connect(endPoint);

            StartProcessing();
        }

        public override void Disconnect() {
            base.Disconnect();

            if(client != null) client.Close();
            client = null;
            endPoint = null;

            onPacket = null;
        }

        internal override void SendPacket(NetPacket packet) {
            try {
                if(client != null) {
                    byte[] data = packet.GetData(true);
                    client.SendAsync(data, data.Length);
                }
            } catch(Exception e) {
                Log.Err($"Error sending data to {endPoint} via UDP: {e}");
            }
        }

        internal override void AwaitData() {
            client.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        private void ReceiveCallback(IAsyncResult _result) {
            try {
                if(client != null && _result != null) {
                    // Read data
                    byte[] array = client.EndReceive(_result, ref this.endPoint);

                    if(array.Length < 4) {
                        Disconnect();
                        return;
                    }
                    HandleData(array);
                }
            } catch(Exception e) {
                Log.Err("Failed to receive data with udp, " + e);
                Disconnect();
            }
            if(client != null)
                client.BeginReceive(new AsyncCallback(this.ReceiveCallback), null);
        }

    }
}
