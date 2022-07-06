using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Extension {
    public static class Vector3Extension {

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

        public static bool Approximately(this Vector3 me, Vector3 other, float allowed_distance_squared) {
            var diff = me - other;
            var square_dist = diff.sqrMagnitude;
            return (square_dist <= allowed_distance_squared);
        }

        public static float Distance(this Vector3 me, Vector3 other) {
            return Vector3.Distance(me, other);
        }

    }
}
