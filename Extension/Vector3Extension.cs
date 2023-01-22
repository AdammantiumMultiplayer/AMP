using UnityEngine;

namespace AMP.Extension {
    internal static class Vector3Extension {

        internal static bool Approximately(this Vector3 me, Vector3 other, float allowed_distance_squared) {
            return (me.SQ_DIST(other) <= allowed_distance_squared);
        }

        internal static bool ApproximatelyMin(this Vector3 me, Vector3 other, float min_distance_squared) {
            return (me.SQ_DIST(other) >= min_distance_squared);
        }

        internal static float SQ_DIST(this Vector3 me, Vector3 other) {
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
            return new Vector3( Mathf.SmoothDampAngle(me.x, target.x, ref velocity.x, smoothTime)
                              , Mathf.SmoothDampAngle(me.y, target.y, ref velocity.y, smoothTime)
                              , Mathf.SmoothDampAngle(me.z, target.z, ref velocity.z, smoothTime)
                              );
        }

    }
}
