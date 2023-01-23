using System;
using System.Collections.Generic;
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


        internal static Quaternion InterpolateTo(this Quaternion me, Quaternion target, float smoothTime) {
            float angle = Quaternion.Angle(me, target) * 6f;
            return Quaternion.RotateTowards(me, target, angle / smoothTime * Time.deltaTime);
        }


        /// <summary>
        /// Used when compressing float values, where the decimal portion of the floating point value
        /// is multiplied by this number prior to storing the result in an Int16. Doing this allows 
        /// us to retain five decimal places, which for many purposes is more than adequate.
        /// </summary>
        private const float FLOAT_PRECISION_MULT = 10000f;

        public static byte[] Compress(this Quaternion rot) {
            byte maxIndex = 0;
            float maxValue = float.MinValue;
            float sign = 1f;

            // Determine the index of the largest (absolute value) element in the Quaternion.
            // We will transmit only the three smallest elements, and reconstruct the largest
            // element during decoding. 
            for(int i = 0; i < 4; i++) {
                float element = rot[i];
                float abs = Mathf.Abs(rot[i]);
                if(abs > maxValue) {
                    // We don't need to explicitly transmit the sign bit of the omitted element because you 
                    // can make the omitted element always positive by negating the entire quaternion if 
                    // the omitted element is negative (in quaternion space (x,y,z,w) and (-x,-y,-z,-w) 
                    // represent the same rotation.), but we need to keep track of the sign for use below.
                    sign = (element < 0) ? -1 : 1;

                    // Keep track of the index of the largest element
                    maxIndex = (byte)i;
                    maxValue = abs;
                }
            }

            // If the maximum value is approximately 1f (such as Quaternion.identity [0,0,0,1]), then we can 
            // reduce storage even further due to the fact that all other fields must be 0f by definition, so 
            // we only need to send the index of the largest field.
            if(Mathf.Approximately(maxValue, 1f)) {
                // Again, don't need to transmit the sign since in quaternion space (x,y,z,w) and (-x,-y,-z,-w) 
                // represent the same rotation. We only need to send the index of the single element whose value
                // is 1f in order to recreate an equivalent rotation on the receiver.
                return new byte[]{ (byte)(maxIndex + 4) };
            }

            short a;
            short b;
            short c;

            // We multiply the value of each element by QUAT_PRECISION_MULT before converting to 16-bit integer 
            // in order to maintain precision. This is necessary since by definition each of the three smallest 
            // elements are less than 1.0, and the conversion to 16-bit integer would otherwise truncate everything 
            // to the right of the decimal place. This allows us to keep five decimal places.

            if(maxIndex == 0) {
                a = (short) (rot.y * sign * FLOAT_PRECISION_MULT);
                b = (short) (rot.z * sign * FLOAT_PRECISION_MULT);
                c = (short) (rot.w * sign * FLOAT_PRECISION_MULT);
            } else if(maxIndex == 1) {
                a = (short) (rot.x * sign * FLOAT_PRECISION_MULT);
                b = (short) (rot.z * sign * FLOAT_PRECISION_MULT);
                c = (short) (rot.w * sign * FLOAT_PRECISION_MULT);
            } else if(maxIndex == 2) {
                a = (short) (rot.x * sign * FLOAT_PRECISION_MULT);
                b = (short) (rot.y * sign * FLOAT_PRECISION_MULT);
                c = (short) (rot.w * sign * FLOAT_PRECISION_MULT);
            } else {
                a = (short) (rot.x * sign * FLOAT_PRECISION_MULT);
                b = (short) (rot.y * sign * FLOAT_PRECISION_MULT);
                c = (short) (rot.z * sign * FLOAT_PRECISION_MULT);
            }

            List<byte> list = new List<byte>();
            list.Add(maxIndex);
            list.AddRange(BitConverter.GetBytes(a));
            list.AddRange(BitConverter.GetBytes(b));
            list.AddRange(BitConverter.GetBytes(c));

            return list.ToArray();
        }

        public static Quaternion Decompress(byte maxIndex, short[] data = null) {
            // Values between 4 and 7 indicate that only the index of the single field whose value is 1f was
            // sent, and (maxIndex - 4) is the correct index for that field.
            if(maxIndex >= 4 && maxIndex <= 7) {
                float x = (maxIndex == 4) ? 1f : 0f;
                float y = (maxIndex == 5) ? 1f : 0f;
                float z = (maxIndex == 6) ? 1f : 0f;
                float w = (maxIndex == 7) ? 1f : 0f;

                return new Quaternion(x, y, z, w);
            }

            if(data == null) {
                throw new ArgumentNullException("Quaternion couldn't be decompressed, no data specified!");
            }
            if(data.Length < 3) {
                throw new ArgumentNullException($"Quaternion couldn't be decompressed, not enough data specified! Required: 3 / Provided: {data.Length}");
            }

            // Read the other three fields and derive the value of the omitted field
            float a = (float) data[0] / FLOAT_PRECISION_MULT;
            float b = (float) data[1] / FLOAT_PRECISION_MULT;
            float c = (float) data[2] / FLOAT_PRECISION_MULT;
            float d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

            if(maxIndex == 0)
                return new Quaternion(d, a, b, c);
            else if(maxIndex == 1)
                return new Quaternion(a, d, b, c);
            else if(maxIndex == 2)
                return new Quaternion(a, b, d, c);

            return new Quaternion(a, b, c, d);
        }


    }
}
