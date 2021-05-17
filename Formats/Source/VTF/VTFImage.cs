using System;

namespace uSource.Formats.Source.VTF
{
    /// <summary>
    /// A VTF image containing binary pixel data in some format.
    /// </summary>
    public class VTFImage
    {
        /// <summary>
        /// The format of this image.
        /// </summary>
        public VTFImageFormat Format { get; set; }

        /// <summary>
        /// The width of the image, in pixels
        /// </summary>
        public Int32 Width { get; set; }

        /// <summary>
        /// The height of the image, in pixels
        /// </summary>
        public Int32 Height { get; set; }

        /// <summary>
        /// The mipmap number of this image. Lower numbers = larger size.
        /// </summary>
        public Int32 Mipmap { get; set; }

        /// <summary>
        /// The frame number of this image.
        /// </summary>
        public Int32 Frame { get; set; }

        /// <summary>
        /// The face number of this image.
        /// </summary>
        public Int32 Face { get; set; }

        /// <summary>
        /// The slice (depth) number of this image.
        /// </summary>
        public Int32 Slice { get; set; }

        /// <summary>
        /// The image data, in native image format
        /// </summary>
        public Byte[] Data { get; set; }

        /// <summary>
        /// Convert the native format data to a standard 32-bit bgra8888 format.
        /// </summary>
        /// <returns>The data in bgra8888 format.</returns>
        public Byte[] GetBgra32Data()
        {
            return VTFImageFormatInfo.FromFormat(Format).ConvertToBgra32(Data, Width, Height);
        }
    }
}