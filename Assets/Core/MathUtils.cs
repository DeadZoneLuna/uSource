using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

enum Angle
{
	PITCH = 0,  // up / down
	YAW,        // left / right
	ROLL        // fall over
};

namespace Engine.Source
{
	class MathUtils
    {

		public static Vector3 SwapZY(Vector3 Inp)
        {
            return new Vector3(-Inp.x, Inp.z, -Inp.y);
        }

        public static Vector3 SwapY(Vector3 Inp)
        {
            return new Vector3(Inp.z, -Inp.y, Inp.x);
        }

        public static Vector3 SwapZYX(Vector3 Inp)
        {
            return new Vector3(-Inp.z, -Inp.y, -Inp.x);
        }

        public static Vector3 UnSwapZY(Vector3 Inp)
        {
            return new Vector3(-Inp.x, -Inp.z, Inp.y);
        }


		public static Vector3 SetupLightNormalFromProps( Vector3 angles, float angle, float pitch, Vector3 output )
		{

			if (angle == -1)
			{
				output[0] = output[1] = 0;
				output[2] = 1;
			}
			else if (angle == -2)
			{
				output[0] = output[1] = 0;
				output[2] = -1;
			}
			else
			{
			// if we don't have a specific "angle" use the "angles" YAW
				if ( angle != pitch )
				{
					angle = angles[(int)Angle.YAW];
				}
		
				output[2] = 0;
				output[0] = Mathf.Cos (angle/180 * (float)Math.PI);
				output[1] = Mathf.Sin (angle/180 * (float)Math.PI);
			}
	
			if ( pitch != angle )
			{
				// if we don't have a specific "pitch" use the "angles" PITCH
				pitch = angles[(int)Angle.PITCH];
			}
	
			output[2] = Mathf.Sin(pitch/ 180 * (float)Math.PI);
			output[0] *= Mathf.Cos(pitch/ 180 * (float)Math.PI);
			output[1] *= Mathf.Cos(pitch/ 180 * (float)Math.PI);

			return output;
		}

		/*public static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
		{
			Vector3 side1 = b - a;
			Vector3 side2 = c - a;
			return Vector3.Cross(side1, side2).normalized;
		}*/

		//SourceEngine Math Lib

		public static long AlignLong(long currentValue, long alignmentValue)
		{
			// File seek to next nearest start of 4-byte block. 
			//      In C++: #define ALIGN4( a ) a = (byte *)((int)((byte *)a + 3) & ~ 3)
			long result = 0;
			result = (currentValue + alignmentValue - 1) & ~(alignmentValue - 1);
			return result;
		}

		public static double DegreesToRadians(double degrees)
		{
			// 57.29578 = 180 / pi
			return degrees / 57.29578;
		}

		public static double RadiansToDegrees(double radians)
		{
			// 57.29578 = 180 / pi
			return radians * 57.29578;
		}

		public static Quaternion Euler(float yaw, float pitch, float roll)
		{
			yaw *= Mathf.Deg2Rad;
			pitch *= Mathf.Deg2Rad;
			roll *= Mathf.Deg2Rad;

			double yawOver2 = yaw * 0.5f;
			float cosYawOver2 = (float)Math.Cos(yawOver2);
			float sinYawOver2 = (float)Math.Sin(yawOver2);
			double pitchOver2 = pitch * 0.5f;
			float cosPitchOver2 = (float)Math.Cos(pitchOver2);
			float sinPitchOver2 = (float)Math.Sin(pitchOver2);
			double rollOver2 = roll * 0.5f;
			float cosRollOver2 = (float)Math.Cos(rollOver2);
			float sinRollOver2 = (float)Math.Sin(rollOver2);
			Quaternion result;
			result.w = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
			result.x = sinYawOver2 * cosPitchOver2 * cosRollOver2 + cosYawOver2 * sinPitchOver2 * sinRollOver2;
			result.y = cosYawOver2 * sinPitchOver2 * cosRollOver2 - sinYawOver2 * cosPitchOver2 * sinRollOver2;
			result.z = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;

			return result;
		}

		//-----------------------------------------------------------------------------
		// Euler QAngle -> Basis Vectors
		//-----------------------------------------------------------------------------

		public static Vector3 AngleVectors(Quaternion angles, Vector3 forward)
		{
			float sp = 0
				, sy = 0
				, cp = 0
				, cy = 0;

			Mathf.Clamp(Mathf.Cos(angles.y ), sy, cy );
			Mathf.Clamp(Mathf.Cos(angles.x ), sp, cp );

			forward.x = cp* cy;
			forward.y = cp* sy;
			forward.z = -sp;

			return forward;
		}

		//-----------------------------------------------------------------------------
		// Euler QAngle -> Basis Vectors.  Each vector is optional
		//-----------------------------------------------------------------------------
		public static Quaternion AngleVectors( Quaternion angles, Vector3 forward, Vector3 right, Vector3 up)
		{

			float sr = 0
				, sp = 0
				, sy = 0
				, cr = 0
				, cp = 0
				, cy = 0;

			Mathf.Clamp(Mathf.Cos(angles.y), sy, cy);
			Mathf.Clamp(Mathf.Cos(angles.x), sp, cp);
			Mathf.Clamp(Mathf.Cos(angles.z), sr, cr);


			if (forward != right || forward != up)
			{
				forward.x = cp * cy;
				forward.y = cp * sy;
				forward.z = -sp;
			}

			if (right != forward || right != up)
			{
				right.x = (-1 * sr * sp * cy + -1 * cr * -sy);
				right.y = (-1 * sr * sp * sy + -1 * cr * cy);
				right.z = -1 * sr * cp;
			}

			if (up != right || up != forward)
			{
				up.x = (cr * sp * cy + -sr * -sy);
				up.y = (cr * sp * sy + -sr * cy);
				up.z = cr * cp;
			}

			return angles;
		}


		public static void ConvertRotationMatrixToDegrees(float m0, float m1, float m2, float m3, float m4, float m5, float m8, ref double angleX, ref double angleY, ref double angleZ)
		{
			double c = 0;
			double translateX = 0;
			double translateY = 0;

			angleY = -Math.Asin(Math.Round(m2, 6));
			c = Math.Cos(angleY);
			angleY = RadiansToDegrees(angleY);
			if (Math.Abs(c) > 0.005)
			{
				translateX = Math.Round(m8, 6) / c;
				translateY = Math.Round(-m5, 6) / c;
				angleX = RadiansToDegrees(Math.Atan2(translateY, translateX));
				translateX = Math.Round(m0, 6) / c;
				translateY = Math.Round(-m1, 6) / c;
				angleZ = RadiansToDegrees(Math.Atan2(translateY, translateX));
			}
			else
			{
				angleX = 0;
				translateX = Math.Round(m4, 6);
				translateY = Math.Round(m3, 6);
				angleZ = RadiansToDegrees(Math.Atan2(translateY, translateX));
			}
		}

		//Quat - SourceQuat
		//Vec3 - SourceVec

		public static Quaternion EulerAnglesToQuaternion(Vector3 angleVector)
		{
			float fPitch = 0;
			float fYaw = 0;
			float fRoll = 0;
			Quaternion rot = new Quaternion();

			fPitch = angleVector.x;
			fYaw = angleVector.y;
			fRoll = angleVector.z;

			float fSinPitch = (float)Math.Sin(fPitch * 0.5F);
			float fCosPitch = (float)Math.Cos(fPitch * 0.5F);
			float fSinYaw = (float)Math.Sin(fYaw * 0.5F);
			float fCosYaw = (float)Math.Cos(fYaw * 0.5F);
			float fSinRoll = (float)Math.Sin(fRoll * 0.5F);
			float fCosRoll = (float)Math.Cos(fRoll * 0.5F);
			float fCosPitchCosYaw = fCosPitch * fCosYaw;
			float fSinPitchSinYaw = fSinPitch * fSinYaw;

			rot.x = fSinRoll * fCosPitchCosYaw - fCosRoll * fSinPitchSinYaw;
			rot.y = fCosRoll * fSinPitch * fCosYaw + fSinRoll * fCosPitch * fSinYaw;
			rot.z = fCosRoll * fCosPitch * fSinYaw - fSinRoll * fSinPitch * fCosYaw;
			rot.w = fCosRoll * fCosPitchCosYaw + fSinRoll * fSinPitchSinYaw;

			return rot;
		}

		public static void AngleMatrix(double pitchRadians, double yawRadians, double rollRadians, ref Vector3 matrixColumn0, ref Vector3 matrixColumn1, ref Vector3 matrixColumn2, ref Vector3 matrixColumn3)
		{
			float sr = 0;
			float sp = 0;
			float sy = 0;
			float cr = 0;
			float cp = 0;
			float cy = 0;

			sy = (float)Math.Sin(yawRadians);
			cy = (float)Math.Cos(yawRadians);
			sp = (float)Math.Sin(pitchRadians);
			cp = (float)Math.Cos(pitchRadians);
			sr = (float)Math.Sin(rollRadians);
			cr = (float)Math.Cos(rollRadians);

			matrixColumn0.x = cp * cy;
			matrixColumn0.y = cp * sy;
			matrixColumn0.z = -sp;
			matrixColumn1.x = sr * sp * cy + cr * -sy;
			matrixColumn1.y = sr * sp * sy + cr * cy;
			matrixColumn1.z = sr * cp;
			matrixColumn2.x = (cr * sp * cy + -sr * -sy);
			matrixColumn2.y = (cr * sp * sy + -sr * cy);
			matrixColumn2.z = cr * cp;
			matrixColumn3.x = 0;
			matrixColumn3.y = 0;
			matrixColumn3.z = 0;
		}

		public static void R_ConcatTransforms(Vector3 in1_matrixColumn0, Vector3 in1_matrixColumn1, Vector3 in1_matrixColumn2, Vector3 in1_matrixColumn3, Vector3 in2_matrixColumn0, Vector3 in2_matrixColumn1, Vector3 in2_matrixColumn2, Vector3 in2_matrixColumn3, ref Vector3 out_matrixColumn0, ref Vector3 out_matrixColumn1, ref Vector3 out_matrixColumn2, ref Vector3 out_matrixColumn3)
		{
			out_matrixColumn0.x = in1_matrixColumn0.x * in2_matrixColumn0.x + in1_matrixColumn1.x * in2_matrixColumn0.y + in1_matrixColumn2.x * in2_matrixColumn0.z;
			out_matrixColumn1.x = in1_matrixColumn0.x * in2_matrixColumn1.x + in1_matrixColumn1.x * in2_matrixColumn1.y + in1_matrixColumn2.x * in2_matrixColumn1.z;
			out_matrixColumn2.x = in1_matrixColumn0.x * in2_matrixColumn2.x + in1_matrixColumn1.x * in2_matrixColumn2.y + in1_matrixColumn2.x * in2_matrixColumn2.z;
			out_matrixColumn3.x = in1_matrixColumn0.x * in2_matrixColumn3.x + in1_matrixColumn1.x * in2_matrixColumn3.y + in1_matrixColumn2.x * in2_matrixColumn3.z + in1_matrixColumn3.x;

			out_matrixColumn0.y = in1_matrixColumn0.y * in2_matrixColumn0.x + in1_matrixColumn1.y * in2_matrixColumn0.y + in1_matrixColumn2.y * in2_matrixColumn0.z;
			out_matrixColumn1.y = in1_matrixColumn0.y * in2_matrixColumn1.x + in1_matrixColumn1.y * in2_matrixColumn1.y + in1_matrixColumn2.y * in2_matrixColumn1.z;
			out_matrixColumn2.y = in1_matrixColumn0.y * in2_matrixColumn2.x + in1_matrixColumn1.y * in2_matrixColumn2.y + in1_matrixColumn2.y * in2_matrixColumn2.z;
			out_matrixColumn3.y = in1_matrixColumn0.y * in2_matrixColumn3.x + in1_matrixColumn1.y * in2_matrixColumn3.y + in1_matrixColumn2.y * in2_matrixColumn3.z + in1_matrixColumn3.y;

			out_matrixColumn0.z = in1_matrixColumn0.z * in2_matrixColumn0.x + in1_matrixColumn1.z * in2_matrixColumn0.y + in1_matrixColumn2.z * in2_matrixColumn0.z;
			out_matrixColumn1.z = in1_matrixColumn0.z * in2_matrixColumn1.x + in1_matrixColumn1.z * in2_matrixColumn1.y + in1_matrixColumn2.z * in2_matrixColumn1.z;
			out_matrixColumn2.z = in1_matrixColumn0.z * in2_matrixColumn2.x + in1_matrixColumn1.z * in2_matrixColumn2.y + in1_matrixColumn2.z * in2_matrixColumn2.z;
			out_matrixColumn3.z = in1_matrixColumn0.z * in2_matrixColumn3.x + in1_matrixColumn1.z * in2_matrixColumn3.y + in1_matrixColumn2.z * in2_matrixColumn3.z + in1_matrixColumn3.z;
		}

		public static float DotProduct(Vector3 vector1, Vector3 vector2)
		{
			return vector1.x * vector2.x + vector1.y * vector2.y + vector1.z * vector2.z;
		}

		public static void VectorCopy(Vector3 input, ref Vector3 output)
		{
			output.x = input.x;
			output.y = input.y;
			output.z = input.z;
		}

		public static Vector3 VectorRotate(Vector3 input, Vector3 matrixColumn0, Vector3 matrixColumn1, Vector3 matrixColumn2, Vector3 matrixColumn3)
		{
			Vector3 output = Vector3.zero;
			Vector3 matrixRow0 = Vector3.zero;
			Vector3 matrixRow1 = Vector3.zero;
			Vector3 matrixRow2 = Vector3.zero;

			output = new Vector3();
			matrixRow0 = new Vector3();
			matrixRow1 = new Vector3();
			matrixRow2 = new Vector3();

			matrixRow0.x = matrixColumn0.x;
			matrixRow0.y = matrixColumn1.x;
			matrixRow0.z = matrixColumn2.x;

			matrixRow1.x = matrixColumn0.y;
			matrixRow1.y = matrixColumn1.y;
			matrixRow1.z = matrixColumn2.y;

			matrixRow2.x = matrixColumn0.z;
			matrixRow2.y = matrixColumn1.z;
			matrixRow2.z = matrixColumn2.z;

			output.x = DotProduct(input, matrixRow0);
			output.y = DotProduct(input, matrixRow1);
			output.z = DotProduct(input, matrixRow2);

			return output;
		}

		public static float VectorNormalize(ref Vector3 ioVector)
		{
			float length = 0;

			length = 0;
			length += ioVector.x * ioVector.x;
			length += ioVector.y * ioVector.y;
			length += ioVector.z * ioVector.z;
			length = (float)Math.Sqrt(length);
			if (length == 0)
			{
				return 0;
			}

			ioVector.x /= length;
			ioVector.y /= length;
			ioVector.z /= length;

			return length;
		}

		public static Vector3 VectorTransform(Vector3 input, Vector3 matrixColumn0, Vector3 matrixColumn1, Vector3 matrixColumn2, Vector3 matrixColumn3)
		{
			Vector3 output = Vector3.zero;
			Vector3 matrixRow0 = Vector3.zero;
			Vector3 matrixRow1 = Vector3.zero;
			Vector3 matrixRow2 = Vector3.zero;

			output = new Vector3();
			matrixRow0 = new Vector3();
			matrixRow1 = new Vector3();
			matrixRow2 = new Vector3();

			matrixRow0.x = matrixColumn0.x;
			matrixRow0.y = matrixColumn1.x;
			matrixRow0.z = matrixColumn2.x;

			matrixRow1.x = matrixColumn0.y;
			matrixRow1.y = matrixColumn1.y;
			matrixRow1.z = matrixColumn2.y;

			matrixRow2.x = matrixColumn0.z;
			matrixRow2.y = matrixColumn1.z;
			matrixRow2.z = matrixColumn2.z;

			output.x = DotProduct(input, matrixRow0) + matrixColumn3.x;
			output.y = DotProduct(input, matrixRow1) + matrixColumn3.y;
			output.z = DotProduct(input, matrixRow2) + matrixColumn3.z;

			return output;
		}

		public static Vector3 VectorITransform(Vector3 input, Vector3 matrixColumn0, Vector3 matrixColumn1, Vector3 matrixColumn2, Vector3 matrixColumn3)
		{
			Vector3 output = Vector3.zero;
			Vector3 temp = Vector3.zero;

			output = new Vector3();
			temp = new Vector3();

			temp.x = input.x - matrixColumn3.x;
			temp.y = input.y - matrixColumn3.y;
			temp.z = input.z - matrixColumn3.z;

			output.x = temp.x * matrixColumn0.x + temp.y * matrixColumn0.y + temp.z * matrixColumn0.z;
			output.y = temp.x * matrixColumn1.x + temp.y * matrixColumn1.y + temp.z * matrixColumn1.z;
			output.z = temp.x * matrixColumn2.x + temp.y * matrixColumn2.y + temp.z * matrixColumn2.z;

			return output;
		}

		public static Vector3 RotateAboutZAxis(Vector3 input, double angleInRadians, StudioMDLLoader.mstudiobone_t aBone)
		{
			Vector3 poseToBoneColumn0 = new Vector3();
			Vector3 poseToBoneColumn1 = new Vector3();
			Vector3 poseToBoneColumn2 = new Vector3();
			Vector3 poseToBoneColumn3 = new Vector3();

			poseToBoneColumn0.x = (float)Math.Cos(angleInRadians);
			poseToBoneColumn0.y = (float)Math.Sin(angleInRadians);
			poseToBoneColumn0.z = 0;

			poseToBoneColumn1.x = -poseToBoneColumn0.y;
			poseToBoneColumn1.y = poseToBoneColumn0.x;
			poseToBoneColumn1.z = 0;

			poseToBoneColumn2.x = 0;
			poseToBoneColumn2.y = 0;
			poseToBoneColumn2.z = 1;

			poseToBoneColumn3.x = 0;
			poseToBoneColumn3.y = 0;
			poseToBoneColumn3.z = 0;

			return VectorITransform(input, poseToBoneColumn0, poseToBoneColumn1, poseToBoneColumn2, poseToBoneColumn3);
		}

		public static Vector3 ToEulerAngles(Vector3 q)
		{
			return Eul_FromQuat(Quaternion.Euler(q), 0, 1, 2, 0, EulerParity.Even, EulerRepeat.No, EulerFrame.S);
		}

		private static Vector3 Eul_FromHMatrix(double[,] M, int i, int j, int k, int h, EulerParity parity, EulerRepeat repeat, EulerFrame frame)
		{
			Vector3 ea = new Vector3();

			if (repeat == EulerRepeat.Yes)
			{
				float sy = (float)Math.Sqrt(M[i, j] * M[i, j] + M[i, k] * M[i, k]);
				if (sy > 16 * FLT_EPSILON)
				{
					ea.x = (float)Math.Atan2(M[i, j], M[i, k]);
					ea.y = (float)Math.Atan2(sy, M[i, i]);
					ea.z = (float)Math.Atan2(M[j, i], -M[k, i]);
				}
				else
				{
					ea.x = (float)Math.Atan2(-M[j, k], M[j, j]);
					ea.y = (float)Math.Atan2(sy, M[i, i]);
					ea.z = 0;
				}
			}
			else
			{
				float cy = (float)Math.Sqrt(M[i, i] * M[i, i] + M[j, i] * M[j, i]);
				if (cy > 16 * FLT_EPSILON)
				{
					ea.x = (float)Math.Atan2(M[k, j], M[k, k]);
					ea.y = (float)Math.Atan2(-M[k, i], cy);
					ea.z = (float)Math.Atan2(M[j, i], M[i, i]);
				}
				else
				{
					ea.x = (float)Math.Atan2(-M[j, k], M[j, j]);
					ea.y = (float)Math.Atan2(-M[k, i], cy);
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
			double[,] M =
			{
				{0, 0, 0, 0},
				{0, 0, 0, 0},
				{0, 0, 0, 0},
				{0, 0, 0, 0}
			};
			double Nq = 0;
			double s = 0;
			double xs = 0;
			double ys = 0;
			double zs = 0;
			double wx = 0;
			double wy = 0;
			double wz = 0;
			double xx = 0;
			double xy = 0;
			double xz = 0;
			double yy = 0;
			double yz = 0;
			double zz = 0;

			Nq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
			if (Nq > 0)
			{
				s = 2.0 / Nq;
			}
			else
			{
				s = 0;
			}
			xs = q.x * s;
			ys = q.y * s;
			zs = q.z * s;

			wx = q.w * xs;
			wy = q.w * ys;
			wz = q.w * zs;
			xx = q.x * xs;
			xy = q.x * ys;
			xz = q.x * zs;
			yy = q.y * ys;
			yz = q.y * zs;
			zz = q.z * zs;

			M[0, 0] = 1.0 - (yy + zz);
			M[0, 1] = xy - wz;
			M[0, 2] = xz + wy;
			M[1, 0] = xy + wz;
			M[1, 1] = 1.0 - (xx + zz);
			M[1, 2] = yz - wx;
			M[2, 0] = xz - wy;
			M[2, 1] = yz + wx;
			M[2, 2] = 1.0 - (xx + yy);
			M[3, 3] = 1.0;

			return Eul_FromHMatrix(M, i, j, k, h, parity, repeat, frame);
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

		private const double FLT_EPSILON = 0.00001;

		//OpenTK

		public static class OpenTkExtensions2
		{
			public static Quaternion QuaternionFromEulerRotation(Vector3 angles)
			{
				return QuaternionFromEulerRotation(angles.x, angles.y, angles.z);
			}

			public static Quaternion QuaternionFromEulerRotation(float yaw, float pitch, float roll)
			{
				// http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternionF/index.htm

				var angles = new Vector3(yaw, pitch, roll);
				angles = angles / 2;

				var sy = Mathf.Sin(angles.z);
				var sp = Mathf.Sin(angles.y);
				var sr = Mathf.Sin(angles.x);
				var cy = Mathf.Cos(angles.z);
				var cp = Mathf.Cos(angles.y);
				var cr = Mathf.Cos(angles.x);

				return new Quaternion(sr * cp * cy - cr * sp * sy,
					cr * sp * cy + sr * cp * sy,
					cr * cp * sy - sr * sp * cy,
					cr * cp * cy + sr * sp * sy);
			}
		}
    }

}
