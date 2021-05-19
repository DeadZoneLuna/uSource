using System;
using UnityEngine;

namespace uSource.MathLib
{
    public class MathLibrary
    {
        //TODO: FIX ROTATION ON ATTACHMENTS
        public static void ConvertRotationMatrixToDegrees(float m0, float m1, float m2, float m3, float m4, float m5, float m8, ref Vector3 angles)
        {
            double c;
            double translateX;
            double translateY;

            // NOTE: For Math.Asin, return value is NaN if d < -1 or d > 1 or d equals NaN.
            // Therefore, change value outside of domain to edge of domain.
            if (m2 < -1)
            {
                m2 = -1;
            }
            else if (m2 > 1f)
            {
                m2 = 1f;
            }

            angles.y = (float)-Math.Asin(Math.Round(m2, 6));
            c = Math.Cos(angles.y);
            angles.y = angles.y * Mathf.Rad2Deg;
            if (Math.Abs(c) > 0.005d)
            {
                translateX = Math.Round(m8, 6) / c;
                translateY = Math.Round(-m5, 6) / c;
                angles.x = (float)Math.Atan2(translateY, translateX) * Mathf.Rad2Deg;
                translateX = Math.Round(m0, 6) / c;
                translateY = Math.Round(-m1, 6) / c;
                angles.z = (float)Math.Atan2(translateY, translateX) * Mathf.Rad2Deg;
            }
            else
            {
                angles.x = (float)0d;
                translateX = Math.Round(m4, 6);
                translateY = Math.Round(m3, 6);
                angles.z = (float)Math.Atan2(translateY, translateX) * Mathf.Rad2Deg;
            }
        }

        public static Vector3 SwapZY(Vector3 Inp)
        {
            Inp.x = -Inp.x;
            float temp = Inp.y;
            Inp.y = Inp.z;
            Inp.z = -temp;

            return Inp;
            //return new Vector3(-Inp.x, Inp.z, -Inp.y);
        }

        public static Vector3 NegateX(Vector3 Inp)
        {
            Inp.x = -Inp.x;
            return Inp;
        }

        public static Quaternion AngleQuaternion(Vector3 eulerAngles)
        {
            Quaternion qx = Quaternion.AngleAxis(eulerAngles.x, Vector3.right);
            Quaternion qy = Quaternion.AngleAxis(-eulerAngles.y, Vector3.up);
            Quaternion qz = Quaternion.AngleAxis(-eulerAngles.z, Vector3.forward);

            return qz * qy * qx;
        }

        public static Vector3 SwapY(Vector3 Inp)
        {
            //X Y Z
            return new Vector3(-Inp.y, Inp.z, Inp.x);
        }

        public static Vector3 SwapZYX(Vector3 Inp)
        {
            return new Vector3(-Inp.z, -Inp.y, Inp.x);
        }

        public static Vector3 UnSwapZY(Vector3 Inp)
        {
            /*Vector3 temp = Inp;
            Inp.x = temp.z;
            Inp.y = -temp.x;
            Inp.z = temp.y;

            return Inp;*/
            return new Vector3(Inp.z, -Inp.x, Inp.y);
        }

        //SOURCEMATH

        /// <summary>
        /// (V_swap)
        /// </summary>
        /// <param name="x">Input X Component</param>
        /// <param name="y">Input Y Component</param>
        public static void SwapComponent<T>(ref T x, ref T y )
        {
            T temp = x;
            x = y;
            y = temp;
        }

        // solves for "a, b, c" where "a x^2 + b x + c = y", return true if solution exists
        public static bool SolveInverseQuadratic(float x1, float y1, float x2, float y2, float x3, float y3, ref float a, ref float b, ref float c )
        {
            float det = (x1 - x2) * (x1 - x3) * (x2 - x3);

            // FIXME: check with some sort of epsilon
            if (det == 0.0f)
                return false;

            a = (x3 * (-y1 + y2) + x2 * (y1 - y3) + x1 * (-y2 + y3)) / det;

            b = (x3 * x3 * (y1 - y2) + x1 * x1 * (y2 - y3) + x2 * x2 * (-y1 + y3)) / det;

            c = (x1 * x3 * (-x1 + x3) * y2 + x2 * x2 * (x3 * y1 - x1 * y3) + x2 * (-(x3 * x3 * y1) + x1 * x1 * y3)) / det;

            return true;
        }

        public static bool SolveInverseQuadraticMonotonic(Single x1, Single y1, Single x2, Single y2, Single x3, Single y3, ref Single a, ref Single b, ref Single c )
        {
            // use SolveInverseQuadratic, but if the sigm of the derivative at the start point is the wrong
            // sign, displace the mid point

            // first, sort parameters
            if (x1 > x2)
            {
                SwapComponent(ref x1, ref x2);
                SwapComponent(ref y1, ref y2);
            }
            if (x2 > x3)
            {
                SwapComponent(ref x2, ref x3);
                SwapComponent(ref y2, ref y3);
            }
            if (x1 > x2)
            {
                SwapComponent(ref x1, ref x2);
                SwapComponent(ref y1, ref y2);
            }
            // this code is not fast. what it does is when the curve would be non-monotonic, slowly shifts
            // the center point closer to the linear line between the endpoints. Should anyone need htis
            // function to be actually fast, it would be fairly easy to change it to be so.
            for (float blend_to_linear_factor = 0.0f; blend_to_linear_factor <= 1.0; blend_to_linear_factor += 0.05f)
            {
                float tempy2 = (1 - blend_to_linear_factor) * y2 + blend_to_linear_factor * FLerp(y1, y3, x1, x3, x2);
                if (!SolveInverseQuadratic(x1, y1, x2, tempy2, x3, y3, ref a, ref b, ref c))
                    return false;
                float derivative = 2.0f * a + b;
                if ((y1 < y2) && (y2 < y3))                         // monotonically increasing
                {
                    if (derivative >= 0.0f)
                        return true;
                }
                else
                {
                    if ((y1 > y2) && (y2 > y3))                         // monotonically decreasing
                    {
                        if (derivative <= 0.0f)
                            return true;
                    }
                    else
                        return true;
                }
            }
            return true;
        }

        // 5-argument floating point linear interpolation.
        // FLerp(f1,f2,i1,i2,x)=
        //    f1 at x=i1
        //    f2 at x=i2
        //   smooth lerp between f1 and f2 at x>i1 and x<i2
        //   extrapolation for x<i1 or x>i2
        //
        //   If you know a function f(x)'s value (f1) at position i1, and its value (f2) at position i2,
        //   the function can be linearly interpolated with FLerp(f1,f2,i1,i2,x)
        //    i2=i1 will cause a divide by zero.
        public static Single FLerp(Single f1, Single f2, Single i1, Single i2, Single x)
        {
            return f1 + (f2 - f1) * (x - i1) / (i2 - i1);
        }

        private enum EulerParity
        {
            Even,
            Odd
        }

        private enum EulerRepeat
        {
            No,
            Yes
        }

        private enum EulerFrame
        {
            S,
            R
        }

        public static Vector3 ToEulerAngles(Quaternion q)
        {
            return Eul_FromQuat(q, 0, 1, 2, 0, EulerParity.Even, EulerRepeat.No, EulerFrame.S);
        }

        private static Vector3 Eul_FromHMatrix(float[,] M, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame)
        {
            Vector3 ea = new Vector3();

            if (repeat == EulerRepeat.Yes)
            {
                float sy = Mathf.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
                if (sy > 16 * Mathf.Epsilon)
                {
                    ea.x = Mathf.Atan2(M[i, j], M[i, k]);
                    ea.y = Mathf.Atan2(sy, M[i, i]);
                    ea.z = Mathf.Atan2(M[j, i], -M[k, i]);
                }
                else
                {
                    ea.x = Mathf.Atan2(-M[j, k], M[j, j]);
                    ea.y = Mathf.Atan2(sy, M[i, i]);
                    ea.z = 0;
                }
            }
            else
            {
                float cy = Mathf.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
                if (cy > 16 * Mathf.Epsilon)
                {
                    ea.x = Mathf.Atan2(M[k, j], M[k, k]);
                    ea.y = Mathf.Atan2(-M[k, i], cy);
                    ea.z = Mathf.Atan2(M[j, i], M[i, i]);
                }
                else
                {
                    ea.x = Mathf.Atan2(-M[j, k], M[j, j]);
                    ea.y = Mathf.Atan2(-M[k, i], cy);
                    ea.z = 0;
                }
            }

            if (parity == EulerParity.Odd)
            {
                ea.x = -ea.x;
                ea.y = -ea.y;
                ea.z = -ea.z;
            }

            if (frame == EulerFrame.R)
            {
                float t = ea.x;
                ea.x = ea.z;
                ea.z = t;
            }

            //ea.w = order
            return ea;
        }

        private static Vector3 Eul_FromQuat(Quaternion q, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame)
        {
            float[,] M =
            {
                {0, 0, 0, 0},
                {0, 0, 0, 0},
                {0, 0, 0, 0},
                {0, 0, 0, 0}
            };
            float Nq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            float s;
            if (Nq > 0)
            {
                s = 2.0f / Nq;
            }
            else
            {
                s = 0;
            }
            float xs = q.x * s;
            float ys = q.y * s;
            float zs = q.z * s;

            float wx = q.w * xs;
            float wy = q.w * ys;
            float wz = q.w * zs;
            float xx = q.x * xs;
            float xy = q.x * ys;
            float xz = q.x * zs;
            float yy = q.y * ys;
            float yz = q.y * zs;
            float zz = q.z * zs;
            M[0, 0] = 1.0f - (yy + zz);
            M[0, 1] = xy - wz;
            M[0, 2] = xz + wy;
            M[1, 0] = xy + wz;
            M[1, 1] = 1.0f - (xx + zz);
            M[1, 2] = yz - wx;
            M[2, 0] = xz - wy;
            M[2, 1] = yz + wx;
            M[2, 2] = 1.0f - (xx + yy);
            M[3, 3] = 1.0f;

            return Eul_FromHMatrix(M, i, j, k, h, parity, repeat, frame);
        }

        //SOURCEMATH
    }
}