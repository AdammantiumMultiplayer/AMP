using AMP.Network.Connection;
using AMP.Network.Packets;
using Steamworks;

namespace AMP.SteamNet {
    internal class SteamSocket : NetSocket {

        CSteamID target;
        EP2PSend mode;
        int channel;

        public SteamSocket(CSteamID target, EP2PSend mode, int channel) {
            this.target  = target;
            this.mode    = mode;
            this.channel = channel;
        }

        internal override void SendPacket(NetPacket packet) {
            byte[] data = packet.GetData();
            SteamNetworking.SendP2PPacket(target, data, (uint) data.Length, mode, channel);
        }

        internal override void AwaitData() {
            uint size;
            byte[] data;
            CSteamID sender;
            if(SteamNetworking.IsP2PPacketAvailable(out size, channel)) {
                data = new byte[size];
                SteamNetworking.ReadP2PPacket(data, size, out size, out sender, channel);

                if(sender == target) {
                    
                } else {
                    HandleData(data);
                }
            }
        }
    }
}
