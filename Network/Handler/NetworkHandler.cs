using AMP.Network.Packets;
using AMP.SteamNet;
using Steamworks;
using System;

namespace AMP.Network.Handler {
    internal class NetworkHandler {

        internal virtual string TYPE => "NONE";

        internal virtual string STATE => "UNKNOWN";

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

        internal static bool UseJoinSecret(string key) {
            string[] splits = key.Split(':');

            switch(splits[0]) {
                case "SOCKET":
                    if(splits.Length >= 3) {
                        string address = splits[1];
                        string port = splits[2];

                        SocketHandler socketHandler = new SocketHandler(address, int.Parse(port));
                        string password = "";
                        if(splits.Length >= 4) {
                            password = splits[3];
                        }
                        ModManager.JoinServer(socketHandler, password);
                        return true;
                    }
                    break;

                case "STEAM":
                    if(splits.Length >= 2) {
                        SteamIntegration.Instance.steamNet = new SteamNetHandler();
                        SteamIntegration.Instance.steamNet.JoinLobby((CSteamID) ulong.Parse(splits[1]));

                        return true;
                    }
                    break;

                default: break;
            }
            return false;
        }

        internal virtual string GetJoinSecret() { return null; }
    }
}
