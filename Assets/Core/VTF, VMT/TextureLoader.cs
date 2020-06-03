using UnityEngine;
using System.IO;
using System;

namespace Engine.Source
{
    public class TextureLoader : VtfSpec
    {
        public static Texture2D[] Frames;

        public static Texture2D Load(string MainTexture, string AltTexture = null)
        {
            //return null;
            tagVTFHEADER VTF_Header = new tagVTFHEADER();
            bool m_Mipmaps = true;
            string Path = string.Empty;

            if (File.Exists(System.IO.Path.Combine(ConfigLoader._PakPath, ConfigLoader.LevelName + "_pakFile/materials/" + MainTexture + ".vtf")))
                Path = System.IO.Path.Combine(ConfigLoader._PakPath, ConfigLoader.LevelName + "_pakFile/materials/" + MainTexture + ".vtf");
            else
            {
                for (int i = 0; i < ConfigLoader.ModFolders.Length; i++)
                {
                    if (File.Exists(ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + "/materials/" + MainTexture + ".vtf"))
                        Path = ConfigLoader.GamePath + "/" + ConfigLoader.ModFolders[i] + "/materials/" + MainTexture + ".vtf";
                }
            }

            if (string.IsNullOrEmpty(Path))
            {
                if (AltTexture != null)
                    return Load(AltTexture);

                Debug.Log(String.Format("{0}: File not found", MainTexture + ".vtf"));
                return Load("debug/debugempty", null);
                //return null;
            }

            MemUtils VTFFileReader = new MemUtils(File.OpenRead(Path));
            VTFFileReader.ReadType(ref VTF_Header);

            if (VTF_Header.Signature != 0x00465456)
            {
                Debug.Log(String.Format("{0}: File signature does not match 'VTF'", MainTexture + ".vtf"));
                return null;
            }

            int[] UiBytesPerPixels =
                {
                4, 4, 3, 3, 2, 1,
                2, 1, 1, 3, 3, 4,
                4, 1, 1, 1, 4, 2,
                2, 2, 1, 2, 2, 4,
                8, 8, 4
            };

            int ImageSize = VTF_Header.Width * VTF_Header.Height * UiBytesPerPixels[(int)VTF_Header.HighResImageFormat];
            TextureFormat InternalFormat;

            switch (VTF_Header.HighResImageFormat)
            {
                case VTFImageFormat.IMAGE_FORMAT_DXT1:
                case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                    ImageSize = ((VTF_Header.Width + 3) / 4) * ((VTF_Header.Height + 3) / 4) * 8;
                    InternalFormat = TextureFormat.DXT1;
                    break;

                case VTFImageFormat.IMAGE_FORMAT_DXT3:
                case VTFImageFormat.IMAGE_FORMAT_DXT5:
                    ImageSize = ((VTF_Header.Width + 3) / 4) * ((VTF_Header.Height + 3) / 4) * 16;
                    InternalFormat = TextureFormat.DXT5;
                    break;

                case VTFImageFormat.IMAGE_FORMAT_RGB888:
                case VTFImageFormat.IMAGE_FORMAT_RGB888_BLUESCREEN:
                case VTFImageFormat.IMAGE_FORMAT_BGR888:
                case VTFImageFormat.IMAGE_FORMAT_BGR888_BLUESCREEN:
                    InternalFormat = TextureFormat.RGB24;
                    break;

                case VTFImageFormat.IMAGE_FORMAT_RGBA8888:
                    InternalFormat = TextureFormat.RGBA32;
                    break;

                case VTFImageFormat.IMAGE_FORMAT_ARGB8888:
                    InternalFormat = TextureFormat.ARGB32;
                    break;

                case VTFImageFormat.IMAGE_FORMAT_BGRA8888:
                case VTFImageFormat.IMAGE_FORMAT_BGRX8888:
                    InternalFormat = TextureFormat.BGRA32;
                    break;

                /*case VTFImageFormat.IMAGE_FORMAT_UV88:
					InternalFormat = TextureFormat.RG16;
					break;*/


                default:
                    Debug.Log(String.Format("{0}: Unsupported format: {1}", MainTexture + ".vtf", VTF_Header.HighResImageFormat));
                    VTFFileReader.Close();
                    return null;
            }

            Frames = new Texture2D[VTF_Header.Frames];

            for (Int32 i = 0; i < VTF_Header.Frames; i++)
            {
                Texture2D VTF_Texture = new Texture2D(VTF_Header.Width, VTF_Header.Height, InternalFormat, false);

                VTFFileReader.BaseStream.Seek(VTFFileReader.BaseStream.Length - ImageSize * (VTF_Header.Frames - i), SeekOrigin.Begin);
                Byte[] VTFFile = VTFFileReader.ReadBytes(ImageSize);
                if (VTF_Header.HighResImageFormat == VTFImageFormat.IMAGE_FORMAT_BGR888 || VTF_Header.HighResImageFormat == VTFImageFormat.IMAGE_FORMAT_BGR888_BLUESCREEN)
                {
                    for (Int32 j = 0; j < VTFFile.Length - 1; j += 3)
                    {
                        Byte PixelX = VTFFile[j];
                        VTFFile[j] = VTFFile[j + 2];
                        VTFFile[j + 2] = PixelX;
                    }
                }

                VTF_Texture.LoadRawTextureData(VTFFile);

                switch (VTF_Header.HighResImageFormat)
                {
                    case VTFImageFormat.IMAGE_FORMAT_DXT1:
                    case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                        Frames[i] = new Texture2D(VTF_Header.Width, VTF_Header.Height, TextureFormat.RGB24, true);
                        break;

                    case VTFImageFormat.IMAGE_FORMAT_DXT3:
                    case VTFImageFormat.IMAGE_FORMAT_DXT5:
                        Frames[i] = new Texture2D(VTF_Header.Width, VTF_Header.Height, TextureFormat.RGBA32, true);
                        break;

                    default:
                        Frames[i] = new Texture2D(VTF_Header.Width, VTF_Header.Height, InternalFormat, true);
                        break;
                }

                if ((VTF_Header.Flags & (VTFImageFlags.TEXTUREFLAGS_CLAMPS | VTFImageFlags.TEXTUREFLAGS_CLAMPT)) != 0)
                    Frames[i].wrapMode = TextureWrapMode.Clamp;

                if (m_Mipmaps)
                {
                    Color32[] pixels = Frames[i].GetPixels32();

                    switch (VTF_Header.HighResImageFormat)
                    {
                        case VTFImageFormat.IMAGE_FORMAT_DXT1:
                        case VTFImageFormat.IMAGE_FORMAT_DXT1_ONEBITALPHA:
                            Frames[i] = new Texture2D(VTF_Header.Width, VTF_Header.Height, TextureFormat.RGB24, true);
                            break;

                        case VTFImageFormat.IMAGE_FORMAT_DXT3:
                        case VTFImageFormat.IMAGE_FORMAT_DXT5:
                            Frames[i] = new Texture2D(VTF_Header.Width, VTF_Header.Height, TextureFormat.RGBA32, true);
                            break;

                        default:
                            Frames[i] = new Texture2D(VTF_Header.Width, VTF_Header.Height, InternalFormat, true);
                            break;
                    }

                    Frames[i].SetPixels32(pixels);
                }

                Frames[i].SetPixels32(VTF_Texture.GetPixels32());
                Frames[i].Apply();

                if (m_Mipmaps)
                    Frames[i].Compress(true);

                Frames[i].Compress(true);
                UnityEngine.Object.DestroyImmediate(VTF_Texture);
            }
            VTFFileReader.BaseStream.Dispose();
            Frames[0].name = Path;

            return Frames[0];
        }
    }
}