using System;
using UnityEngine;

/*
Copyright 2016 Max Kaufmann (max.kaufmann@gmail.com)
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace AMP.Extension {
    public static class QuaternionExtension {

        public static Quaternion SmoothDamp(this Quaternion rot, Quaternion target, ref Vector3 velocity, float time) {
            Vector3 from = rot.eulerAngles;
            Vector3 to = target.eulerAngles;

            return Quaternion.Euler(
                Mathf.SmoothDampAngle(from.x, to.x, ref velocity.x, time),
                Mathf.SmoothDampAngle(from.y, to.y, ref velocity.y, time),
                Mathf.SmoothDampAngle(from.z, to.z, ref velocity.z, time)
            );
        }


        internal static Quaternion InterpolateTo(this Quaternion me, Quaternion target, ref float velocity, float smoothTime) {
            return SmoothDamp(me, target, ref velocity, smoothTime);
        }

        public static Quaternion SmoothDamp(this Quaternion rot, Quaternion target, ref float velocity, float smoothTime) {
            float delta = Quaternion.Angle(rot, target);
            if(delta > 0f) {
                float t = Mathf.SmoothDampAngle(delta, 0.0f, ref velocity, smoothTime);

                if(t < 0) t = 0;
                t = 1.0f - (t / delta);

                return Quaternion.Slerp(rot, target, t);
            }
            return target;
        }

        /*
        public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time) {
            if(Time.deltaTime < Mathf.Epsilon) return rot;
            // account for double-cover
            var Dot = Quaternion.Dot(rot, target);
            var Multi = Dot > 0f ? 1f : -1f;
            target.x *= Multi;
            target.y *= Multi;
            target.z *= Multi;
            target.w *= Multi;
            // smooth damp (nlerp approx)
            var Result = new Vector4(
                Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
                Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
                Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
                Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
            ).normalized;

            // ensure deriv is tangent
            var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
            deriv.x -= derivError.x;
            deriv.y -= derivError.y;
            deriv.z -= derivError.z;
            deriv.w -= derivError.w;

            return new Quaternion(Result.x, Result.y, Result.z, Result.w);
        }
        */

        public static Vector3 ConvertToEuler(this Quaternion q) {
            float t0 = 2f * (q.w * q.x + q.y * q.z);
            float t1 = 1f - 2f * (q.x * q.x + q.y * q.y);
            float roll = (float) Math.Atan2(t0, t1);
            float t2 = 2f * (q.w * q.y - q.z * q.x);
            t2 = t2 >  1 ? 1f : t2;
            t2 = t2 < -1 ? -1f : t2;
            float pitch = (float) Math.Asin(t2);
            float t3 = 2f * (q.w * q.z + q.x * q.y);
            float t4 = 1f - 2f * (q.y * q.y + q.z * q.z);
            float yaw = (float) Math.Atan2(t3, t4);
            return new Vector3(yaw, pitch, roll);
        }
    }
}
