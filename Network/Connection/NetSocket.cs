using AMP.Logging;
using AMP.Network.Packets;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AMP.Network.Connection {
    internal class NetSocket {

        internal Action<NetPacket> onPacket;

        private int bytesSent = 0;
        private int bytesReceived = 0;

        private List<byte> packet_buffer = new List<byte>();
        
        private bool handelingData = false;
        internal void HandleData(byte[] data) {
            packet_buffer.AddRange(data);

            if(handelingData) return;

            handelingData = true;
            while(packet_buffer.Count > 3) { // At least 3 bytes are needed 2 bytes for length, and 1 byte for the packet type
                short length = BitConverter.ToInt16(new byte[] { packet_buffer[0], packet_buffer[1] }, 0);

                if(length <= 0) { // TODO: Find a better solution :/
                    packet_buffer.Clear();
                    break;
                }

                if(packet_buffer.Count >= length + 2) {
                    byte[] packet_data = packet_buffer.GetRange(2, length).ToArray();

                    using(NetPacket packet = NetPacket.ReadPacket(packet_data)) {
                        if(packet == null) break;
                        HandleData(packet);
                    }

                    packet_buffer.RemoveRange(0, length + 2);
                } else {
                    break;
                }
            }
            handelingData = false;
        }

        internal void HandleData(NetPacket packet) {
            if(packet == null) return;
            if(onPacket == null) return;
            onPacket.Invoke(packet);
            Interlocked.Add(ref bytesReceived, packet.GetData().Length);
        }

        internal void SendPacket(NetPacket packet) {
            Interlocked.Add(ref bytesSent, packet.GetData().Length);
        }


        public int GetBytesSent() {
            int i = bytesSent;
            bytesSent = 0;
            return i;
        }

        public int GetBytesReceived() {
            int i = bytesReceived;
            bytesReceived = 0;
            return i;
        }
    }
}
