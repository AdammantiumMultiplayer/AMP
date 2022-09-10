using AMP.Network.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace AMP.Network.Handler {
    public class NetworkHandler {

        public bool isConnected = false;

        public long unreliableSent = 0;
        public long reliableSent = 0;
        public long unreliableReceive = 0;
        public long reliableReceive = 0;

        public Action<Packet> onPacketReceived;

        public virtual void Connect() {

        }

        public virtual void Disconnect() {

        }

        public virtual void RunCallbacks() { }
        public virtual void RunLateCallbacks() { }

        public virtual void SendReliable(Packet packet) {

        }

        public virtual void SendUnreliable(Packet packet) {

        }

        public float GetBandwidthSent() { // Returns kb/s
            float kbs = (unreliableSent + reliableSent) / 1024f;
            
            unreliableSent = 0;
            reliableSent = 0;
            
            return Mathf.Round(kbs * 100) / 100;
        }
        public float GetBandwidthReceive() { // Returns kb/s
            float kbs = (unreliableReceive + reliableReceive) / 1024f;

            unreliableReceive = 0;
            reliableReceive = 0;
            
            return Mathf.Round(kbs * 100) / 100;
        }
    }
}
