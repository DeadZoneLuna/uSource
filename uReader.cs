using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System;
using System.Text;

namespace uSource
{
    public class uReader : BinaryReader
    {
        public Stream InputStream;

        public uReader(Stream InputStream) 
            : base(InputStream)
        {
            this.InputStream = InputStream;

            if (!InputStream.CanRead)
                throw new InvalidDataException("Stream unreadable!");
        }

        public byte[] GetBytes(int Count, long Offset)
        {
            if (!Offset.Equals(0) && !Offset.Equals(InputStream.Position))
                InputStream.Seek(Offset, SeekOrigin.Begin);

            byte[] Buffer = new byte[Count];
            InputStream.Read(Buffer, 0, Buffer.Length);

            return Buffer;
        }

        public void ReadType<T>(ref T Variable, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            Byte[] Buffer = new byte[Marshal.SizeOf(typeof(T))];
            InputStream.Read(Buffer, 0, Buffer.Length);

            GCHandle Handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            try
            {
                Variable = (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                Handle.Free();
            }
        }

        public void ReadArray<T>(ref T[] Array, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            for (Int32 i = 0; i < Array.Length; i++)
                ReadType(ref Array[i]);
        }

        public void ReadTypeFixed<T>(ref T Variable, Int32 TypeSizeOf, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            Byte[] Buffer = new byte[TypeSizeOf];
            InputStream.Read(Buffer, 0, Buffer.Length);

            GCHandle Handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            try
            {
                Variable = (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                Handle.Free();
            }
        }

        public void ReadArrayFixed<T>(ref T[] Array, Int32 TypeSizeOf, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            for (Int32 i = 0; i < Array.Length; i++)
                ReadTypeFixed(ref Array[i], TypeSizeOf);
        }

        [ThreadStatic]
        private static StringBuilder _sBuilder;
        public String ReadNullTerminatedString(long? Offset = null)
        {
            if (Offset.HasValue && !Offset.Value.Equals(InputStream.Position))
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            if (_sBuilder == null) 
                _sBuilder = new StringBuilder();
            else 
                _sBuilder.Remove(0, _sBuilder.Length);

            while (true)
            {
                Char c = (char)InputStream.ReadByte();
                if (c == 0) 
                    return _sBuilder.ToString();

                _sBuilder.Append(c);
            }
        }

        public Vector3 ReadVector2D()
        {
            Vector2 Vector2D;// = new Vector2(ReadSingle(), ReadSingle());
            Vector2D.x = ReadSingle();
            Vector2D.y = ReadSingle();

            return Vector2D;
        }

        public Vector3 ReadVector3D(bool SwapZY = true)
        {
            Vector3 Vector3D;// = new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
            Vector3D.x = ReadSingle();
            Vector3D.y = ReadSingle();
            Vector3D.z = ReadSingle();

            if (SwapZY)
            {
                //float x = Vector3D.x;
                //float y = Vector3D.y;
                //float z = Vector3D.z;

                Single tempX = Vector3D.x;

                Vector3D.x = -Vector3D.y;
                Vector3D.y = Vector3D.z;
                Vector3D.z = tempX;
            }

            return Vector3D;
        }

        public Vector3 ReadVector4D()
        {
            Vector4 Vector4D;
            Vector4D.x = ReadSingle();
            Vector4D.y = ReadSingle();
            Vector4D.z = ReadSingle();
            Vector4D.w = ReadSingle();

            return Vector4D;
        }
    }
}
