using AMP.Logging;
using AMP.Network.Handler;
using System;
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
            runningThread.Start();
        }

        public static void Stop() {
            if(runningThread != null) {
                running = false;
                runningThread.Abort();
                runningThread = null;
            }
        }

        private static void Run() {
            string ip = "127.0.0.1";
            int port = 13698;
            var server = new TcpListener(IPAddress.Parse(ip), port);

            server.Start();

            Log.Debug($"[AMP WebInterface] Server has started on {ip}:{port}, Waiting for a connection…");

            while(running) {
                TcpClient client = server.AcceptTcpClient();
                Log.Debug("[AMP WebInterface] Received connection from browser...");

                new Thread(() => ClientThread(client)).Start();
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
                    Log.Debug("[AMP WebInterface] Connection with browser established.");
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

                        ProcessData(text);
                    } else {
                        Log.Err("[AMP WebInterface] mask bit not set");
                    }
                }
            }
        }

        public static void ProcessData(string text) {
            if(text.StartsWith("join:")) {
                string[] splits = text.Split(':');

                if(ModManager.clientInstance == null) {
                    Log.Debug("[AMP WebInterface] Requested joining " + splits[1] + ":" + splits[2]);

                    if(ModManager.discordGuiManager.enabled) {
                        ModManager.discordGuiManager.enabled = false;
                        ModManager.guiManager.enabled = true;
                        ModManager.guiManager.windowRect = ModManager.discordGuiManager.windowRect;
                    }

                    ModManager.JoinServer(new SocketHandler(splits[1], int.Parse(splits[2])));
                }
            } else {
                Log.Warn("[AMP WebInterface] Invalid request: " + text);
            }
        }
    }
}
