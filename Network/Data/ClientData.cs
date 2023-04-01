using AMP.Network.Data.Sync;

namespace AMP.Network.Data {
    public class ClientData {

        public static ClientData SERVER = new ClientData() {
              
        };

        internal bool greeted = false;

        internal PlayerNetworkData playerSync;
    }
}
