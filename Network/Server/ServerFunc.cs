using AMP.Extension;
using AMP.Network.Data;
using UnityEngine;

namespace AMP.Network.Server {
    internal class ServerFunc {

        public static ClientData GetClosestPlayerTo(Vector3 target, float distance = 1000f, float threshold = 0f) {
            if(ModManager.serverInstance == null) return null;
            if(ModManager.serverInstance.clients.Count == 0) return null;

            ClientData clientData = null;
            foreach(ClientData cd in ModManager.serverInstance.clients.Values) {
                float dist = cd.playerSync.position.SQ_DIST(target);
                if(dist < distance - (distance / 100 * threshold)) {
                    distance = dist;
                    clientData = cd;
                }
            }

            return clientData;
        }
    }
}
