using AMP.Logging;
using AMP.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace AMP.Network.Connection {
    internal class TcpSocket : NetSocket {

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
            if(stream == null) return;
            if(buffer == null) return;
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
            } catch(ObjectDisposedException) { }
        }

        public new void SendPacket(NetPacket packet) {
            if(packet == null) return;
            base.SendPacket(packet);
            try {
                if(client != null) {
                    byte[] data = packet.GetData(true);

                    stream.WriteAsync(data, 0, data.Length);
                }
            } catch(Exception e) {
                Log.Err($"Error sending data to player via TCP: {e}");
            }
        }

        public void Disconnect() {
            if(client != null && stream != null) {
                stream.Flush();
                Thread.Sleep(1000);
            }

            if(client != null) client.Dispose();
            if(stream != null) stream.Dispose();
            _stream = null;
            _client = null;

            onPacket = null;
        }
    }
}
