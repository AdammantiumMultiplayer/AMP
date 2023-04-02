using AMP.Network.Data.Sync;
using Netamite.Server.Data;

namespace AMP.Network.Data {
    public class ClientData {

        public static ClientData SERVER = new ClientData(null) {
              
        };

        internal bool greeted = false;

        internal PlayerNetworkData playerSync;

        internal ClientInformation clientInformation;

        public ClientData(ClientInformation clientInformation) {
            this.clientInformation = clientInformation;
        }
    }
}
