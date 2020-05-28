using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

public class CustomReader
{
    public Stream InputStream { get; set; }
    BinaryReader BinaryReader;

    public byte[] GetBytes(int Count, long Offset)
    {
        if (!Offset.Equals(0) && !Offset.Equals(InputStream.Position))
            InputStream.Seek(Offset, SeekOrigin.Begin);

        byte[] Buffer = new byte[Count];
        InputStream.Read(Buffer, 0, Buffer.Length);

        return Buffer;
    }

    public T ReadType<T>(long? Offset = null)
    {
        if (Offset.HasValue && !Offset.Value.Equals(InputStream.Position))
            InputStream.Seek(Offset.Value, SeekOrigin.Begin);

        byte[] StrInBytes = new byte[Marshal.SizeOf(typeof(T))];
        InputStream.Read(StrInBytes, 0, StrInBytes.Length);

        GCHandle Handle = GCHandle.Alloc(StrInBytes, GCHandleType.Pinned);
        return (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));
    }

    public T[] ReadType<T>(int Count, long Offset)
    {
        if (!Offset.Equals(0) && !Offset.Equals(InputStream.Position))
            InputStream.Seek(Offset, SeekOrigin.Begin);

        List<T> TList = new List<T>();

        for (int i = 0; i < Count; i++)
            TList.Add(ReadType<T>());

        return TList.ToArray();
    }

    public string ReadNullTerminatedString(long? Offset = null)
    {
        if (Offset.HasValue && !Offset.Value.Equals(InputStream.Position))
            InputStream.Seek(Offset.Value, SeekOrigin.Begin);

        List<byte> StrInBytes = new List<byte>();
        byte b;

        while ((b = (byte)InputStream.ReadByte()) != 0x00)
            StrInBytes.Add(b);

        return Encoding.ASCII.GetString(StrInBytes.ToArray());
    }

    public string[] ReadNullTerminatedString(int[] Array, long Offset)
    {
        if (Offset.Equals(0) && !Offset.Equals(InputStream.Position))
            Offset = InputStream.Position;

        List<string> TList = new List<string>();

        for (int i = 0; i < Array.Length; i++)
            TList.Add(ReadNullTerminatedString(Offset + Array[i]));

        return TList.ToArray();
    }

    public BinaryReader BR(long? Offset = null)
    {
        if (BinaryReader == null)
            BinaryReader = new BinaryReader(InputStream);

        if (Offset.HasValue && !Offset.Value.Equals(InputStream.Position))
        {
            BinaryReader.BaseStream.Seek(Offset.Value, SeekOrigin.Begin);
            InputStream.Seek(Offset.Value, SeekOrigin.Begin);
        }

        return BinaryReader;
    }

    public CustomReader(Stream InputStream)
    {
        this.InputStream = InputStream;

        if (!InputStream.CanRead)
            throw new InvalidOperationException();
    }

    public void Dispose()
    {
        InputStream.Dispose();

        if (BinaryReader != null)
            BinaryReader.BaseStream.Dispose();
    }
}
