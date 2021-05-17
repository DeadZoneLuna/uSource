using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace uSource
{
#if !NET_4_6
    /// <summary>
    /// Extentions for enums.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// A FX 3.5 way to mimic the FX4 "HasFlag" method.
        /// </summary>
        /// <param name="variable">The tested enum.</param>
        /// <param name="value">The value to test.</param>
        /// <returns>True if the flag is set. Otherwise false.</returns>
        public static bool HasFlag(this Enum variable, Enum value)
        {
            // check if from the same type.
            if (variable.GetType() != value.GetType())
            {
                throw new ArgumentException("The checked flag is not from the same type as the checked variable.");
            }

            Convert.ToUInt64(value);
            ulong num = Convert.ToUInt64(value);
            ulong num2 = Convert.ToUInt64(variable);

            return (num2 & num) == num;
        }
    }
#endif

    public static class Converters
    {
        public static string ToString(this string param)
        {
            return param;
        }

        public static int ToInt32(this string param, int defValue = 0)
        {
            int intVal;
            float value;
            return (param != null) ? (int.TryParse(param, out intVal) ? intVal : (float.TryParse(param, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? ((int)value) : defValue)) : defValue;
        }

        public static bool ToBoolean(this string param)
        {
            return param != null && ToInt32(param) != 0;
        }

        public static float ToSingle(this string param, float defValue = 0)
        {
            float value;
            return (param == null) ? defValue : (float.TryParse(param.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0f);
        }

        public static Vector3 ToVector3(this string param)
        {
            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);

            var x = split0 == -1 ? ToSingle(param) : ToSingle(param.Substring(0, split0));
            var y = split0 == -1 ? 0f : split1 == -1 ? ToSingle(param.Substring(split0 + 1)) : ToSingle(param.Substring(split0 + 1, split1 - split0 - 1));
            var z = split1 == -1 ? 0f : ToSingle(param.Substring(split1 + 1));

            return new Vector3(x, y, z);
        }

        public static Color32 ToColor32(this string param)
        {
            if (param == null) return new Color32(0x00, 0x00, 0x00, 255);

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            var r = split0 == -1 ? ToInt32(param) : ToInt32(param.Substring(0, split0));
            var g = split0 == -1 ? 0 : split1 == -1 ? ToInt32(param.Substring(split0 + 1)) : ToInt32(param.Substring(split0 + 1, split1 - split0 - 1));
            var b = split1 == -1 ? 0 : split2 == -1 ? ToInt32(param.Substring(split1 + 1)) : ToInt32(param.Substring(split1 + 1, split2 - split1 - 1));
            var a = split2 == -1 ? 255 : ToInt32(param.Substring(split2 + 1));

            return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public static Color ToColor(this string param)
        {
            if (param == null) return new Color(0, 0, 0, 1);

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            var r = split0 == -1 ? ToInt32(param) : ToInt32(param.Substring(0, split0));
            var g = split0 == -1 ? 0 : split1 == -1 ? ToInt32(param.Substring(split0 + 1)) : ToInt32(param.Substring(split0 + 1, split1 - split0 - 1));
            var b = split1 == -1 ? 0 : split2 == -1 ? ToInt32(param.Substring(split1 + 1)) : ToInt32(param.Substring(split1 + 1, split2 - split1 - 1));
            var a = split2 == -1 ? 255 : ToInt32(param.Substring(split2 + 1));

            //float Pow = Mathf.Pow(2, a / 255f);

            //pow( r / 255.0, 2.2 ) * 255

            Color color = new Vector4(ToLinearF(r), ToLinearF(g), ToLinearF(b), ToLinearF(a));

            return color;
        }

        static float ToLinearF(int color)
        {
            return Mathf.Clamp(color, 0, 255) / 255.0f;
        }

        public static Vector4 ToColorVec(this string param)
        {
            if (param == null) return new Color(0, 0, 0, 200);

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            var r = split0 == -1 ? ToInt32(param) : ToInt32(param.Substring(0, split0));
            var g = split0 == -1 ? r : split1 == -1 ? ToInt32(param.Substring(split0 + 1)) : ToInt32(param.Substring(split0 + 1, split1 - split0 - 1));
            var b = split1 == -1 ? g : split2 == -1 ? ToInt32(param.Substring(split1 + 1)) : ToInt32(param.Substring(split1 + 1, split2 - split1 - 1));
            var a = split2 == -1 ? b : ToInt32(param.Substring(split2 + 1));

            return new Vector4(r, g, b, a);
        }

        public static Color GetColorFromHDR(this Color color)
        {
            color.r /= color.a;
            color.g /= color.a;
            color.b /= color.a;

            return color;
        }

        public static Int32 ToAlpha(this string param)
        {
            if (param == null) return 255;

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            return split2 == -1 ? 255 : ToInt32(param.Substring(split2 + 1));
        }
    }

    [System.Serializable]
    public struct HSBColor
    {
        public float h;
        public float s;
        public float b;
        public float a;

        public HSBColor(float h, float s, float b, float a)
        {
            this.h = h;
            this.s = s;
            this.b = b;
            this.a = a;
        }

        public HSBColor(float h, float s, float b)
        {
            this.h = h;
            this.s = s;
            this.b = b;
            this.a = 1f;
        }

        public HSBColor(Color col)
        {
            HSBColor temp = col;
            h = temp.h;
            s = temp.s;
            b = temp.b;
            a = temp.a;
        }

        public static implicit operator HSBColor(Color color)
        {
            HSBColor ret = new HSBColor(0f, 0f, 0f, color.a);

            float r = color.r;
            float g = color.g;
            float b = color.b;

            float max = Mathf.Max(r, Mathf.Max(g, b));

            if (max <= 0)
            {
                return ret;
            }

            float min = Mathf.Min(r, Mathf.Min(g, b));
            float dif = max - min;

            if (max > min)
            {
                if (g == max)
                {
                    ret.h = (b - r) / dif * 60f + 120f;
                }
                else if (b == max)
                {
                    ret.h = (r - g) / dif * 60f + 240f;
                }
                else if (b > g)
                {
                    ret.h = (g - b) / dif * 60f + 360f;
                }
                else
                {
                    ret.h = (g - b) / dif * 60f;
                }
                if (ret.h < 0)
                {
                    ret.h = ret.h + 360f;
                }
            }
            else
            {
                ret.h = 0;
            }

            ret.h *= 1f / 360f;
            ret.s = (dif / max) * 1f;
            ret.b = max;

            return ret;
        }

        public static implicit operator Color(HSBColor hsbColor)
        {
            float r = hsbColor.b;
            float g = hsbColor.b;
            float b = hsbColor.b;
            if (hsbColor.s != 0)
            {
                float max = hsbColor.b;
                float dif = hsbColor.b * hsbColor.s;
                float min = hsbColor.b - dif;

                float h = hsbColor.h * 360f;

                if (h < 60f)
                {
                    r = max;
                    g = h * dif / 60f + min;
                    b = min;
                }
                else if (h < 120f)
                {
                    r = -(h - 120f) * dif / 60f + min;
                    g = max;
                    b = min;
                }
                else if (h < 180f)
                {
                    r = min;
                    g = max;
                    b = (h - 120f) * dif / 60f + min;
                }
                else if (h < 240f)
                {
                    r = min;
                    g = -(h - 240f) * dif / 60f + min;
                    b = max;
                }
                else if (h < 300f)
                {
                    r = (h - 240f) * dif / 60f + min;
                    g = min;
                    b = max;
                }
                else if (h <= 360f)
                {
                    r = max;
                    g = min;
                    b = -(h - 360f) * dif / 60 + min;
                }
                else
                {
                    r = 0;
                    g = 0;
                    b = 0;
                }
            }

            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), hsbColor.a);
        }

        public override string ToString()
        {
            return "H:" + h + " S:" + s + " B:" + b;
        }

        public static HSBColor Lerp(HSBColor a, HSBColor b, float t)
        {
            float h, s;

            //check special case black (color.b==0): interpolate neither hue nor saturation!
            //check special case grey (color.s==0): don't interpolate hue!
            if (a.b == 0)
            {
                h = b.h;
                s = b.s;
            }
            else if (b.b == 0)
            {
                h = a.h;
                s = a.s;
            }
            else
            {
                if (a.s == 0)
                {
                    h = b.h;
                }
                else if (b.s == 0)
                {
                    h = a.h;
                }
                else
                {
                    // works around bug with LerpAngle
                    float angle = Mathf.LerpAngle(a.h * 360f, b.h * 360f, t);
                    while (angle < 0f)
                        angle += 360f;
                    while (angle > 360f)
                        angle -= 360f;
                    h = angle / 360f;
                }
                s = Mathf.Lerp(a.s, b.s, t);
            }
            return new HSBColor(h, s, Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));
        }
    }

    public static class ArrayExtensions
    {

        public static void Add<T>(ref T[] array, T item)
        {
            System.Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
        }

        public static bool ArrayEquals<T>(T[] lhs, T[] rhs)
        {
            if (lhs == null || rhs == null)
                return lhs == rhs;

            if (lhs.Length != rhs.Length)
                return false;

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!lhs[i].Equals(rhs[i]))
                    return false;

            }
            return true;
        }

        public static bool ArrayReferenceEquals<T>(T[] lhs, T[] rhs)
        {
            if (lhs == null || rhs == null)
                return lhs == rhs;

            if (lhs.Length != rhs.Length)
                return false;

            for (int i = 0; i < lhs.Length; i++)
            {
                if (!object.ReferenceEquals(lhs[i], rhs[i]))
                    return false;

            }
            return true;
        }

        public static void AddRange<T>(ref T[] array, T[] items)
        {
            int size = array.Length;
            System.Array.Resize(ref array, array.Length + items.Length);
            for (int i = 0; i < items.Length; i++)
                array[size + i] = items[i];
        }

        public static void Insert<T>(ref T[] array, int index, T item)
        {
            ArrayList a = new ArrayList();
            a.AddRange(array);
            a.Insert(index, item);
            array = a.ToArray(typeof(T)) as T[];
        }

        public static void Remove<T>(ref T[] array, T item)
        {
            List<T> newList = new List<T>(array);
            newList.Remove(item);
            array = newList.ToArray();
        }

        public static List<T> FindAll<T>(T[] array, Predicate<T> match)
        {
            List<T> list = new List<T>(array);
            return list.FindAll(match);
        }

        public static T Find<T>(T[] array, Predicate<T> match)
        {
            List<T> list = new List<T>(array);
            return list.Find(match);
        }

        public static int FindIndex<T>(T[] array, Predicate<T> match)
        {
            List<T> list = new List<T>(array);
            return list.FindIndex(match);
        }

        public static int IndexOf<T>(T[] array, T value)
        {
            List<T> list = new List<T>(array);
            return list.IndexOf(value);
        }

        public static int LastIndexOf<T>(T[] array, T value)
        {
            List<T> list = new List<T>(array);
            return list.LastIndexOf(value);
        }

        public static void RemoveAt<T>(ref T[] array, int index)
        {
            List<T> list = new List<T>(array);
            list.RemoveAt(index);
            array = list.ToArray();
        }

        public static bool Contains<T>(T[] array, T item)
        {
            List<T> list = new List<T>(array);
            return list.Contains(item);
        }

        public static void Clear<T>(ref T[] array)
        {
            System.Array.Clear(array, 0, array.Length);
            System.Array.Resize(ref array, 0);
        }

        //me
        public static int Push<T>(this T[] source, T value)
        {
            var index = Array.IndexOf(source, default(T));

            if (index != -1)
            {
                source[index] = value;
            }

            return index;
        }

        public static T ReadAtPosition<T>(this byte[] buffer, int position)
            where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            var bytes = new byte[size];

            Array.Copy(buffer, position, bytes, 0, size);
            T stuff;
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return stuff;
        }
    }

    public static class OtherExt
    {
        public static void ReverseNormals(this Mesh mesh)
        {
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
                normals[i] = -normals[i];
            mesh.normals = normals;

            for (int m = 0; m < mesh.subMeshCount; m++)
            {
                int[] triangles = mesh.GetTriangles(m);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int temp = triangles[i + 0];
                    triangles[i + 0] = triangles[i + 1];
                    triangles[i + 1] = temp;
                }
                mesh.SetTriangles(triangles, m);
            }
        }

        public static GameObject CreateSubModel(this Transform source, SkinnedMeshRenderer skinnedMesh)
        {
            GameObject Temp = new GameObject(skinnedMesh.gameObject.name);
            Temp.transform.parent = source;

            SkinnedMeshRenderer TempRenderer = Temp.AddComponent<SkinnedMeshRenderer>();

            Transform[] ObjectBones = new Transform[skinnedMesh.bones.Length];

            for (int i = 0; i < skinnedMesh.bones.Length; i++)
            {
                ObjectBones[i] = FindChildByName(skinnedMesh.bones[i].name, source);
            }

            TempRenderer.bones = ObjectBones;
            TempRenderer.sharedMesh = skinnedMesh.sharedMesh;
            TempRenderer.sharedMaterials = skinnedMesh.sharedMaterials;
            TempRenderer.updateWhenOffscreen = true;
            return TempRenderer.gameObject;
        }

        private static Transform FindChildByName(string Name, Transform GO)
        {
            Transform ReturnObj;

            if (GO.name == Name)
                return GO.transform;

            foreach (Transform child in GO)
            {
                ReturnObj = FindChildByName(Name, child);

                if (ReturnObj != null)
                    return ReturnObj;
            }

            return null;
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }

        public static T[] RemoveAt<T>(this T[] source, int index)
        {
            T[] dest = new T[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }

        public static Quaternion Normalize(this Quaternion q)
        {
            float mag = Mathf.Sqrt(Quaternion.Dot(q, q));

            if (mag < Mathf.Epsilon)
                return Quaternion.identity;

            return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        }

        public static Quaternion ConvertToUnity(this Quaternion input)
        {
            return new Quaternion(
                -input.x,   // -(  right = -left  )
                -input.z,   // -(     up =  up     )
                -input.y,   // -(forward =  forward)
                 input.w
            );
        }

        public static Quaternion GetNormalized(this Quaternion q)
        {
            float f = 1f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return new Quaternion(q.x * f, q.y * f, q.z * f, q.w * f);
        }

        public static Vector3 Multiply(this Vector3 a, Vector3 d)
        {
            a.x *= d.x;
            a.y *= d.y;
            a.z *= d.z;
            return a;
        }

        public static Vector3 Multiply2 (this Vector3 a, Vector3 d)
        {
            return new Vector3(a.x * d.x, a.y * d.y, a.z * d.z);
        }

        public static string GetTransformPath(this Transform to, Transform from)
        {
            var target = to;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (to != from)
            {
                while (true)
                {

                    sb.Insert(0, target.name + "/");
                    target = target.parent;
                    if (target == null || target == from) break;

                }
                sb.Remove(sb.Length - 1, 1);
            }
            var calculateTransformPath = sb.ToString();

            return calculateTransformPath;
        }

        public static short[] ReadAnimationFrameValues(this System.IO.BinaryReader br, int count)
        {
            /*
             * RLE data:
             * byte compressed_length - compressed number of values in the data
             * byte uncompressed_length - uncompressed number of values in run
             * short values[compressed_length] - values in the run, the last value is repeated to reach the uncompressed length
             */
            var values = new short[count];

            for (var i = 0; i < count; /* i = i */)
            {
                var run = br.ReadBytes(2); // read the compressed and uncompressed lengths
                var vals = br.ReadShortArray(run[0]); // read the compressed data
                for (var j = 0; j < run[1] && i < count; i++, j++)
                {
                    var idx = Math.Min(run[0] - 1, j); // value in the data or the last value if we're past the end
                    values[i] = vals[idx];
                }
            }

            return values;
        }

        public static object GetMemberValue(this MemberInfo member, object obj)
        {
            if (member is FieldInfo)
            {
                return (member as FieldInfo).GetValue(obj);
            }
            if (member is PropertyInfo)
            {
                return (member as PropertyInfo).GetGetMethod(nonPublic: true).Invoke(obj, null);
            }
            throw new ArgumentException("Can't get the value of a " + member.GetType().Name);
        }

        public static System.Text.StringBuilder PrintProperties(this object o, System.Text.StringBuilder Input = null)
        {
            if(Input == null)
                Input = new System.Text.StringBuilder();

            IEnumerable<MemberInfo> Members = o.GetType().GetProperties().Where(p => p.CanRead || p.CanWrite).Cast<MemberInfo>().Concat(o.GetType().GetFields());

            Input.AppendLine(o.GetType().Name);
            Input.AppendLine("{");

            foreach (MemberInfo a in Members)
            {
                if (!a.IsDefined(typeof(ObsoleteAttribute), true))
                {
                    object value = a is FieldInfo ? ((FieldInfo)a).GetValue(o) : a is MemberInfo ? a.GetMemberValue(o) : ((PropertyInfo)a).GetValue(o, null);
                    Input.AppendLine(string.Format("     {0} = {1}", a.Name, value != null ? value.ToString() : "<NULL>"));
                }
            }

            Input.AppendLine("}");

            return Input;
        }
    }

    public static class StreamExtension
    {
        public static void CopyToLimited(this System.IO.Stream inputStream, System.IO.Stream outputStream, long limit, int bufferSize = 81920)
        {
            long bytesLeftToRead = limit;

            if (bufferSize > limit)
            {
                bufferSize = (int)limit;
            }

            byte[] buffer = new byte[bufferSize];

            while (bytesLeftToRead > 0)
            {
                int bytesToRead = bufferSize;

                //if we're about to read over the limit, clamp it down to whatever is left
                if ((bytesLeftToRead - bytesToRead) < 0)
                {
                    bytesToRead = (int)bytesLeftToRead;
                }

                int bytesRead = inputStream.Read(buffer, 0, bytesToRead);

                //now immediately write to the output stream

                outputStream.Write(buffer, 0, bytesRead);

                bytesLeftToRead -= bytesRead;
            }

        }

        public static void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] b = new byte[32768];
            int r;
            while ((r = input.Read(b, 0, b.Length)) > 0)
                output.Write(b, 0, r);
        }
    }

    public static class BinaryExtension
    {
        /// <summary>
        /// Read a fixed number of bytes from the reader and parse out an optionally null-terminated string
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <param name="encoding">The text encoding to use</param>
        /// <param name="length">The number of bytes to read</param>
        /// <returns>The string that was read</returns>
        public static string ReadFixedLengthString(this System.IO.BinaryReader br, System.Text.Encoding encoding, int length)
        {
            var bstr = br.ReadBytes(length).TakeWhile(b => b != 0).ToArray();
            return encoding.GetString(bstr);
        }

        /// <summary>
        /// Read a variable number of bytes into a string until a null terminator is reached.
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <returns>The string that was read</returns>
        public static string ReadNullTerminatedString(this System.IO.BinaryReader br)
        {
            var str = "";
            char c;
            while ((c = br.ReadChar()) != 0)
            {
                str += c;
            }
            return str;
        }

        public static float[] ReadSingleArray(this System.IO.BinaryReader br, int num)
        {
            var arr = new float[num];
            for (var i = 0; i < num; i++) arr[i] = br.ReadSingle();
            return arr;
        }

        public static int[] ReadIntArray(this System.IO.BinaryReader br, int num)
        {
            var arr = new int[num];
            for (var i = 0; i < num; i++) arr[i] = br.ReadInt32();
            return arr;
        }

        /// <summary>
        /// Read an array of short unsigned integers
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <param name="num">The number of values to read</param>
        /// <returns>The resulting array</returns>
        public static ushort[] ReadUshortArray(this System.IO.BinaryReader br, int num)
        {
            var arr = new ushort[num];
            for (var i = 0; i < num; i++) arr[i] = br.ReadUInt16();
            return arr;
        }

        /// <summary>
        /// Read an array of short integers
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <param name="num">The number of values to read</param>
        /// <returns>The resulting array</returns>
        public static short[] ReadShortArray(this System.IO.BinaryReader br, int num)
        {
            var arr = new short[num];
            for (var i = 0; i < num; i++) arr[i] = br.ReadInt16();
            return arr;
        }
    }
}