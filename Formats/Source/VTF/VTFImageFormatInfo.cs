using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace uSource.Formats.Source.VTF
{
    public enum VTFImageFormat
    {
        IMAGE_FORMAT_NONE = -1,
        IMAGE_FORMAT_RGBA8888 = 0,
        IMAGE_FORMAT_ABGR8888,
        IMAGE_FORMAT_RGB888,
        IMAGE_FORMAT_BGR888,
        IMAGE_FORMAT_RGB565,
        IMAGE_FORMAT_I8,
        IMAGE_FORMAT_IA88,
        IMAGE_FORMAT_P8,
        IMAGE_FORMAT_A8,
        IMAGE_FORMAT_RGB888_BLUESCREEN,
        IMAGE_FORMAT_BGR888_BLUESCREEN,
        IMAGE_FORMAT_ARGB8888,
        IMAGE_FORMAT_BGRA8888,
        IMAGE_FORMAT_DXT1,
        IMAGE_FORMAT_DXT3,
        IMAGE_FORMAT_DXT5,
        IMAGE_FORMAT_BGRX8888,
        IMAGE_FORMAT_BGR565,
        IMAGE_FORMAT_BGRX5551,
        IMAGE_FORMAT_BGRA4444,
        IMAGE_FORMAT_DXT1_ONEBITALPHA,
        IMAGE_FORMAT_BGRA5551,
        IMAGE_FORMAT_UV88,
        IMAGE_FORMAT_UVWQ8888,
        IMAGE_FORMAT_RGBA16161616F,
        IMAGE_FORMAT_RGBA16161616,
        IMAGE_FORMAT_UVLX8888,
        IMAGE_FORMAT_R32F,
        IMAGE_FORMAT_RGB323232F,
        IMAGE_FORMAT_RGBA32323232F,
        IMAGE_FORMAT_NV_DST16,
        IMAGE_FORMAT_NV_DST24,
        IMAGE_FORMAT_NV_INTZ,
        IMAGE_FORMAT_NV_RAWZ,
        IMAGE_FORMAT_ATI_DST16,
        IMAGE_FORMAT_ATI_DST24,
        IMAGE_FORMAT_NV_NULL,
        IMAGE_FORMAT_ATI_2N,
        IMAGE_FORMAT_ATI_1N,
    }

    // Uses logic from the excellent (LGPL-licensed) VtfLib, courtesy of Neil Jedrzejewski & Ryan Gregg
    public class VTFImageFormatInfo
    {
        private delegate void TransformPixel(Byte[] data, Int32 offset, Int32 count);

        public VTFImageFormat Format { get; }

        public Int32 BitsPerPixel { get; }
        public Int32 BytesPerPixel { get; }

        public Int32 RedBitsPerPixel { get; }
        public Int32 GreenBitsPerPixel { get; }
        public Int32 BlueBitsPerPixel { get; }
        public Int32 AlphaBitsPerPixel { get; }

        public Int32 RedIndex { get; }
        public Int32 GreenIndex { get; }
        public Int32 BlueIndex { get; }
        public Int32 AlphaIndex { get; }

        public Boolean IsCompressed { get; }
        public Boolean IsSupported { get; }

        private readonly TransformPixel _pixelTransform;

        private readonly Boolean _is8Aligned;
        private readonly Boolean _is16Aligned;
        private readonly Boolean _is32Aligned;
        private readonly Mask[] _masks;

        public static VTFImageFormatInfo FromFormat(VTFImageFormat imageFormat) => ImageFormats[imageFormat];

        private VTFImageFormatInfo(
            VTFImageFormat format,
            Int32 bitsPerPixel, Int32 bytesPerPixel,
            Int32 redBitsPerPixel, Int32 greenBitsPerPixel, Int32 blueBitsPerPixel, Int32 alphaBitsPerPixel,
            Int32 redIndex, Int32 greenIndex, Int32 blueIndex, Int32 alphaIndex,
            Boolean isCompressed, Boolean isSupported,
            TransformPixel pixelTransform = null
            )
        {
            Format = format;

            BitsPerPixel = bitsPerPixel;
            BytesPerPixel = bytesPerPixel;

            RedBitsPerPixel = redBitsPerPixel;
            GreenBitsPerPixel = greenBitsPerPixel;
            BlueBitsPerPixel = blueBitsPerPixel;
            AlphaBitsPerPixel = alphaBitsPerPixel;

            RedIndex = redIndex;
            GreenIndex = greenIndex;
            BlueIndex = blueIndex;
            AlphaIndex = alphaIndex;

            IsCompressed = isCompressed;
            IsSupported = isSupported;

            _pixelTransform = pixelTransform;

            _is8Aligned = (redBitsPerPixel   == 0 || redBitsPerPixel   == 8) &&
                          (greenBitsPerPixel == 0 || greenBitsPerPixel == 8) &&
                          (blueBitsPerPixel  == 0 || blueBitsPerPixel  == 8) &&
                          (alphaBitsPerPixel == 0 || alphaBitsPerPixel == 8);

            _is16Aligned = (redBitsPerPixel   == 0 || redBitsPerPixel   == 16) &&
                           (greenBitsPerPixel == 0 || greenBitsPerPixel == 16) &&
                           (blueBitsPerPixel  == 0 || blueBitsPerPixel  == 16) &&
                           (alphaBitsPerPixel == 0 || alphaBitsPerPixel == 16);

            _is32Aligned = (redBitsPerPixel   == 0 || redBitsPerPixel   == 32) &&
                           (greenBitsPerPixel == 0 || greenBitsPerPixel == 32) &&
                           (blueBitsPerPixel  == 0 || blueBitsPerPixel  == 32) &&
                           (alphaBitsPerPixel == 0 || alphaBitsPerPixel == 32);

            if (!_is8Aligned && !_is16Aligned && !_is32Aligned)
            {
                var masks = new[] {
                    new Mask('r', redBitsPerPixel, redIndex),
                    new Mask('g', greenBitsPerPixel, greenIndex),
                    new Mask('b', blueBitsPerPixel, blueIndex),
                    new Mask('a', alphaBitsPerPixel, alphaIndex),
                }.OrderBy(x => x.Index).ToList();

                var offset = bitsPerPixel;
                foreach (var m in masks)
                {
                    offset -= m.Size;
                    m.Offset = offset;
                }

                var dict = masks.ToDictionary(x => x.Component, x => x);
                _masks = new[] { dict['b'], dict['g'], dict['r'], dict['a'] };
            }
        }

        /// <summary>
        /// Gets the size of the image data for this format in bytes
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <returns>The size of the image, in bytes</returns>
        public Int32 GetSize(Int32 width, Int32 height)
        {
            switch (Format)
            {
                case VTFImageFormat.IMAGE_FORMAT_DXT1:
                case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                    if (width < 4 && width > 0) width = 4;
                    if (height < 4 && height > 0) height = 4;
                    return (width + 3) / 4 * ((height + 3) / 4) * 8;
                case VTFImageFormat.IMAGE_FORMAT_DXT3:
                case VTFImageFormat.IMAGE_FORMAT_DXT5:
                    if (width < 4 && width > 0) width = 4;
                    if (height < 4 && height > 0) height = 4;
                    return (width + 3) / 4 * ((height + 3) / 4) * 16;
                default:
                    return width * height * BytesPerPixel;
            }
        }

        /// <summary>
        /// Convert an array of data in this format to a standard bgra8888 format.
        /// </summary>
        /// <param name="data">The data in this format</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <returns>The data in bgra8888 format.</returns>
        public Byte[] ConvertToBgra32(Byte[] data, Int32 width, Int32 height)
        {
            var buffer = new Byte[width * height * 4];

            // No format, return blank array
            if (Format == VTFImageFormat.IMAGE_FORMAT_NONE) return buffer;

            // This is the exact format we want, take the fast path
            else if (Format == VTFImageFormat.IMAGE_FORMAT_BGRA8888)
            {
                Array.Copy(data, buffer, buffer.Length);
                return buffer;
            }

            // Handle compressed formats
            else if (IsCompressed)
            {
                switch (Format)
                {
                    case VTFImageFormat.IMAGE_FORMAT_DXT1:
                    case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                        DXTDecompress.DecompressDXT1(buffer, data, width, height);
                        break;
                    case VTFImageFormat.IMAGE_FORMAT_DXT3:
                        DXTDecompress.DecompressDXT3(buffer, data, width, height);
                        break;
                    case VTFImageFormat.IMAGE_FORMAT_DXT5:
                        DXTDecompress.DecompressDXT5(buffer, data, width, height);
                        break;
                    default:
                        throw new NotImplementedException($"Unsupported format: {Format}");
                }
            }

            // Handle simple Byte-aligned data
            else if (_is8Aligned)
            {
                for (Int32 i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                {
                    buffer[j + 0] = BlueIndex  >= 0 ? data[i + BlueIndex ] : (Byte) 0  ; // b
                    buffer[j + 1] = GreenIndex >= 0 ? data[i + GreenIndex] : (Byte) 0  ; // g
                    buffer[j + 2] = RedIndex   >= 0 ? data[i + RedIndex  ] : (Byte) 0  ; // r
                    buffer[j + 3] = AlphaIndex >= 0 ? data[i + AlphaIndex] : (Byte) 255; // a
                    _pixelTransform?.Invoke(buffer, j, 4);
                }
            }

            // Special logic for half-precision HDR format
            else if (Format == VTFImageFormat.IMAGE_FORMAT_RGBA16161616F)
            {
                var logAverageLuminance = 0.0f;

                var shorts = new ushort[data.Length / 2];
                for (Int32 i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                {
                    for (var k = 0; k < 4; k++)
                    {
                        shorts[j + k] = BitConverter.ToUInt16(data, i + k * 2);
                    }

                    var lum = shorts[j + 0] * 0.299f + shorts[j + 1] * 0.587f + shorts[j + 2] * 0.114f;
                    logAverageLuminance += (float) Math.Log(0.0000000001d + lum);
                }

                logAverageLuminance = (float) Math.Exp(logAverageLuminance / (width * height));

                for (var i = 0; i < shorts.Length; i += 4)
                {
                    TransformFp16(shorts, i, logAverageLuminance);

                    buffer[i + 2] = (Byte)(shorts[i + 0] >> 8);
                    buffer[i + 1] = (Byte)(shorts[i + 1] >> 8);
                    buffer[i + 0] = (Byte)(shorts[i + 2] >> 8);
                    buffer[i + 3] = (Byte)(shorts[i + 3] >> 8);
                }
            }

            // Handle short-aligned data
            else if (_is16Aligned)
            {
                for (Int32 i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                {
                    var b = BlueIndex  >= 0 ? BitConverter.ToUInt16(data, i + BlueIndex  * 2) : UInt16.MinValue;
                    var g = GreenIndex >= 0 ? BitConverter.ToUInt16(data, i + GreenIndex * 2) : UInt16.MinValue;
                    var r = RedIndex   >= 0 ? BitConverter.ToUInt16(data, i + RedIndex   * 2) : UInt16.MinValue;
                    var a = AlphaIndex >= 0 ? BitConverter.ToUInt16(data, i + AlphaIndex * 2) : UInt16.MaxValue;

                    buffer[j + 0] = (Byte) (b >> 8);
                    buffer[j + 1] = (Byte) (g >> 8);
                    buffer[j + 2] = (Byte) (r >> 8);
                    buffer[j + 3] = (Byte) (a >> 8);

                    _pixelTransform?.Invoke(buffer, j, 4);
                }
            }

            // Handle custom-aligned data that fits into a uint
            else if (BitsPerPixel <= 32)
            {
                for (Int32 i = 0, j = 0; i < data.Length; i += BytesPerPixel, j += 4)
                {
                    var val = 0u;
                    for (var k = BytesPerPixel - 1; k >= 0; k--)
                    {
                        val = val << 8;
                        val |= data[i + k];
                    }
                    buffer[j + 0] = _masks[0].Apply(val, BitsPerPixel);
                    buffer[j + 1] = _masks[1].Apply(val, BitsPerPixel);
                    buffer[j + 2] = _masks[2].Apply(val, BitsPerPixel);
                    buffer[j + 3] = _masks[3].Apply(val, BitsPerPixel);
                }
            }

            // Format not supported yet
            else
            {
                throw new NotImplementedException($"Unsupported format: {Format}");
            }

            return buffer;
        }

        private static void TransformFp16(ushort[] shorts, Int32 offset, float logAverageLuminance)
        {
            const float fp16HdrKey = 4.0f;
            const float fp16HdrShift = 0.0f;
            const float fp16HdrGamma = 2.25f;

            float sR = shorts[offset + 0], sG = shorts[offset + 1], sB = shorts[offset + 2];

            var sY = sR * 0.299f + sG * 0.587f + sB * 0.114f;

            var sU = (sB - sY) * 0.565f;
            var sV = (sR - sY) * 0.713f;

            var sTemp = sY;

            sTemp = fp16HdrKey * sTemp / logAverageLuminance;
            sTemp = sTemp / (1.0f + sTemp);
            sTemp = sTemp / sY;

            shorts[offset + 0] = Clamp(Math.Pow((sY + 1.403f * sV) * sTemp + fp16HdrShift, fp16HdrGamma) * 65535.0f);
            shorts[offset + 1] = Clamp(Math.Pow((sY - 0.344f * sU - 0.714f * sV) * sTemp + fp16HdrShift, fp16HdrGamma) * 65535.0f);
            shorts[offset + 2] = Clamp(Math.Pow((sY + 1.770f * sU) * sTemp + fp16HdrShift, fp16HdrGamma) * 65535.0f);
        }

        static ushort Clamp(double sValue)
        {
            if (sValue < UInt16.MinValue) return UInt16.MinValue;
            if (sValue > UInt16.MaxValue) return UInt16.MaxValue;
            return (ushort)sValue;
        }

        private static readonly Dictionary<VTFImageFormat, VTFImageFormatInfo> ImageFormats = new Dictionary<VTFImageFormat, VTFImageFormatInfo>
        {
            {VTFImageFormat.IMAGE_FORMAT_NONE, null},
            {VTFImageFormat.IMAGE_FORMAT_RGBA8888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGBA8888, 32, 4, 8, 8, 8, 8, 0, 1, 2, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_ABGR8888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_ABGR8888, 32, 4, 8, 8, 8, 8, 3, 2, 1, 0, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_RGB888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGB888, 24, 3, 8, 8, 8, 0, 0, 1, 2, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_BGR888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGR888, 24, 3, 8, 8, 8, 0, 2, 1, 0, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_RGB565, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGB565, 16, 2, 5, 6, 5, 0, 0, 1, 2, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_I8, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_I8, 8, 1, 8, 8, 8, 0, 0, -1, -1, -1, false, true, TransformLuminance)},
            {VTFImageFormat.IMAGE_FORMAT_IA88, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_IA88, 16, 2, 8, 8, 8, 8, 0, -1, -1, 1, false, true, TransformLuminance)},
            {VTFImageFormat.IMAGE_FORMAT_P8, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_P8, 8, 1, 0, 0, 0, 0, -1, -1, -1, -1, false, false)},
            {VTFImageFormat.IMAGE_FORMAT_A8, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_A8, 8, 1, 0, 0, 0, 8, -1, -1, -1, 0, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_RGB888_BLUESCREEN, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGB888_BLUESCREEN, 24, 3, 8, 8, 8, 8, 0, 1, 2, -1, false, true, TransformBluescreen)},
            {VTFImageFormat.IMAGE_FORMAT_BGR888_BLUESCREEN, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGR888_BLUESCREEN, 24, 3, 8, 8, 8, 8, 2, 1, 0, -1, false, true, TransformBluescreen)},
            {VTFImageFormat.IMAGE_FORMAT_ARGB8888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_ARGB8888, 32, 4, 8, 8, 8, 8, 3, 0, 1, 2, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_BGRA8888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGRA8888, 32, 4, 8, 8, 8, 8, 2, 1, 0, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_DXT1, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_DXT1, 4, 0, 0, 0, 0, 0, -1, -1, -1, -1, true, true)},
            {VTFImageFormat.IMAGE_FORMAT_DXT3, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_DXT3, 8, 0, 0, 0, 0, 8, -1, -1, -1, -1, true, true)},
            {VTFImageFormat.IMAGE_FORMAT_DXT5, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_DXT5, 8, 0, 0, 0, 0, 8, -1, -1, -1, -1, true, true)},
            {VTFImageFormat.IMAGE_FORMAT_BGRX8888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGRX8888, 32, 4, 8, 8, 8, 0, 2, 1, 0, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_BGR565, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGR565, 16, 2, 5, 6, 5, 0, 2, 1, 0, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_BGRX5551, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGRX5551, 16, 2, 5, 5, 5, 0, 2, 1, 0, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_BGRA4444, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGRA4444, 16, 2, 4, 4, 4, 4, 2, 1, 0, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA, 4, 0, 0, 0, 0, 1, -1, -1, -1, -1, true, true)},
            {VTFImageFormat.IMAGE_FORMAT_BGRA5551, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_BGRA5551, 16, 2, 5, 5, 5, 1, 2, 1, 0, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_UV88, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_UV88, 16, 2, 8, 8, 0, 0, 0, 1, -1, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_UVWQ8888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_UVWQ8888, 32, 4, 8, 8, 8, 8, 0, 1, 2, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_RGBA16161616F, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGBA16161616F, 64, 8, 16, 16, 16, 16, 0, 1, 2, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_RGBA16161616, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGBA16161616, 64, 8, 16, 16, 16, 16, 0, 1, 2, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_UVLX8888, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_UVLX8888, 32, 4, 8, 8, 8, 8, 0, 1, 2, 3, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_R32F, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_R32F, 32, 4, 32, 0, 0, 0, 0, -1, -1, -1, false, false)},
            {VTFImageFormat.IMAGE_FORMAT_RGB323232F, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGB323232F, 96, 12, 32, 32, 32, 0, 0, 1, 2, -1, false, false)},
            {VTFImageFormat.IMAGE_FORMAT_RGBA32323232F, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_RGBA32323232F, 128, 16, 32, 32, 32, 32, 0, 1, 2, 3, false, false)},
            {VTFImageFormat.IMAGE_FORMAT_NV_DST16, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_NV_DST16, 16, 2, 16, 0, 0, 0, 0, -1, -1, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_NV_DST24, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_NV_DST24, 24, 3, 24, 0, 0, 0, 0, -1, -1, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_NV_INTZ, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_NV_INTZ, 32, 4, 0, 0, 0, 0, -1, -1, -1, -1, false, false)},
            {VTFImageFormat.IMAGE_FORMAT_NV_RAWZ, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_NV_RAWZ, 24, 3, 0, 0, 0, 0, -1, -1, -1, -1, false, false)},
            {VTFImageFormat.IMAGE_FORMAT_ATI_DST16, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_ATI_DST16, 16, 2, 16, 0, 0, 0, 0, -1, -1, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_ATI_DST24, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_ATI_DST24, 24, 3, 24, 0, 0, 0, 0, -1, -1, -1, false, true)},
            {VTFImageFormat.IMAGE_FORMAT_NV_NULL, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_NV_NULL, 32, 4, 0, 0, 0, 0, -1, -1, -1, -1, false, false)},
            {VTFImageFormat.IMAGE_FORMAT_ATI_1N, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_ATI_1N, 4, 0, 0, 0, 0, 0, -1, -1, -1, -1, true, false)},
            {VTFImageFormat.IMAGE_FORMAT_ATI_2N, new VTFImageFormatInfo(VTFImageFormat.IMAGE_FORMAT_ATI_2N, 8, 0, 0, 0, 0, 0, -1, -1, -1, -1, true, false)},
        };

        private static void TransformBluescreen(Byte[] bytes, Int32 index, Int32 count)
        {
            for (var i = index; i < index + count; i += 4)
            {
                if (bytes[i + 0] == Byte.MaxValue && bytes[i + 1] == 0 && bytes[i + 2] == 0)
                {
                    bytes[i + 3] = 0;
                }
            }
        }

        private static void TransformLuminance(Byte[] bytes, Int32 index, Int32 count)
        {
            for (var i = index; i < index + count; i += 4)
            {
                bytes[i + 0] = bytes[i + 2];
                bytes[i + 1] = bytes[i + 2];
            }
        }

        private static Byte PartialToByte(Byte partial, Int32 bits)
        {
            Byte b = 0;
            var dest = 8;
            while (dest >= bits)
            {
                b <<= bits;
                b |= partial;
                dest -= bits;
            }
            if (dest != 0)
            {
                partial >>= bits - dest;
                b <<= dest;
                b |= partial;
            }
            return b;
        }

        private class Mask
        {
            public char Component { get; }
            public Int32 Size { get; }
            public Int32 Index { get; }
            public Int32 Offset { get; set; }
            private uint Bitmask => ~0u >> (32 - Size);

            public Mask(char component, Int32 size, Int32 index)
            {
                Component = component;
                Size = size;
                Index = index;
            }

            public Byte Apply(uint value, Int32 bitsPerPixel)
            {
                if (Index < 0) return Component == 'a' ? Byte.MaxValue : Byte.MinValue;
                var im = value >> (bitsPerPixel - Offset - Size);
                im &= Bitmask;
                return PartialToByte((Byte) im, Size);
            }
        }
    }
}