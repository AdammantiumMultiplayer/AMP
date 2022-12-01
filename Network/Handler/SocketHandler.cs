using AMP.Data;
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


        internal override void Connect(string password = "") {
            Log.Info(Defines.CLIENT, $"Connecting to {ip}:{port}...");
            tcp = new TcpSocket(ip, port);
            tcp.onPacket += onTcpPacketReceived;
            tcp.onDisconnect += onConnectionAbort;
            udp = new UdpSocket(ip, port);
            udp.onPacket += onUdpPacketReceived;
            udp.onDisconnect += onConnectionAbort;

            isConnected = tcp.client.Connected;
            if(!isConnected) {
                Log.Err(Defines.CLIENT, $"Connection failed. Check ip address and ports.");
                Disconnect();
            } else {
                tcp.QueuePacket(new EstablishConnectionPacket(UserData.GetUserName(), Defines.MOD_VERSION, password));
            }
        }

        private void onConnectionAbort() {
            Log.Err(Defines.CLIENT, "Error at connection resulted in disconnect.");
            Disconnect();
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
                tcp.onDisconnect -= onConnectionAbort;
                tcp.QueuePacket(new DisconnectPacket(0, "Connection closed"));
                tcp.Disconnect();
            }
            if(udp != null) {
                udp.onDisconnect -= onConnectionAbort;
                udp.Disconnect();
            }
            Log.Info(Defines.CLIENT, "Disconnected.");
        }

        internal override void SendReliable(NetPacket packet) {
            tcp.QueuePacket(packet);
            reliableSent += packet.GetData().Length;
        }

        internal override void SendUnreliable(NetPacket packet) {
            udp.QueuePacket(packet);
            unreliableSent += packet.GetData().Length;
        }

    }
}
