using AMP.Logging;
using Netamite.Network.Packet;

namespace AMP.Extension {
    internal static class NetPacketExtensions {

        internal static void SendToServerReliable(this NetPacket packet) {
            if(packet == null) return;
            ModManager.clientInstance.netclient.SendReliable(packet); 
        }

        internal static void SendToServerUnreliable(this NetPacket packet) {
            if(packet == null) return;
            ModManager.clientInstance.netclient.SendUnreliable(packet);
        }

        internal static void SendToServer(this NetPacket packet) {
            if(packet == null) return;
            ModManager.clientInstance.netclient.Send(packet);
        }
    }
}
