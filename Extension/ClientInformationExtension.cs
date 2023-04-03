using AMP.Network.Client;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using Netamite.Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Extension {
    internal static class ClientInformationExtension {

        public static ClientData GetData(this ClientInformation client) {
            ClientData cd;
            if(!ModManager.serverInstance.clientData.ContainsKey(client.ClientId)) {
                cd = new ClientData(client);
                ModManager.serverInstance.clientData.TryAdd(client.ClientId, cd);
            } else {
                cd = ModManager.serverInstance.clientData[client.ClientId];
            }
            if(cd.playerSync == null) {
                cd.playerSync = new PlayerNetworkData() { clientId = client.ClientId };
            }
            return cd;
        }        

    }
}
