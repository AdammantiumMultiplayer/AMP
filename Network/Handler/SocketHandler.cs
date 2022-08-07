﻿using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Network.Handler {
    internal class SocketHandler : NetworkHandler {

        private string ip;
        private int port;

        public TcpSocket tcp;
        public UdpSocket udp;

        public SocketHandler(string address, int port) {
            this.ip = NetworkUtil.GetIP(address);
            this.port = port;
        }


        public new void Connect() {
            Log.Info($"[Client] Connecting to {ip}:{port}...");
            tcp = new TcpSocket(ip, port);
            tcp.onPacket += onPacketReceived.Invoke;
            udp = new UdpSocket(ip, port);
            udp.onPacket += onPacketReceived.Invoke;

            isConnected = tcp.client.Connected;
            if(!isConnected) {
                Log.Err("[Client] Connection failed. Check ip address and ports.");
                Disconnect();
            }
        }

        public new void Disconnect() {
            isConnected = false;
            if(tcp != null) {
                tcp.SendPacket(PacketWriter.Disconnect(0, "Connection closed"));
                tcp.Disconnect();
            }
            if(udp != null) udp.Disconnect();
            Log.Info("[Client] Disconnected.");
        }

        public new void SendReliable(Packet packet) {

        }

        public new void SendUnreliable(Packet packet) {

        }

    }
}