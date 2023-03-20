using AMP.Data;
using AMP.Logging;
using AMP.Network.Packets;
using AMP.Performance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace AMP.Network.Connection {
    internal class NetSocket {

        public virtual string TYPE => "NetSocket";

        public virtual bool IsConnected => false;

        public Action<NetPacket> onPacket;
        public Action onDisconnect;

        internal int bytesSent = 0;
        internal int bytesReceived = 0;

        private List<byte> packet_buffer = new List<byte>();

        internal bool closing = false;

        internal void HandleData(byte[] data) {
            packet_buffer.AddRange(data);

            #if PERFORMANCE_WARNING
            PerformanceError pe = new PerformanceError( errorPrefix: Defines.AMP
                                                      , errorMessage: "Processing of Packets is taking longer than expected, probably a performance issue. " + TYPE + " processing loop is running for {0}ms."
                                                      );
            #endif

            while(packet_buffer.Count > 3) { // At least 3 bytes are needed 2 bytes for length, and 1 byte for the packet type
                short length = BitConverter.ToInt16(new byte[] { packet_buffer[0], packet_buffer[1] }, 0);

                if(length <= 0) {
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

                #if PERFORMANCE_WARNING
                pe.HasPerformanceIssue();
                #endif
            }
        }

        internal void HandleData(NetPacket packet) {
            if(packet == null) return;
            if(onPacket == null) return;
            try {
                onPacket.Invoke(packet);
            } catch(Exception e) {
                Log.Err(e);
            }
            bytesReceived += packet.GetData().Length;
        }

        internal ConcurrentQueue<NetPacket> processPacketQueue = new ConcurrentQueue<NetPacket>();
        internal void QueuePacket(NetPacket packet) {
            if(packet == null) return;
            if(closing) return;

            processPacketQueue.Enqueue(packet);
        }

        internal void ProcessSendQueue() {
            NetPacket packet;
            #if PERFORMANCE_WARNING
            PerformanceError pe = new PerformanceError( errorPrefix: Defines.AMP
                                                      , errorMessage: "Sending of Packets is taking longer than expected, probably a performance issue. " + TYPE + " sending loop is running for {0}ms."
                                                      );
            #endif
            while(true) {
                #if PERFORMANCE_WARNING
                pe.Reset();
                #endif
                try {
                    while(processPacketQueue.TryDequeue(out packet)) {
                        bytesSent += packet.GetData().Length;

                        if(packet == null) continue;
                        SendPacket(packet);

                        #if PERFORMANCE_WARNING
                        pe.HasPerformanceIssue();
                        #endif
                    }
                    if(ModManager.safeFile.modSettings.lowLatencyMode) Thread.Yield();
                    else Thread.Sleep(1);
                }catch(ThreadAbortException) { 
                    return;
                }
            }
        }

        internal virtual void SendPacket(NetPacket packet) { }

        public virtual void Disconnect() {
            if(!closing) {
                closing = true;
                if(onDisconnect != null) onDisconnect.Invoke();
                StopProcessing();
            }
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


        private Thread processDataThread = null;
        internal void StartProcessData() {
            if(processDataThread != null && processDataThread.IsAlive) return;
            processDataThread = new Thread(ProcessSendQueue);
            processDataThread.Name = "NetSocket Data Send Thread";
            processDataThread.Start();
        }

        internal void StopProcessData() {
            if(processDataThread == null) return;
            processDataThread.Abort();
        }

        protected Thread awaitDataThread = null;
        internal void StartAwaitData() {
            /*
            if(awaitDataThread != null && awaitDataThread.IsAlive) return;
            awaitDataThread = new Thread(AwaitData);
            awaitDataThread.Name = "NetSocket Data Read Thread";
            awaitDataThread.Start();
            */
            AwaitData();
        }

        internal void StopAwaitData() {
            if(awaitDataThread == null) return;
            awaitDataThread.Abort();
        }

        internal void StartProcessing() {
            StartAwaitData();
            StartProcessData();
        }

        internal void StopProcessing(bool flush = true) {
            if(flush) {
                int tries = 100;
                while(processPacketQueue.Count > 0 && tries > 100) {
                    Thread.Sleep(10);
                    tries--;
                }
                Thread.Sleep(100);
            }

            StopAwaitData();
            StopProcessData();
        }

        internal virtual void AwaitData() { }
    }
}
