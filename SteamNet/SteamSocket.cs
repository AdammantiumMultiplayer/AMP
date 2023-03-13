using AMP.Data;
using AMP.Logging;
using AMP.Network.Connection;
using AMP.Network.Packets;
using AMP.Performance;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AMP.SteamNet {
    internal class SteamSocket : NetSocket {

        public Action<ulong, NetPacket> onPacketWithId;
        private Dictionary<ulong, List<byte>> packet_buffer = new Dictionary<ulong, List<byte>>();

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
            byte[] data = packet.GetData(true);

            if(SteamIntegration.Instance.mySteamId == target) {
                SteamIntegration.Instance.steamNet.onPacketReceived?.Invoke(packet);
            } else {
                byte attempts = 50;
                while(!SteamNetworking.SendP2PPacket(target, data, (uint) data.Length, mode, channel)) {
                    Thread.Sleep(1);
                    attempts--;
                    if(attempts <= 0) break;
                }
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

                    HandleData((ulong) sender, data);
                    //try {
                    //    NetPacket packet = NetPacket.ReadPacket(data);
                    //
                    //    if(packet == null) continue;
                    //
                    //    onPacketWithId?.Invoke((ulong) sender, packet);
                    //}catch(Exception ex) {
                    //    Log.Err(ex);
                    //}
                }
                Thread.Sleep(1);
            }
        }

        internal void HandleData(ulong sender, byte[] data) {
            if(!packet_buffer.ContainsKey(sender)) {
                packet_buffer.Add(sender, new List<byte>());
            }

            List<byte> buffer = packet_buffer[sender];

            if(buffer == null) return;

            buffer.AddRange(data);

            #if PERFORMANCE_WARNING
            PerformanceError pe = new PerformanceError( errorPrefix: Defines.AMP
                                                      , errorMessage: "Processing of Packets is taking longer than expected, probably a performance issue. " + TYPE + " processing loop is running for {0}ms."
                                                      );
            #endif

            while(buffer.Count > 3) { // At least 3 bytes are needed 2 bytes for length, and 1 byte for the packet type
                short length = BitConverter.ToInt16(new byte[] { buffer[0], buffer[1] }, 0);

                if(length <= 0) {
                    buffer.Clear();
                    break;
                }

                if(buffer.Count >= length + 2) {
                    byte[] packet_data = buffer.GetRange(2, length).ToArray();
                    
                    try {
                        using(NetPacket packet = NetPacket.ReadPacket(packet_data)) {
                            if(packet == null) break;

                            onPacketWithId?.Invoke(sender, packet);
                        }

                        buffer.RemoveRange(0, length + 2);
                    }catch(Exception e) {
                        //Log.Err($"{buffer.Count} {length}\n" + e);
                        Log.Err(e);
                        buffer.RemoveRange(0, length + 2);
                        break;
                    }
                } else {
                    break;
                }

                #if PERFORMANCE_WARNING
                pe.HasPerformanceIssue();
                #endif
            }
        }
    }
}
