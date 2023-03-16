using AMP.Data;
using AMP.Logging;
using AMP.Network.Connection;
using AMP.Network.Helper;
using AMP.Network.Packets;
using AMP.Network.Packets.Implementation;
using AMP.Security;
using AMP.SupportFunctions;
using System.Threading;

namespace AMP.Network.Handler {
    internal class SocketHandler : NetworkHandler {

        private string ip;
        private int port;
        private string password = "";

        internal TcpSocket tcp;
        internal UdpSocket udp;

        internal override string TYPE => "SOCKET";

        private string _STATE = "";
        internal override string STATE { get { return _STATE; } }

        internal SocketHandler(string address, int port) {
            this.ip = NetworkUtil.GetIP(address);
            this.port = port;
        }

        internal override string GetJoinSecret() {
            if(ip.StartsWith("127.") || ip.StartsWith("192.") || ip.StartsWith("172.")) return "";

            if(password != null && password.Length > 0) return "";

            return TYPE + ":" + ip + ":" + port;
        }

        internal override void Connect(string password = "") {
            Log.Info(Defines.CLIENT, $"Connecting to {ip}:{port}...");
            this.password = Encryption.SHA256(password);

            tcp = new TcpSocket(ip, port);
            tcp.onPacket += onTcpPacketReceived;
            tcp.onDisconnect += onConnectionAbort;

            udp = new UdpSocket(ip, port);
            udp.onPacket += onUdpPacketReceived;
            udp.onDisconnect += onConnectionAbort;

            isConnected = tcp.client.Connected;
            if(!isConnected) {
                _STATE = $"Connection failed. Check ip address and ports";
                Log.Err(Defines.CLIENT, _STATE);
                Disconnect();
            } else {
                Thread connectionThread = new Thread(() => {
                    int cnt = 5;
                    while(tcp.client.Connected && ModManager.clientInstance.myPlayerId == 0 && cnt >= 0) {
                        tcp.QueuePacket(new EstablishConnectionPacket(UserData.GetUserName(), Defines.MOD_VERSION, this.password));
                        cnt--;
                        Thread.Sleep(500);
                    }
                    if(ModManager.clientInstance.myPlayerId == 0) {
                        _STATE = $"Couldn't establish a connection, handshake with server failed after multiple retries.";
                        Log.Err(Defines.CLIENT, _STATE);
                    }
                });
                connectionThread.Name = "Establish Connection Thread";
                connectionThread.Start();
            }
        }

        private void onConnectionAbort() {
            Log.Err(Defines.CLIENT, "Error at connection resulted in disconnect.");
            Disconnect();
        }

        internal void onTcpPacketReceived(NetPacket p) {
            onPacketReceived?.Invoke(p);
            reliableReceive += p.GetData().Length;
        }

        internal void onUdpPacketReceived(NetPacket p) {
            onPacketReceived?.Invoke(p);
            unreliableReceive += p.GetData().Length;
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
