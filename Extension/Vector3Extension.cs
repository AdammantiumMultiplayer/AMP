﻿using System;
using UnityEngine;

namespace AMP.Extension {
    public static class Vector3Extension {

        public static bool CloserThan(this Vector3 me, Vector3 other, float allowed_distance) {
            return (me.Distance(other) <= allowed_distance);
        }

        public static bool FurtherThan(this Vector3 me, Vector3 other, float min_distance) {
            return (me.Distance(other) >= min_distance);
        }

        public static float Distance(this Vector3 me, Vector3 other) {
            return Vector3.Distance(me, other);
        }

        public static Vector3 InterpolateTo(this Vector3 me, Vector3 target, ref Vector3 velocity, float smoothTime) {
            return Vector3.SmoothDamp(me, target, ref velocity, smoothTime);
        }

        // Not the nicest solution to interpolate between Eulers, but its good enough
        // ^ Not anymore, function is pretty much useless atm, just keeping it if i need it later, finally changed to quaternions
        public static Vector3 InterpolateEulerTo(this Vector3 me, Vector3 target, ref Vector3 velocity, float smoothTime) {
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
