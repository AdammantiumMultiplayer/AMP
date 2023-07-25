using AMP.Extension;
using AMP.Network.Data;
using UnityEngine;

namespace AMP.Network.Server {
    internal class ServerFunc {

        public static ClientData GetClosestPlayerTo(Vector3 target, float distance = 1000f, float threshold = 0f) {
            if(ModManager.serverInstance == null) return null;
            if(ModManager.serverInstance.clientData.Count == 0) return null;

            ClientData clientData = null;
            foreach(ClientData cd in ModManager.serverInstance.clientData.Values) {
                float dist = cd.player.position.SqDist(target);
                if(dist < distance - (distance / 100 * threshold)) {
                    distance = dist;
                    clientData = cd;
                }
            }

            return clientData;
        }
    }
}
