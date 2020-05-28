using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System;

namespace Engine.Source
{
    public class MemUtils : BinaryReader
    {
        Stream InputStream;

        public MemUtils(Stream InputStream)
            : base(InputStream)
        {
            this.InputStream = InputStream;

            if (!InputStream.CanRead)
                throw new FileLoadException();
        }

        public void ReadType<T>(ref T Variable, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            Byte[] Buffer = new byte[Marshal.SizeOf(typeof(T))];
            InputStream.Read(Buffer, 0, Buffer.Length);

            GCHandle Handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            Variable = (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));
        }

        public void ReadArray<T>(ref T[] Array, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            for (Int32 i = 0; i < Array.Length; i++)
                ReadType(ref Array[i]);
        }

        public String ReadNullTerminatedString(long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            return new string(ReadChars(128)).Split('\0')[0];
        }

        public Vector3 ReadVector3D(bool SwapZY = true)
        {
            Vector3 Vector3D = new Vector3(ReadSingle(), ReadSingle(), ReadSingle());

            if (SwapZY)
            {
                Single AxisY = Vector3D.y;

                Vector3D.x = -Vector3D.x;
                Vector3D.y = Vector3D.z;
                Vector3D.z = -AxisY;
            }

            return Vector3D;
        }
    }
}
