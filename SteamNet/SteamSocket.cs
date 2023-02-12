﻿using AMP.Logging;
using AMP.Network.Connection;
using AMP.Network.Packets;
using Steamworks;
using System.Diagnostics;
using System.Threading;

namespace AMP.SteamNet {
    internal class SteamSocket : NetSocket {

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
            while(true) {
                uint size;
                byte[] data;
                CSteamID sender;
                if(SteamNetworking.IsP2PPacketAvailable(out size, channel)) {
                    data = new byte[size];
                    SteamNetworking.ReadP2PPacket(data, size, out size, out sender, channel);

                    HandleData(data);
                }
                Thread.Sleep(1);
            }
        }
    }
}
