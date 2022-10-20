using System;

namespace AMP.Network.Packets.Attributes {
    [AttributeUsage( AttributeTargets.Field
                    )]
    internal class SyncedVar : Attribute {

        public bool LowPrecision = false;

        public SyncedVar() { }

        public SyncedVar(bool LowPrecision) {
            this.LowPrecision = LowPrecision;
        }

    }
}
