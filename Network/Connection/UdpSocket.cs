using AMP.Logging;
using AMP.Network.Helper;
using AMP.Network.Packets;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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

            Task.Run(() => AwaitData());
        }

        internal void Disconnect() {
            if(client != null) client.Close();
            client = null;
            endPoint = null;
        }

        internal override void ProcessSendQueue() {
            lock(processPacketQueue) {
                while(processPacketQueue.Count > 0) {
                    NetPacket packet = processPacketQueue.Dequeue();

                    try {
                        if(client != null) {
                            byte[] data = packet.GetData(true);
                            client.Send(data, data.Length);
                        }
                    } catch(Exception e) {
                        Log.Err($"Error sending data to {endPoint} via UDP: {e}");
                    }
                }
            }
        }

        private async Task AwaitData() {
            while(client != null) {
                try {
                    // Read data
                    UdpReceiveResult result = await client.ReceiveAsync();

                    HandleData(result.Buffer);
                } catch(Exception e) {
                    Log.Err("Failed to receive data with udp, " + e);
                    Disconnect();
                }
            }
        }
    }
}
