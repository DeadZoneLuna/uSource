using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSource.Formats.Source.VTF
{
    public class VTFFile
    {
        private const String VTFHeader = "VTF";
        public VTFHeader Header { get; set; }

        public VTFResource[] Resources { get; set; }

        public VTFImage LowResImage { get; set; }

        public Texture2D[,] Frames { get; set; }

        public UInt16 Width;
        public UInt16 Height;

        //http://wiki.xentax.com/index.php/Source_VTF
        /// <summary>
        /// Parser VTF format
        /// <br>Supported versions: 7.1 - 7.5 (maybe 7.0)</br>
        /// </summary>
        /// <param name="stream">Stream of input file</param>
        /// <param name="FileName">Name of input file (optional)</param>
        public VTFFile(Stream stream, String FileName = "")
        {
            using (uReader FileStream = new uReader(stream))
            {
                String TempHeader = FileStream.ReadFixedLengthString(Encoding.ASCII, 4);
                if (TempHeader != VTFHeader)
                    throw new Exception("Invalid VTF header. Expected '" + VTFHeader + "', got '" + TempHeader + "'.");

                Header = new VTFHeader();

                UInt32 VersionMajor = FileStream.ReadUInt32();
                UInt32 VersionMinor = FileStream.ReadUInt32();
                Decimal Version = VersionMajor + (VersionMinor / 10m); // e.g. 7.3
                Header.Version = Version;

                UInt32 headerSize = FileStream.ReadUInt32();
                Width = FileStream.ReadUInt16();
                Height = FileStream.ReadUInt16();

                Header.Flags = (VTFImageFlag)FileStream.ReadUInt32();

                UInt16 NumFrames = FileStream.ReadUInt16();
                UInt16 FirstFrame = FileStream.ReadUInt16();

                FileStream.ReadBytes(4); // padding

                Header.Reflectivity = FileStream.ReadVector3D(false);

                FileStream.ReadBytes(4); // padding

                Header.BumpmapScale = FileStream.ReadSingle();

                VTFImageFormat HighResImageFormat = (VTFImageFormat)FileStream.ReadUInt32();
                Byte MipmapCount = FileStream.ReadByte();
                VTFImageFormat LowResImageFormat = (VTFImageFormat)FileStream.ReadUInt32();
                Byte LowResWidth = FileStream.ReadByte();
                Byte LowResHeight = FileStream.ReadByte();

                UInt16 Depth = 1;
                UInt32 NumResources = 0;

                if (Version >= 7.2m)
                {
                    Depth = FileStream.ReadUInt16();
                }
                if (Version >= 7.3m)
                {
                    FileStream.ReadBytes(3);
                    NumResources = FileStream.ReadUInt32();
                    FileStream.ReadBytes(8);
                }

                Int32 NumFaces = 1;
                if (Header.Flags.HasFlag(VTFImageFlag.TEXTUREFLAGS_ENVMAP))
                {
                    NumFaces = Version < 7.5m && FirstFrame != 0xFFFF ? 7 : 6;
                }

                VTFImageFormatInfo HighResFormatInfo = VTFImageFormatInfo.FromFormat(HighResImageFormat);
                VTFImageFormatInfo LowResFormatInfo = VTFImageFormatInfo.FromFormat(LowResImageFormat);

                Int32 ThumbnailSize = LowResImageFormat == VTFImageFormat.IMAGE_FORMAT_NONE ? 0 : LowResFormatInfo.GetSize(LowResWidth, LowResHeight);

                UInt32 ThumbnailOffset = headerSize;
                Int64 DataOffset = headerSize + ThumbnailSize;

                Resources = new VTFResource[NumResources];
                for (Int32 i = 0; i < NumResources; i++)
                {
                    VTFResourceType type = (VTFResourceType)FileStream.ReadUInt32();
                    UInt32 DataSize = FileStream.ReadUInt32();
                    switch (type)
                    {
                        case VTFResourceType.LowResImage:
                            // Low res image
                            ThumbnailOffset = DataSize;
                            break;
                        case VTFResourceType.Image:
                            // Regular image
                            DataOffset = DataSize;
                            break;
                        case VTFResourceType.Sheet:
                        case VTFResourceType.CRC:
                        case VTFResourceType.TextureLodSettings:
                        case VTFResourceType.TextureSettingsEx:
                        case VTFResourceType.KeyValueData:
                            // todo
                            Resources[i] = new VTFResource
                            {
                                Type = type,
                                Data = DataSize
                            };
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type), (uint)type, "Unknown resource type");
                    }
                }

                if (LowResImageFormat != VTFImageFormat.IMAGE_FORMAT_NONE)
                {
                    FileStream.BaseStream.Position = ThumbnailOffset;
                    Int32 thumbSize = LowResFormatInfo.GetSize(LowResWidth, LowResHeight);
                    LowResImage = new VTFImage
                    {
                        Format = LowResImageFormat,
                        Width = LowResWidth,
                        Height = LowResHeight,
                        Data = FileStream.ReadBytes(thumbSize)
                    };
                }

                Boolean ConvertToBGRA32 = true;
                Boolean hasAlpha = true;
                switch (HighResImageFormat)
                {
                    //Unity support this formats natively
                    case VTFImageFormat.IMAGE_FORMAT_A8:
                    case VTFImageFormat.IMAGE_FORMAT_ABGR8888:
                    case VTFImageFormat.IMAGE_FORMAT_ARGB8888:
                    case VTFImageFormat.IMAGE_FORMAT_BGRA4444:
                    case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                    case VTFImageFormat.IMAGE_FORMAT_DXT3:
                    case VTFImageFormat.IMAGE_FORMAT_DXT5:
                    case VTFImageFormat.IMAGE_FORMAT_RGBA8888:
                    case VTFImageFormat.IMAGE_FORMAT_BGRA8888:
                    case VTFImageFormat.IMAGE_FORMAT_BGRX8888:
                    case VTFImageFormat.IMAGE_FORMAT_RGBA16161616F:
                    case VTFImageFormat.IMAGE_FORMAT_RGBA16161616:
                        ConvertToBGRA32 = false;
                        break;
                    case VTFImageFormat.IMAGE_FORMAT_BGR565:
                    case VTFImageFormat.IMAGE_FORMAT_RGB565:
                    case VTFImageFormat.IMAGE_FORMAT_DXT1:
                    case VTFImageFormat.IMAGE_FORMAT_RGB888:
                        hasAlpha = false;
                        ConvertToBGRA32 = false;
                        break;
                }

                FileStream.BaseStream.Position = DataOffset;
                Frames = new Texture2D[NumFrames, NumFaces];
                List<Byte>[] FramesData = new List<Byte>[NumFrames];
                for (Int32 MipLevel = MipmapCount - 1; MipLevel >= 0; MipLevel--)
                {
                    for (Int32 FrameID = 0; FrameID < NumFrames; FrameID++)
                    {
                        if (FramesData[FrameID] == null)
                            FramesData[FrameID] = new List<Byte>();

                        for (Int32 FaceID = 0; FaceID < NumFaces; FaceID++)
                        {
                            for (Int32 SliceID = 0; SliceID < Depth; SliceID++)
                            {
                                Int32 Wid = GetMipSize(Width, MipLevel);
                                Int32 Hei = GetMipSize(Height, MipLevel);
                                Int32 DataSize = HighResFormatInfo.GetSize(Wid, Hei);

                                if(ConvertToBGRA32)
                                    FramesData[FrameID].InsertRange(0, VTFImageFormatInfo.FromFormat(HighResImageFormat).ConvertToBgra32(FileStream.ReadBytes(DataSize), Wid, Hei));
                                else
                                    FramesData[FrameID].InsertRange(0, FileStream.ReadBytes(DataSize));
                            }
                        }
                    }
                }

                TextureFormat InternalFormat = TextureFormat.BGRA32;
                Boolean needCompress = false;
                switch (HighResImageFormat)
                {
                    case VTFImageFormat.IMAGE_FORMAT_A8:
                        InternalFormat = TextureFormat.Alpha8;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_ABGR8888:
                    case VTFImageFormat.IMAGE_FORMAT_ARGB8888:
                        InternalFormat = TextureFormat.ARGB32;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_BGR565:
                    case VTFImageFormat.IMAGE_FORMAT_RGB565:
                        InternalFormat = TextureFormat.RGB565;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_BGRA4444:
                        InternalFormat = TextureFormat.RGBA4444;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_DXT1:
                    case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                        InternalFormat = TextureFormat.DXT1;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_DXT3:
                    case VTFImageFormat.IMAGE_FORMAT_DXT5:
                        InternalFormat = TextureFormat.DXT5;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_RGB888:
                        InternalFormat = TextureFormat.RGB24;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_RGBA8888:
                        InternalFormat = TextureFormat.RGBA32;
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_RGBA16161616F:
                    case VTFImageFormat.IMAGE_FORMAT_RGBA16161616:
                        InternalFormat = TextureFormat.RGBAHalf;
                        break;

                    default:
                        //needCompress = true;
                        break;
                }

                Boolean mipmaps = MipmapCount > 1;
                for (Int32 FrameID = 0; FrameID < NumFrames; FrameID++)
                {
                    for (Int32 FaceID = 0; FaceID < NumFaces; FaceID++)
                    {
                        Frames[FrameID, FaceID] = new Texture2D(Width, Height, InternalFormat, mipmaps);
                        Frames[FrameID, FaceID].name = FileName;
                        Frames[FrameID, FaceID].alphaIsTransparency = hasAlpha;
                        Frames[FrameID, FaceID].LoadRawTextureData(FramesData[FrameID].ToArray());
                        Frames[FrameID, FaceID].Apply();

                        if (needCompress)
                        {
                            Frames[FrameID, FaceID].Compress(false);
                            Debug.LogWarning(FileName + " compressed!");
                        }
                    }
                }
            }
        }

        private static Int32 GetMipSize(Int32 input, Int32 level)
        {
            Int32 res = input >> level;
            if (res < 1) res = 1;
            return res;
        }
    }
}