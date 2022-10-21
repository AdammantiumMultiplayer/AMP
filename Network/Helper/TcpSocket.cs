﻿using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Packets;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

namespace AMP.Network.Helper {
    internal class TcpSocket {

        private TcpClient _client;
        public TcpClient client {
            get { return _client; }
        }

        private NetworkStream _stream;
        public NetworkStream stream {
            get { return _stream; }
        }

        public static char packet_end_indicator = (char) 4;
        public static int transmission_bits = 1024;
        private byte[] buffer = new byte[0];
        private List<byte> packet_buffer = new List<byte>();

        public bool IsConnected {
            get {
                try {
                    if(client != null && client.Client != null && client.Client.Connected && stream.CanRead) {
                        /* pear to the documentation on Poll:
                         * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                         * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                         * -or- true if data is available for reading; 
                         * -or- true if the connection has been closed, reset, or terminated; 
                         * otherwise, returns false
                         */

                        // Detect if client disconnected
                        if(client.Client.Poll(0, SelectMode.SelectRead)) {
                            byte[] buff = new byte[1];
                            if(client.Client.Receive(buff, SocketFlags.Peek) == 0) {
                                // Client disconnected
                                return false;
                            } else {
                                return true;
                            }
                        }

                        return true;
                    } else {
                        return false;
                    }
                } catch {
                    return false;
                }
            }
        }

        public event Action<NetPacket> onPacket;

        public int packetsSent = 0;
        public int packetsReceived = 0;

        public TcpSocket(TcpClient client) {
            _client = client;

            client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            client.ReceiveBufferSize = transmission_bits;
            client.SendBufferSize = transmission_bits;
            _stream = client.GetStream();

            buffer = new byte[transmission_bits];
            stream.BeginRead(buffer, 0, transmission_bits, ReceiveCallback, null);
        }

        public TcpSocket(string ip, int port) {
            _client = new TcpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            client.ReceiveBufferSize = transmission_bits;
            client.SendBufferSize = transmission_bits;

            buffer = new byte[transmission_bits];

            IAsyncResult result = client.BeginConnect(ip, port, ConnectRequestCallback, client);
            // Begin timeout wait
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));
            // If timed out
            if(!success) {
                // End connection
                try {
                    client.EndConnect(result);
                }catch(Exception e) { Log.Err(e); }
                Log.Err("Could not connect to server, timed out");
                return;
            }
        }

        private void ConnectRequestCallback(IAsyncResult _result) {
            client.EndConnect(_result);
            // If the socket is already connected then stop
            if(!client.Connected) {
                return;
            }
            _stream = client.GetStream();

            stream.BeginRead(buffer, 0, transmission_bits, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult _result) {
            try {
                int bytesRead = stream.EndRead(_result);
                if(bytesRead <= 0) {
                    Disconnect();
                    return;
                }

                byte[] data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);
                HandleData(data);

                stream.BeginRead(buffer, 0, transmission_bits, ReceiveCallback, null);
            } catch(SocketException e) {
                Disconnect();
                Log.Err($"Error receiving TCP data: {e}");
            }
        }


        private void HandleData(byte[] data) {
            packet_buffer.AddRange(data);

            Dispatcher.Enqueue(() => {
                while(true) {
                    if(packet_buffer.Count < 2) return;
                    short length = BitConverter.ToInt16(new byte[] { packet_buffer[0], packet_buffer[1] }, 0);

                    if(buffer.Length - 2 >= length) {
                        byte[] packet_data = packet_buffer.GetRange(2, length).ToArray();

                        using(NetPacket packet = NetPacket.ReadPacket(packet_data)) {
                            if(packet == null) return;
                            onPacket.Invoke(packet);
                            packetsReceived++;
                        }

                        packet_buffer.RemoveRange(0, length + 2);
                    }
                }
            });
        }


        public void SendPacket(NetPacket packet) {
            if(packet == null) return;

            try {
                if(client != null) {
                    // TODO: Handle Disconnect on SocketException
                    List<byte> list = packet.GetData().ToList();
                    list.InsertRange(0, BitConverter.GetBytes((short) list.Count));

                    byte[] data = list.ToArray();

                    stream.Write(data, 0, data.Length);
                    packetsSent++;
                }
            } catch(Exception e) {
                Log.Err($"Error sending data to player via TCP: {e}");
            }
        }

        public int GetPacketsSent() {
            int i = packetsSent;
            packetsSent = 0;
            return i;
        }

        public int GetPacketsReceived() {
            int i = packetsReceived;
            packetsReceived = 0;
            return i;
        }

        public void Disconnect() {
            if(client != null) client.Close();
            if(stream != null) stream.Close();
            _stream = null;
            _client = null;
        }
    }
}
