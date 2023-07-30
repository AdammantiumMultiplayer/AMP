using AMP.Logging;
using System;
using ThunderRoad;
using UnityEngine;
using static ThunderRoad.HandleRagdollData;

namespace AMP.Extension {
    internal static class Vector3Extension {

        internal static bool CloserThan(this Vector3 me, Vector3 other, float allowed_distance_squared) {
            return (me.SqDist(other) <= allowed_distance_squared);
        }

        internal static bool FurtherThan(this Vector3 me, Vector3 other, float min_distance_squared) {
            return (me.SqDist(other) >= min_distance_squared);
        }

        internal static float SqDist(this Vector3 me, Vector3 other) {
            var diff = me - other;
            var square_dist = diff.sqrMagnitude;
            return square_dist;
        }

        internal static float Distance(this Vector3 me, Vector3 other) {
            return Vector3.Distance(me, other);
        }

        internal static Vector3 InterpolateTo(this Vector3 me, Vector3 target, ref Vector3 velocity, float smoothTime) {
            return Vector3.SmoothDamp(me, target, ref velocity, smoothTime);
        }

        // Not the nicest solution to interpolate between Eulers, but its good enough
        // ^ Not anymore, function is pretty much useless atm, just keeping it if i need it later, finally changed to quaternions
        internal static Vector3 InterpolateEulerTo(this Vector3 me, Vector3 target, ref Vector3 velocity, float smoothTime) {
            if(Vector3.Angle(me, target) > 0) {
                return new Vector3( Mathf.SmoothDampAngle(me.x, target.x, ref velocity.x, smoothTime)
                                  , Mathf.SmoothDampAngle(me.y, target.y, ref velocity.y, smoothTime)
                                  , Mathf.SmoothDampAngle(me.z, target.z, ref velocity.z, smoothTime)
                                  );
            }
            return target;
        }


        public static Quaternion ConvertToQuaternion(this Vector3 r) {
            return new Quaternion(
                  (float)(Math.Sin(r.z / 2) * Math.Cos(r.y / 2) * Math.Cos(r.x / 2) - Math.Cos(r.z / 2) * Math.Sin(r.y / 2) * Math.Sin(r.x / 2))
                , (float)(Math.Cos(r.z / 2) * Math.Sin(r.y / 2) * Math.Cos(r.x / 2) + Math.Sin(r.z / 2) * Math.Cos(r.y / 2) * Math.Sin(r.x / 2))
                , (float)(Math.Cos(r.z / 2) * Math.Cos(r.y / 2) * Math.Sin(r.x / 2) - Math.Sin(r.z / 2) * Math.Sin(r.y / 2) * Math.Cos(r.x / 2))
                , (float)(Math.Cos(r.z / 2) * Math.Cos(r.y / 2) * Math.Cos(r.x / 2) + Math.Sin(r.z / 2) * Math.Sin(r.y / 2) * Math.Sin(r.x / 2))
            );
        }
    }
}
