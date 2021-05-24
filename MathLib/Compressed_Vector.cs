using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace uSource.MathLib
{
	[StructLayout(LayoutKind.Explicit)]
	public struct IntegerAndSingleUnion
	{
		[FieldOffset(0)]
		public Int32 i;
		[FieldOffset(0)]
		public Single s;
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Float16
	{
		public Single GetFloat
		{
			get
			{
				Int32 sign = GetSign(bits);
				Int32 floatSign;
				if (sign == 1)
				{
					floatSign = -1;
				}
				else
				{
					floatSign = 1;
				}

				if (IsInfinity(bits))
				{
					return maxfloat16bits * floatSign;
				}

				if (IsNaN(bits))
				{
					return 0;
				}
				Int32 mantissa = GetMantissa(bits);
				Int32 biased_exponent = GetBiasedExponent(bits);
				Single result;
				if (biased_exponent == 0 && mantissa != 0)
				{
					Single floatMantissa = mantissa / 1024.0F;
					result = floatSign * floatMantissa * half_denorm;
				}
				else
				{
					result = GetSingle();
				}
				return result;
			}
		}

		private Int32 GetMantissa(UInt16 value)
		{
			return (value & 0x3FF);
		}

		private Int32 GetBiasedExponent(UInt16 value)
		{
			return (value & 0x7C00) >> 10;
		}

		private Int32 GetSign(UInt16 value)
		{
			return (value & 0x8000) >> 15;
		}

		private Single GetSingle()
		{
			IntegerAndSingleUnion bitsResult = new IntegerAndSingleUnion();
			bitsResult.i = 0;

			Int32 mantissa = GetMantissa(bits);
			Int32 biased_exponent = GetBiasedExponent(bits);
			Int32 sign = GetSign(bits);

			Int32 resultMantissa = mantissa << 23 - 10;
			Int32 resultBiasedExponent;
			if (biased_exponent == 0)
			{
				resultBiasedExponent = 0;
			}
			else
			{
				resultBiasedExponent = (biased_exponent - float16bias + float32bias) << 23;
			}
			Int32 resultSign = sign << 31;

			bitsResult.i = resultSign | resultBiasedExponent | resultMantissa;

			return bitsResult.s;
		}

		private bool IsInfinity(ushort value)
		{
			Int32 mantissa = GetMantissa(value);
			Int32 biased_exponent = GetBiasedExponent(value);
			return ((biased_exponent == 31) && (mantissa == 0));
		}

		private bool IsNaN(ushort value)
		{
			Int32 mantissa = GetMantissa(value);
			Int32 biased_exponent = GetBiasedExponent(value);
			return ((biased_exponent == 31) && (mantissa != 0));
		}

		private const Int32 float32bias = 127;
		private const Int32 float16bias = 15;
		private const Single maxfloat16bits = 65504.0F;
		private const Single half_denorm = (1.0F / 16384.0F);

		public UInt16 bits;
	}

	/// <summary>
	/// sizeof = 6
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Vector48
	{
		public Float16 x;
		public Float16 y;
		public Float16 z;

		public Vector3 ToVector3()
		{
			Vector3 result;
			result.x = x.GetFloat;
			result.y = y.GetFloat;
			result.z = z.GetFloat;

			return result;
		}

		public static explicit operator Vector3(Vector48 obj)
		{
			return obj.ToVector3();
		}
	}

	/// <summary>
	/// sizeof = 6
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Quaternion48
	{
		public UInt16 theXInput;
		public UInt16 theYInput;
		public UInt16 theZWInput;

		public Single x
		{
			get
			{
				Single result = (Convert.ToInt32(theXInput) - 32768) * (1 / 32768.0f);
				return result;
			}
		}

		public Single y
		{
			get
			{
				Single result = (Convert.ToInt32(theYInput) - 32768) * (1 / 32768.0f);
				return result;
			}
		}

		public Single z
		{
			get
			{
				Int32 zInput = theZWInput & 0x7FFF;
				Single result = (zInput - 16384) * (1 / 16384.0f);
				return result;
			}
		}

		public Single w
		{
			get
			{
				return Mathf.Sqrt(1 - x * x - y * y - z * z) * wneg;
			}
		}

		public Single wneg
		{
			get
			{
				if ((theZWInput & 0x8000) > 0)
				{
					return -1;
				}
				else
				{
					return 1;
				}
			}
		}

		public Quaternion quaternion
		{
			get
			{
				Quaternion aQuaternion;
				aQuaternion.x = x;
				aQuaternion.y = y;
				aQuaternion.z = z;
				aQuaternion.w = w;
				return aQuaternion;
			}
		}
	}

	/// <summary>
	/// sizeof = 8
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Quaternion64
	{
		[MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
		public Byte[] theBytes;

		public Single x
		{
			get
			{
				IntegerAndSingleUnion bitsResult = new IntegerAndSingleUnion();
				Int32 byte0 = Convert.ToInt32(theBytes[0]) & 0xFF;
				Int32 byte1 = (Convert.ToInt32(theBytes[1]) & 0xFF) << 8;
				Int32 byte2 = (Convert.ToInt32(theBytes[2]) & 0x1F) << 16;

				bitsResult.i = byte2 | byte1 | byte0;
				Single result = (bitsResult.i - 1048576) * (1 / 1048576.5f);

				if (Single.IsNaN(result))
					result = 0.0f;

				return result;
			}
		}

		public Single y
		{
			get
			{
				IntegerAndSingleUnion bitsResult = new IntegerAndSingleUnion();
				Int32 byte2 = (Convert.ToInt32(theBytes[2]) & 0xE0) >> 5;
				Int32 byte3 = (Convert.ToInt32(theBytes[3]) & 0xFF) << 3;
				Int32 byte4 = (Convert.ToInt32(theBytes[4]) & 0xFF) << 11;
				Int32 byte5 = (Convert.ToInt32(theBytes[5]) & 0x3) << 19;

				bitsResult.i = byte5 | byte4 | byte3 | byte2;
				Single result = (bitsResult.i - 1048576) * (1 / 1048576.5f);

				if (Single.IsNaN(result))
					result = 0.0f;

				return result;
			}
		}

		public Single z
		{
			get
			{
				IntegerAndSingleUnion bitsResult = new IntegerAndSingleUnion();
				Int32 byte5 = (Convert.ToInt32(theBytes[5]) & 0xFC) >> 2;
				Int32 byte6 = (Convert.ToInt32(theBytes[6]) & 0xFF) << 6;
				Int32 byte7 = (Convert.ToInt32(theBytes[7]) & 0x7F) << 14;

				bitsResult.i = byte7 | byte6 | byte5;
				Single result = (bitsResult.i - 1048576) * (1 / 1048576.5f);

				if (Single.IsNaN(result))
					result = 0.0f;

				return result;
			}
		}

		public Single w
		{
			get
			{
				//result = Me.wneg
				Single result = Mathf.Sqrt(1 - x * x - y * y - z * z) * wneg;

				if (Single.IsNaN(result))
					result = 0.0f;

				return result;
			}
		}

		public Single wneg
		{
			get
			{
				if ((theBytes[7] & 0x80) > 0)
				{
					return -1;
				}
				else
				{
					return 1;
				}
			}
		}

		public Quaternion quaternion
		{
			get
			{
				Quaternion aQuaternion;
				aQuaternion.x = x;
				aQuaternion.y = y;
				aQuaternion.z = z;
				aQuaternion.w = w;
				return aQuaternion;
			}
		}
	}
}