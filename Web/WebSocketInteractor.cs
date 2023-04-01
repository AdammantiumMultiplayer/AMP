using AMP.Data;
using AMP.Logging;
using AMP.Overlay;
using Netamite.Network.Packet.Implementations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AMP.Web {
    public class WebSocketInteractor {
        private static bool running = true;
        private static Thread runningThread;

        public static void Start() {
            if(runningThread != null && running) return;
            
            running = true;
            runningThread = new Thread(Run);
            runningThread.Name = "WebIntegration";
            runningThread.Start();
        }

        public static void Stop() {
            if(runningThread != null) {
                running = false;
                runningThread.Abort();
                runningThread = null;
            }
        }

        private static List<TcpClient> _clients = new List<TcpClient>();
        private static void Run() {
            string ip = "127.0.0.1";
            int port = 13698;
            var server = new TcpListener(IPAddress.Parse(ip), port);

            server.Start();

            Log.Debug(Defines.WEB_INTERFACE, $"Server has started on {ip}:{port}, Waiting for a connection…");

            while(running) {
                TcpClient client = server.AcceptTcpClient();
                Log.Debug(Defines.WEB_INTERFACE, "Received connection from browser...");

                Thread clientThread = new Thread(() => ClientThread(client));
                clientThread.Name = "WebIntegration Client";
                clientThread.Start();
                _clients.Add(client);
            }
        }

        private static void ClientThread(TcpClient client) {
            NetworkStream stream = client.GetStream();

            // enter to an infinite cycle to be able to handle every change in stream
            while(client.Connected) {
                while(!stream.DataAvailable) ;
                while(client.Available < 3) ; // match against "get"

                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, client.Available);
                string s = Encoding.UTF8.GetString(bytes);

                if(Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase)) {
                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);
                    Log.Debug(Defines.WEB_INTERFACE, "Connection with browser established.");
                } else {
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
                    int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                        offset = 2;
                    ulong msglen = (ulong)(bytes[1] & 0b01111111);

                    if(msglen == 126) {
                        // bytes are reversed because websocket will print them in Big-Endian, whereas
                        // BitConverter will want them arranged in little-endian on windows
                        msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                        offset = 4;
                    } else if(msglen == 127) {
                        // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                        // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                        // websocket frame available through client.Available).
                        msglen = BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] }, 0);
                        offset = 10;
                    }

                    if(msglen == 0) {
                        Console.WriteLine("msglen == 0");
                    } else if(mask) {
                        byte[] decoded = new byte[msglen];
                        byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                        offset += 4;

                        for(ulong i = 0; i < msglen; ++i)
                            decoded[i] = (byte)(bytes[offset + (int)i] ^ masks[i % 4]);

                        string text = Encoding.UTF8.GetString(decoded);

                        string response = ProcessData(client, text);
                        if(!string.IsNullOrEmpty(response)) {
                            SendMessageToClient(client, response);
                        }
                    } else {
                        Log.Err(Defines.WEB_INTERFACE, "mask bit not set");
                    }
                }
            }
        }

        public static string ProcessData(TcpClient client, string text) {
            if(text.StartsWith("\u0003")) {
                Log.Debug(Defines.WEB_INTERFACE, "Connection with browser has been closed");
            } else if(text.StartsWith("join:")) {
                string[] splits = text.Split(':');

                if(ModManager.clientInstance == null) {
                    Log.Debug(Defines.WEB_INTERFACE, "Requested joining " + splits[1] + ":" + splits[2]);

                    //if(ModManager.steamGuiManager.enabled) {
                    //    ModManager.steamGuiManager.enabled = false;
                    //    ModManager.guiManager.enabled = true;
                    //    ModManager.guiManager.windowRect = ModManager.discordGuiManager.windowRect;
                    //}

                    ModManager.guiManager.join_ip = splits[1];
                    ModManager.guiManager.join_port = splits[2];
                    if(splits.Length > 3) {
                        ModManager.guiManager.join_password = splits[3];
                    } else {
                        ModManager.guiManager.join_password = "";
                    }

                    GUIManager.JoinServer(ModManager.guiManager.join_ip, ModManager.guiManager.join_port, ModManager.guiManager.join_password);

                    if(ModManager.clientInstance == null) {
                        return $"ERROR|Connection to server {ModManager.guiManager.join_ip}:{ModManager.guiManager.join_port} failed.";
                    }
                }
            } else {
                Log.Warn(Defines.WEB_INTERFACE, "Invalid request: " + text);
            }
            return "";
        }

        private static void SendMessageToClient(TcpClient client, string msg) {
            NetworkStream stream = client.GetStream();
            Queue<string> que = new Queue<string>(msg.SplitInGroups(125));
            int len = que.Count;

            while(que.Count > 0) {
                var header = GetHeader(
                    que.Count > 1 ? false : true,
                    que.Count == len ? false : true
                );

                byte[] list = Encoding.UTF8.GetBytes(que.Dequeue());
                header = (header << 7) + list.Length;
                stream.Write(IntToByteArray((ushort)header), 0, 2);
                stream.Write(list, 0, list.Length);
            }
        }

        protected static int GetHeader(bool finalFrame, bool contFrame) {
            int header = finalFrame ? 1 : 0;//fin: 0 = more frames, 1 = final frame
            header = (header << 1) + 0;//rsv1
            header = (header << 1) + 0;//rsv2
            header = (header << 1) + 0;//rsv3
            header = (header << 4) + (contFrame ? 0 : 1);//opcode : 0 = continuation frame, 1 = text
            header = (header << 1) + 0;//mask: server -> client = no mask

            return header;
        }

        protected static byte[] IntToByteArray(ushort value) {
            var ary = BitConverter.GetBytes(value);
            if(BitConverter.IsLittleEndian) {
                Array.Reverse(ary);
            }
            return ary;
        }

        internal static void InvokeError(ErrorPacket errorPacket) {
            foreach(TcpClient client in _clients) {
                if(client.Connected) {
                    try {
                        SendMessageToClient(client, "ERROR|" + errorPacket.message);
                    } catch { }
                }
            }
        }
    }

    public static class XLExtensions {
        public static IEnumerable<string> SplitInGroups(this string original, int size) {
            var p = 0;
            var l = original.Length;
            while(l - p > size) {
                yield return original.Substring(p, size);
                p += size;
            }
            yield return original.Substring(p);
        }
    }
}
