using AMP.Data;
using System;
using UnityEngine;

namespace AMP.Network.Data.Sync {
    public class NetworkData {

        public long dataTimestamp = 0;

        public static long GetDataTimestamp() {
            if(ModManager.clientInstance != null) {
                return ModManager.clientInstance.netclient.FixedTimestamp;
            }
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public void RecalculateDataTimestamp() {
            dataTimestamp = GetDataTimestamp();
        }

        public static float GetCompensationFactor(long originalDataTimestamp) {
            // 0.001f because we are using Milliseconds
            float factor = 0.001f * (GetDataTimestamp() - originalDataTimestamp + Config.LATENCY_COMP_ADDITION);
            factor = Mathf.Min(factor, Config.MAX_LATENCY_COMP_FACTOR);
            return factor;
        }


        public static float Compensate(float value, float velocity, float compensationFactor) {
            return value + (velocity * compensationFactor);
        }

        public static Vector3 Compensate(Vector3 value, Vector3 velocity, float compensationFactor) {
            return value + (velocity.normalized * compensationFactor);
        }

        public static Quaternion Compensate(Quaternion value, Vector3 velocity, float compensationFactor) {
            return Quaternion.Euler(value.eulerAngles + (velocity.normalized * compensationFactor));
        }

    }
}
