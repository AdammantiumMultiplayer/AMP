using AMP.Network.Data;
using AMP.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Network.Helper {
    public class TcpSocket {

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
        private byte[] buffer;
        private Packet receivedData = new Packet();

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

        public event Action<Packet> onPacket;

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
                client.EndConnect(result);
                Debug.LogError("Could not connect to server, timed out");
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

                receivedData.Reset(HandleData(data));
                stream.BeginRead(buffer, 0, transmission_bits, ReceiveCallback, null);
            } catch(Exception e) {
                Disconnect();
                Debug.Log($"Error receiving TCP data: {e}");
            }
        }

        private bool HandleData(byte[] _data) {
            // Initialises the packet length variable
            int packetLength = 0;

            receivedData.SetBytes(_data);
            // If length - read position is above or equal to 4
            if(receivedData.UnreadLength() >= 4) {
                // Get packet length
                packetLength = receivedData.ReadInt();
                // If packet is empty
                if(packetLength <= 0) {
                    return true;
                }
            }

            // Begin reading the packet
            while(packetLength > 0 && packetLength <= receivedData.UnreadLength()) {
                byte[] _packetBytes = receivedData.ReadBytes(packetLength);
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    using(Packet packet = new Packet(_packetBytes)) {
                        onPacket.Invoke(packet);
                    }
                });

                packetLength = 0;
                if(receivedData.UnreadLength() >= 4) {
                    packetLength = receivedData.ReadInt();
                    if(packetLength <= 0) {
                        return true;
                    }
                }
            }

            if(packetLength <= 1) {
                return true;
            }

            return false;
        }

        public void SendPacket(Packet packet) {
            packet.WriteLength();
            try {
                if(client != null) {
                    stream.Write(packet.ToArray(), 0, packet.Length());
                }
            } catch(Exception e) {
                Debug.Log($"Error sending data to player via TCP: {e}");
            }
        }

        public void Disconnect() {
            if(client != null) client.Close();
            if(stream != null) stream.Close();
            _stream = null;
            _client = null;
        }
    }
}
