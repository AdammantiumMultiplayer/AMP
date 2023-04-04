using AMP.Data;
using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Implementations;
#if DEBUG_SELF
#endif

namespace AMP.Network.Client {
    internal class Client {
        internal bool allowTransmission = false;

        internal NetamiteClient netclient;

        public ServerInfoPacket serverInfo = new ServerInfoPacket();

        internal Client(NetamiteClient netclient) {
            this.netclient = netclient;

            netclient.OnDataReceived += OnPacket;
        }

        internal void Connect(string password) {
            netclient.Connect(password);
        }

        internal void OnPacket(NetPacket p) {
            Dispatcher.Enqueue(() => {
                OnPacketMainThread(p);
            });
        }

        private void OnPacketMainThread(NetPacket p) {
            if(p == null) return;

            byte type = p.getPacketType();

            //Log.Warn("CLIENT", type);

            switch(type) {
                #region Connection handling and stuff
                case (byte) Netamite.Network.Packet.PacketType.DISCONNECT:
                    DisconnectPacket disconnectPacket = (DisconnectPacket) p;

                    if(netclient.ClientId == disconnectPacket.ClientId) { // Should never really happen, and be handled by the netamite onDisconnect Event
                        Log.Info(Defines.CLIENT, $"Disconnected: " + disconnectPacket.Reason);
                        ModManager.StopClient();
                    } else {
                        if(ModManager.clientSync.syncData.players.ContainsKey(disconnectPacket.ClientId)) {
                            PlayerNetworkData ps = ModManager.clientSync.syncData.players[disconnectPacket.ClientId];
                            ModManager.clientSync.LeavePlayer(ps);
                            Log.Info(Defines.CLIENT, $"{ps.name} disconnected: " + disconnectPacket.Reason);
                        }
                    }
                    break;
                #endregion

                default: break;
            }
        }

        internal void Disconnect() {
            if(netclient != null) {
                netclient.OnDataReceived -= OnPacket;
                netclient.Disconnect();
                netclient = null;
            }
        }

        internal void StartSync() {
            ModManager.clientSync.StartThreads();
        }
    }
}
