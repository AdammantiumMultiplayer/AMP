using AMP.Network.Packets;
using System;

namespace AMP.Network.Handler {
    internal class NetworkHandler {

        internal bool isConnected = false;

        internal long unreliableSent = 0;
        internal long reliableSent = 0;
        internal long unreliableReceive = 0;
        internal long reliableReceive = 0;

        internal Action<NetPacket> onPacketReceived;

        internal virtual void Connect(string password = "") {

        }

        internal virtual void Disconnect() {

        }

        internal virtual void RunCallbacks() { }
        internal virtual void RunLateCallbacks() { }

        internal virtual void SendReliable(NetPacket packet) {

        }

        internal virtual void SendUnreliable(NetPacket packet) {

        }

        internal long GetBytesSent() {
            long bytesSent = unreliableSent + reliableSent;
            
            unreliableSent = 0;
            reliableSent = 0;
            
            return bytesSent;
        }

        internal long GetBytesReceive() {
            long bytesReceive = unreliableReceive + reliableReceive;

            unreliableReceive = 0;
            reliableReceive = 0;
            
            return bytesReceive;
        }

        internal virtual string GetJoinSecret() { return null; }
    }
}
