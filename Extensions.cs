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
        public static Boolean HasFlag(this Enum variable, Enum value)
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
        public static String ToString(this String param)
        {
            return param;
        }

        public static Int32 ToInt32(this String param, Int32 defValue = 0)
        {
            Int32 intVal;
            Single value;
            return (param != null) ? (Int32.TryParse(param, out intVal) ? intVal : (Single.TryParse(param, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? ((Int32)value) : defValue)) : defValue;
        }

        public static Boolean ToBoolean(this String param)
        {
            return param != null && ToInt32(param) != 0;
        }

        public static Single ToSingle(this String param, Single defValue = 0)
        {
            Single value;
            return (param == null) ? defValue : (Single.TryParse(param.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : 0f);
        }

        public static Vector3 ToVector3(this String param)
        {
            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);

            var x = split0 == -1 ? ToSingle(param) : ToSingle(param.Substring(0, split0));
            var y = split0 == -1 ? 0f : split1 == -1 ? ToSingle(param.Substring(split0 + 1)) : ToSingle(param.Substring(split0 + 1, split1 - split0 - 1));
            var z = split1 == -1 ? 0f : ToSingle(param.Substring(split1 + 1));

            return new Vector3(x, y, z);
        }

        public static Color32 ToColor32(this String param)
        {
            if (param == null) return new Color32(0x00, 0x00, 0x00, 255);

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            var r = split0 == -1 ? ToInt32(param) : ToInt32(param.Substring(0, split0));
            var g = split0 == -1 ? 0 : split1 == -1 ? ToInt32(param.Substring(split0 + 1)) : ToInt32(param.Substring(split0 + 1, split1 - split0 - 1));
            var b = split1 == -1 ? 0 : split2 == -1 ? ToInt32(param.Substring(split1 + 1)) : ToInt32(param.Substring(split1 + 1, split2 - split1 - 1));
            var a = split2 == -1 ? 255 : ToInt32(param.Substring(split2 + 1));

            return new Color32((Byte)r, (Byte)g, (Byte)b, (Byte)a);
        }

        public static Color ToColor(this String param)
        {
            if (param == null) return new Color(0, 0, 0, 1);

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            var r = split0 == -1 ? ToInt32(param) : ToInt32(param.Substring(0, split0));
            var g = split0 == -1 ? 0 : split1 == -1 ? ToInt32(param.Substring(split0 + 1)) : ToInt32(param.Substring(split0 + 1, split1 - split0 - 1));
            var b = split1 == -1 ? 0 : split2 == -1 ? ToInt32(param.Substring(split1 + 1)) : ToInt32(param.Substring(split1 + 1, split2 - split1 - 1));
            var a = split2 == -1 ? 255 : ToInt32(param.Substring(split2 + 1));

            //Single Pow = Mathf.Pow(2, a / 255f);

            //pow( r / 255.0, 2.2 ) * 255

            Color color = new Vector4(ToLinearF(r), ToLinearF(g), ToLinearF(b), ToLinearF(a));

            return color;
        }

        static Single ToLinearF(Int32 color)
        {
            return Mathf.Clamp(color, 0, 255) / 255.0f;
        }

        public static Single SRGB2Linear(this Single s)
        {
            Single output;
            if (s <= 0.0404482362771082f)
                output = s / 12.92f;
            else
                output = Mathf.Pow(((s + 0.055f) / 1.055f), 2.4f);

            return output;
        }

        public static Vector4 ToColorVec(this String param)
        {
            if (param == null) return new Color(0, 0, 0, 200);

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            var r = split0 == -1 ? ToInt32(param) : ToInt32(param.Substring(0, split0));
            var g = split0 == -1 ? r : split1 == -1 ? ToInt32(param.Substring(split0 + 1)) : ToInt32(param.Substring(split0 + 1, split1 - split0 - 1));
            var b = split1 == -1 ? g : split2 == -1 ? ToInt32(param.Substring(split1 + 1)) : ToInt32(param.Substring(split1 + 1, split2 - split1 - 1));
            var a = split2 == -1 ? 200 : ToInt32(param.Substring(split2 + 1));

            return new Vector4(r, g, b, a);
        }

        public static Color GetColorFromHDR(this Color color)
        {
            color.r /= color.a;
            color.g /= color.a;
            color.b /= color.a;

            return color;
        }

        public static Int32 ToAlpha(this String param)
        {
            if (param == null) return 255;

            var split0 = param.IndexOf(' ');
            var split1 = split0 == -1 ? -1 : param.IndexOf(' ', split0 + 1);
            var split2 = split1 == -1 ? -1 : param.IndexOf(' ', split1 + 1);

            return split2 == -1 ? 255 : ToInt32(param.Substring(split2 + 1));
        }
    }

    public static class ArrayExtensions
    {

        public static void Add<T>(ref T[] array, T item)
        {
            System.Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
        }

        public static Boolean ArrayEquals<T>(T[] lhs, T[] rhs)
        {
            if (lhs == null || rhs == null)
                return lhs == rhs;

            if (lhs.Length != rhs.Length)
                return false;

            for (Int32 i = 0; i < lhs.Length; i++)
            {
                if (!lhs[i].Equals(rhs[i]))
                    return false;

            }
            return true;
        }

        public static Boolean ArrayReferenceEquals<T>(T[] lhs, T[] rhs)
        {
            if (lhs == null || rhs == null)
                return lhs == rhs;

            if (lhs.Length != rhs.Length)
                return false;

            for (Int32 i = 0; i < lhs.Length; i++)
            {
                if (!object.ReferenceEquals(lhs[i], rhs[i]))
                    return false;

            }
            return true;
        }

        public static void AddRange<T>(ref T[] array, T[] items)
        {
            Int32 size = array.Length;
            System.Array.Resize(ref array, array.Length + items.Length);
            for (Int32 i = 0; i < items.Length; i++)
                array[size + i] = items[i];
        }

        public static void Insert<T>(ref T[] array, Int32 index, T item)
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

        public static Int32 FindIndex<T>(T[] array, Predicate<T> match)
        {
            List<T> list = new List<T>(array);
            return list.FindIndex(match);
        }

        public static Int32 IndexOf<T>(T[] array, T value)
        {
            List<T> list = new List<T>(array);
            return list.IndexOf(value);
        }

        public static Int32 LastIndexOf<T>(T[] array, T value)
        {
            List<T> list = new List<T>(array);
            return list.LastIndexOf(value);
        }

        public static void RemoveAt<T>(ref T[] array, Int32 index)
        {
            List<T> list = new List<T>(array);
            list.RemoveAt(index);
            array = list.ToArray();
        }

        public static Boolean Contains<T>(T[] array, T item)
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
        public static Int32 Push<T>(this T[] source, T value)
        {
            var index = Array.IndexOf(source, default(T));

            if (index != -1)
            {
                source[index] = value;
            }

            return index;
        }

        public static T ReadAtPosition<T>(this Byte[] buffer, Int32 position)
            where T : struct
        {
            Int32 size = Marshal.SizeOf(typeof(T));
            var bytes = new Byte[size];

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
            for (Int32 i = 0; i < normals.Length; i++)
                normals[i] = -normals[i];
            mesh.normals = normals;

            for (Int32 m = 0; m < mesh.subMeshCount; m++)
            {
                Int32[] triangles = mesh.GetTriangles(m);
                for (Int32 i = 0; i < triangles.Length; i += 3)
                {
                    Int32 temp = triangles[i + 0];
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

            for (Int32 i = 0; i < skinnedMesh.bones.Length; i++)
            {
                ObjectBones[i] = FindChildByName(skinnedMesh.bones[i].name, source);
            }

            TempRenderer.bones = ObjectBones;
            TempRenderer.sharedMesh = skinnedMesh.sharedMesh;
            TempRenderer.sharedMaterials = skinnedMesh.sharedMaterials;
            TempRenderer.updateWhenOffscreen = true;
            return TempRenderer.gameObject;
        }

        private static Transform FindChildByName(String Name, Transform GO)
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

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, Int32 N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }

        public static T[] RemoveAt<T>(this T[] source, Int32 index)
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
            Single mag = Mathf.Sqrt(Quaternion.Dot(q, q));

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
            Single f = 1f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
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

        public static String GetTransformPath(this Transform to, Transform from)
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

        public static short[] ReadAnimationFrameValues(this System.IO.BinaryReader br, Int32 count)
        {
            /*
             * RLE data:
             * Byte compressed_length - compressed number of values in the data
             * Byte uncompressed_length - uncompressed number of values in run
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
                    Input.AppendLine(String.Format("     {0} = {1}", a.Name, value != null ? value.ToString() : "<NULL>"));
                }
            }

            Input.AppendLine("}");

            return Input;
        }
    }

    public static class StreamExtension
    {
        public static void CopyToLimited(this System.IO.Stream inputStream, System.IO.Stream outputStream, long limit, Int32 bufferSize = 81920)
        {
            long bytesLeftToRead = limit;

            if (bufferSize > limit)
            {
                bufferSize = (Int32)limit;
            }

            Byte[] buffer = new Byte[bufferSize];

            while (bytesLeftToRead > 0)
            {
                Int32 bytesToRead = bufferSize;

                //if we're about to read over the limit, clamp it down to whatever is left
                if ((bytesLeftToRead - bytesToRead) < 0)
                {
                    bytesToRead = (Int32)bytesLeftToRead;
                }

                Int32 bytesRead = inputStream.Read(buffer, 0, bytesToRead);

                //now immediately write to the output stream

                outputStream.Write(buffer, 0, bytesRead);

                bytesLeftToRead -= bytesRead;
            }

        }

        public static void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            Byte[] b = new Byte[32768];
            Int32 r;
            while ((r = input.Read(b, 0, b.Length)) > 0)
                output.Write(b, 0, r);
        }
    }

    public static class BinaryExtension
    {
        /// <summary>
        /// Read a fixed number of bytes from the reader and parse out an optionally null-terminated String
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <param name="encoding">The text encoding to use</param>
        /// <param name="length">The number of bytes to read</param>
        /// <returns>The String that was read</returns>
        public static String ReadFixedLengthString(this System.IO.BinaryReader br, System.Text.Encoding encoding, Int32 length)
        {
            var bstr = br.ReadBytes(length).TakeWhile(b => b != 0).ToArray();
            return encoding.GetString(bstr);
        }

        /// <summary>
        /// Read a variable number of bytes into a String until a null terminator is reached.
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <returns>The String that was read</returns>
        public static String ReadNullTerminatedString(this System.IO.BinaryReader br)
        {
            var str = "";
            Char c;
            while ((c = br.ReadChar()) != 0)
            {
                str += c;
            }
            return str;
        }

        public static Single[] ReadSingleArray(this System.IO.BinaryReader br, Int32 num)
        {
            var arr = new Single[num];
            for (var i = 0; i < num; i++) arr[i] = br.ReadSingle();
            return arr;
        }

        public static Int32[] ReadIntArray(this System.IO.BinaryReader br, Int32 num)
        {
            var arr = new Int32[num];
            for (var i = 0; i < num; i++) arr[i] = br.ReadInt32();
            return arr;
        }

        /// <summary>
        /// Read an array of short unsigned integers
        /// </summary>
        /// <param name="br">Binary reader</param>
        /// <param name="num">The number of values to read</param>
        /// <returns>The resulting array</returns>
        public static ushort[] ReadUshortArray(this System.IO.BinaryReader br, Int32 num)
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
        public static short[] ReadShortArray(this System.IO.BinaryReader br, Int32 num)
        {
            var arr = new short[num];
            for (var i = 0; i < num; i++) arr[i] = br.ReadInt16();
            return arr;
        }
    }
}