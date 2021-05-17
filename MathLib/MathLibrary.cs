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