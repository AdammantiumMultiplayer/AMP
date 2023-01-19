using AMP.Network.Handler;
using Steamworks;

namespace AMP.SteamNet {
    internal class SteamNet : NetworkHandler {

        public SteamNet() {
            SteamNetworking.AllowP2PPacketRelay(true);
        }


    }
}
