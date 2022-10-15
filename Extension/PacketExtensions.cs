using AMP.Network.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Extension {
    internal static class PacketExtensions {

        internal static void SendToServerReliable(this Packet packet) {
            if(packet == null) return;
            ModManager.clientInstance.nw.SendReliable(packet);
        }

        internal static void SendToServerUnreliable(this Packet packet) {
            if(packet == null) return;
            ModManager.clientInstance.nw.SendUnreliable(packet);
        }

    }
}
