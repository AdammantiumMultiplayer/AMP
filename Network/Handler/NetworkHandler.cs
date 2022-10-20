using AMP.Network.Data;
using AMP.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace AMP.Network.Handler {
    internal class NetworkHandler {

        internal bool isConnected = false;

        internal long unreliableSent = 0;
        internal long reliableSent = 0;
        internal long unreliableReceive = 0;
        internal long reliableReceive = 0;

        internal Action<NetPacket> onPacketReceived;

        internal virtual void Connect() {

        }

        internal virtual void Disconnect() {

        }

        internal virtual void RunCallbacks() { }
        internal virtual void RunLateCallbacks() { }

        internal virtual void SendReliable(NetPacket packet) {

        }

        internal virtual void SendUnreliable(NetPacket packet) {

        }

        internal float GetBandwidthSent() { // Returns kb/s
            float kbs = (unreliableSent + reliableSent) / 1024f;
            
            unreliableSent = 0;
            reliableSent = 0;
            
            return Mathf.Round(kbs * 100) / 100;
        }
        internal float GetBandwidthReceive() { // Returns kb/s
            float kbs = (unreliableReceive + reliableReceive) / 1024f;

            unreliableReceive = 0;
            reliableReceive = 0;
            
            return Mathf.Round(kbs * 100) / 100;
        }
    }
}
