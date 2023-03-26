using AMP.Data;
using AMP.Network.Connection;
using AMP.Network.Data.Sync;
using System;
using System.Threading;

namespace AMP.Network.Data {
    public class ClientData {

        public static ClientData SERVER = new ClientData(-1) {
              name = Defines.SERVER
        };

        public long playerId = 1;
        public string name = "Unnamed";

        public long lastTimeSyncStamp = 0;

        internal bool greeted = false;
        internal long last_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        internal NetSocket reliable;
        internal NetSocket unreliable;

        internal PlayerNetworkData playerSync;

        internal Thread disconnectThread = null;

        internal bool isHost {
            get {
                return (ModManager.clientInstance != null && ModManager.clientInstance.myPlayerId == playerId);
            }
        }

        internal ClientData(long playerId) {
            this.playerId = playerId;
        }

        internal void Disconnect() {
            reliable.onDisconnect = null;
            unreliable.onDisconnect = null;

            if(reliable != null) reliable.Disconnect();
            if(unreliable != null) unreliable.Disconnect();
            reliable = null;
            unreliable = null;
        }
    }
}
