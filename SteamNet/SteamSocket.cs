using AMP.Logging;
using AMP.Network.Connection;
using AMP.Network.Packets;
using Steamworks;
using System;
using System.Threading;

namespace AMP.SteamNet {
    internal class SteamSocket : NetSocket {

        public Action<ulong, NetPacket> onPacketWithId;

        CSteamID target;
        EP2PSend mode;
        int channel;

        public override bool IsConnected => true;

        public SteamSocket(CSteamID target, EP2PSend mode, int channel) {
            this.target  = target;
            this.mode    = mode;
            this.channel = channel;

            StartProcessing();
        }

        internal override void SendPacket(NetPacket packet) {
            byte[] data = packet.GetData();

            if(SteamIntegration.Instance.mySteamId == target) {
                SteamIntegration.Instance.steamNet.onPacketReceived.Invoke(packet);
            } else {
                SteamNetworking.SendP2PPacket(target, data, (uint) data.Length, mode, channel);
            }
        }

        internal override void AwaitData() {
            if(awaitDataThread != null && awaitDataThread.IsAlive) return;
            awaitDataThread = new Thread(ReadSocket);
            awaitDataThread.Name = "SteamSocket Data Read Thread";
            awaitDataThread.Start();
        }

        private void ReadSocket() {
            while(!closing) {
                uint size;
                byte[] data;
                CSteamID sender;
                if(SteamNetworking.IsP2PPacketAvailable(out size, channel)) {
                    data = new byte[size];
                    SteamNetworking.ReadP2PPacket(data, size, out size, out sender, channel);

                    if(size == 0) continue;
                    if(data.Length == 0) continue;
                    
                    bytesReceived += (int) size;

                    try {
                        NetPacket packet = NetPacket.ReadPacket(data);

                        if(packet == null) continue;

                        onPacketWithId?.Invoke((ulong) sender, packet);
                    }catch(Exception ex) {
                        Log.Err(ex);
                    }
                }
                Thread.Sleep(1);
            }
        }
    }
}
