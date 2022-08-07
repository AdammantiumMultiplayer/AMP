using AMP.Network.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace AMP.Network.Handler {
    public class NetworkHandler {

        public bool isConnected = false;

        public UnityAction<Packet> onPacketReceived;

        public virtual void Connect() {

        }

        public virtual void Disconnect() {

        }

        public virtual void RunCallbacks() {
            
        }

        public virtual void SendReliable(Packet packet) {

        }

        public virtual void SendUnreliable(Packet packet) {

        }
    }
}
