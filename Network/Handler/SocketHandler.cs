using AMP.Logging;
using AMP.Network.Connection;
using AMP.Network.Helper;
using AMP.Network.Packets;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using System.Threading;

namespace AMP.Network.Handler {
    internal class SocketHandler : NetworkHandler {

        private string ip;
        private int port;

        internal TcpSocket tcp;
        internal UdpSocket udp;

        internal SocketHandler(string address, int port) {
            this.ip = NetworkUtil.GetIP(address);
            this.port = port;
        }


        internal override void Connect() {
            Log.Info($"[Client] Connecting to {ip}:{port}...");
            tcp = new TcpSocket(ip, port);
            tcp.onPacket += onTcpPacketReceived;
            udp = new UdpSocket(ip, port);
            udp.onPacket += onUdpPacketReceived;

            isConnected = tcp.client.Connected;
            if(!isConnected) {
                Log.Err("[Client] Connection failed. Check ip address and ports.");
                Disconnect();
            } else {
                tcp.SendPacket(new EstablishConnectionPacket(UserData.GetUserName(), ModManager.MOD_VERSION));
            }
        }

        internal void onTcpPacketReceived(NetPacket p) {
            onPacketReceived.Invoke(p);
            Interlocked.Add(ref reliableReceive, p.GetData().Length);
        }

        internal void onUdpPacketReceived(NetPacket p) {
            onPacketReceived.Invoke(p);
            Interlocked.Add(ref unreliableReceive, p.GetData().Length);
        }

        internal override void Disconnect() {
            isConnected = false;
            if(tcp != null) {
                tcp.SendPacket(new DisconnectPacket(0, "Connection closed"));
                tcp.Disconnect();
            }
            if(udp != null) udp.Disconnect();
            Log.Info("[Client] Disconnected.");
        }

        internal override void SendReliable(NetPacket packet) {
            tcp.SendPacket(packet);
            reliableSent += packet.GetData().Length;
        }

        internal override void SendUnreliable(NetPacket packet) {
            udp.SendPacket(packet);
            unreliableSent += packet.GetData().Length;
        }

    }
}
