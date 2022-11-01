using AMP.Network.Packets;

namespace AMP.Extension {
    internal static class NetPacketExtensions {

        internal static void SendToServerReliable(this NetPacket packet) {
            if(packet == null) return;
            ModManager.clientInstance.nw.SendReliable(packet);
        }

        internal static void SendToServerUnreliable(this NetPacket packet) {
            if(packet == null) return;
            ModManager.clientInstance.nw.SendUnreliable(packet);
        }

    }
}
