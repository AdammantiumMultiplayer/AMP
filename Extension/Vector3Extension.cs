using UnityEngine;

namespace AMP.Extension {
    internal static class Vector3Extension {

        //public static bool Approximately(this Vector3 me, Vector3 other, float allowedDifference) {
        //    var dx = me.x - other.x;
        //    if(Mathf.Abs(dx) > allowedDifference)
        //        return false;
        //
        //    var dy = me.y - other.y;
        //    if(Mathf.Abs(dy) > allowedDifference)
        //        return false;
        //
        //    var dz = me.z - other.z;
        //
        //    return Mathf.Abs(dz) >= allowedDifference;
        //}

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

    }
}
