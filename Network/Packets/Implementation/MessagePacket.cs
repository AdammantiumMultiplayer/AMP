using AMP.Network.Packets.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.MESSAGE)]
    internal class MessagePacket {
        [SyncedVar] public string message;
    }
}
