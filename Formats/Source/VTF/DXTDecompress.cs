using System;

namespace uSource.Formats.Source.VTF
{
    public static class DXTDecompress
    {
        public static void DecompressDXT1(Byte[] Buffer, Byte[] Data, Int32 Width, Int32 Height)
        {
            Int32 Position = 0;
            Byte[] c = new Byte[16];
            for (Int32 y = 0; y < Height; y += 4)
            {
                for (Int32 x = 0; x < Width; x += 4)
                {
                    Int32 c0 = Data[Position++];
                    c0 |= Data[Position++] << 8;

                    Int32 c1 = Data[Position++];
                    c1 |= Data[Position++] << 8;

                    c[0] = (Byte)((c0 & 0xF800) >> 8);
                    c[1] = (Byte)((c0 & 0x07E0) >> 3);
                    c[2] = (Byte)((c0 & 0x001F) << 3);
                    c[3] = 255;

                    c[4] = (Byte)((c1 & 0xF800) >> 8);
                    c[5] = (Byte)((c1 & 0x07E0) >> 3);
                    c[6] = (Byte)((c1 & 0x001F) << 3);
                    c[7] = 255;

                    if (c0 > c1)
                    {
                        // No Alpha channel

                        c[8] = (Byte)((2 * c[0] + c[4]) / 3);
                        c[9] = (Byte)((2 * c[1] + c[5]) / 3);
                        c[10] = (Byte)((2 * c[2] + c[6]) / 3);
                        c[11] = 255;

                        c[12] = (Byte)((c[0] + 2 * c[4]) / 3);
                        c[13] = (Byte)((c[1] + 2 * c[5]) / 3);
                        c[14] = (Byte)((c[2] + 2 * c[6]) / 3);
                        c[15] = 255;
                    }
                    else
                    {
                        // 1-bit Alpha channel

                        c[8] = (Byte)((c[0] + c[4]) / 2);
                        c[9] = (Byte)((c[1] + c[5]) / 2);
                        c[10] = (Byte)((c[2] + c[6]) / 2);
                        c[11] = 255;
                        c[12] = 0;
                        c[13] = 0;
                        c[14] = 0;
                        c[15] = 0;
                    }

                    Int32 Bytes = Data[Position++];
                    Bytes |= Data[Position++] << 8;
                    Bytes |= Data[Position++] << 16;
                    Bytes |= Data[Position++] << 24;

                    for (Int32 yy = 0; yy < 4; yy++)
                    {
                        for (Int32 xx = 0; xx < 4; xx++)
                        {
                            Int32 xPosition = x + xx;
                            Int32 yPosition = y + yy;
                            if (xPosition < Width && yPosition < Height)
                            {
                                Int32 Index = Bytes & 0x0003;
                                Index *= 4;
                                Int32 Pointer = yPosition * Width * 4 + xPosition * 4;
                                Buffer[Pointer + 0] = c[Index + 2]; // b
                                Buffer[Pointer + 1] = c[Index + 1]; // g
                                Buffer[Pointer + 2] = c[Index + 0]; // r
                                Buffer[Pointer + 3] = c[Index + 3]; // a
                            }
                            Bytes >>= 2;
                        }
                    }
                }
            }
        }

        public static void DecompressDXT3(Byte[] Buffer, Byte[] Data, Int32 Width, Int32 Height)
        {
            Int32 Position = 0;
            Byte[] c = new Byte[16];
            Byte[] a = new Byte[8];
            for (Int32 y = 0; y < Height; y += 4)
            {
                for (Int32 x = 0; x < Width; x += 4)
                {
                    for (Int32 i = 0; i < 8; i++)
                        a[i] = Data[Position++];

                    Int32 c0 = Data[Position++];
                    c0 |= Data[Position++] << 8;

                    Int32 c1 = Data[Position++];
                    c1 |= Data[Position++] << 8;

                    c[0] = (Byte)((c0 & 0xF800) >> 8);
                    c[1] = (Byte)((c0 & 0x07E0) >> 3);
                    c[2] = (Byte)((c0 & 0x001F) << 3);
                    c[3] = 255;

                    c[4] = (Byte)((c1 & 0xF800) >> 8);
                    c[5] = (Byte)((c1 & 0x07E0) >> 3);
                    c[6] = (Byte)((c1 & 0x001F) << 3);
                    c[7] = 255;

                    c[8] = (Byte)((2 * c[0] + c[4]) / 3);
                    c[9] = (Byte)((2 * c[1] + c[5]) / 3);
                    c[10] = (Byte)((2 * c[2] + c[6]) / 3);
                    c[11] = 255;

                    c[12] = (Byte)((c[0] + 2 * c[4]) / 3);
                    c[13] = (Byte)((c[1] + 2 * c[5]) / 3);
                    c[14] = (Byte)((c[2] + 2 * c[6]) / 3);
                    c[15] = 255;

                    Int32 Bytes = Data[Position++];
                    Bytes |= Data[Position++] << 8;
                    Bytes |= Data[Position++] << 16;
                    Bytes |= Data[Position++] << 24;

                    for (Int32 yy = 0; yy < 4; yy++)
                    {
                        for (Int32 xx = 0; xx < 4; xx++)
                        {
                            Int32 xPosition = x + xx;
                            Int32 yPosition = y + yy;
                            Int32 aIndex = yy * 4 + xx;
                            if (xPosition < Width && yPosition < Height)
                            {
                                Int32 Index = Bytes & 0x0003;
                                Index *= 4;
                                Byte Alpha = (Byte)((a[aIndex >> 1] >> (aIndex << 2 & 0x07)) & 0x0f);
                                Alpha = (Byte)((Alpha << 4) | Alpha);
                                Int32 Pointer = yPosition * Width * 4 + xPosition * 4;
                                Buffer[Pointer + 0] = c[Index + 2]; // b
                                Buffer[Pointer + 1] = c[Index + 1]; // g
                                Buffer[Pointer + 2] = c[Index + 0]; // r
                                Buffer[Pointer + 3] = Alpha; // a
                            }
                            Bytes >>= 2;
                        }
                    }
                }
            }
        }

        public static void DecompressDXT5(Byte[] Buffer, Byte[] Data, Int32 Width, Int32 Height)
        {
            Int32 Position = 0;
            Byte[] c = new Byte[16];
            Int32[] a = new Int32[8];
            for (Int32 y = 0; y < Height; y += 4)
            {
                for (Int32 x = 0; x < Width; x += 4)
                {
                    Byte a0 = Data[Position++];
                    Byte a1 = Data[Position++];

                    a[0] = a0;
                    a[1] = a1;

                    if (a0 > a1)
                    {
                        a[2] = (6 * a[0] + 1 * a[1] + 3) / 7;
                        a[3] = (5 * a[0] + 2 * a[1] + 3) / 7;
                        a[4] = (4 * a[0] + 3 * a[1] + 3) / 7;
                        a[5] = (3 * a[0] + 4 * a[1] + 3) / 7;
                        a[6] = (2 * a[0] + 5 * a[1] + 3) / 7;
                        a[7] = (1 * a[0] + 6 * a[1] + 3) / 7;
                    }
                    else
                    {
                        a[2] = (4 * a[0] + 1 * a[1] + 2) / 5;
                        a[3] = (3 * a[0] + 2 * a[1] + 2) / 5;
                        a[4] = (2 * a[0] + 3 * a[1] + 2) / 5;
                        a[5] = (1 * a[0] + 4 * a[1] + 2) / 5;
                        a[6] = 0x00;
                        a[7] = 0xFF;
                    }

                    Int64 aIndex = 0L;
                    for (Int32 i = 0; i < 6; i++)
                        aIndex |= ((Int64)Data[Position++]) << (8 * i);

                    Int32 c0 = Data[Position++];
                    c0 |= Data[Position++] << 8;

                    Int32 c1 = Data[Position++];
                    c1 |= Data[Position++] << 8;

                    c[0] = (Byte)((c0 & 0xF800) >> 8);
                    c[1] = (Byte)((c0 & 0x07E0) >> 3);
                    c[2] = (Byte)((c0 & 0x001F) << 3);
                    c[3] = 255;

                    c[4] = (Byte)((c1 & 0xF800) >> 8);
                    c[5] = (Byte)((c1 & 0x07E0) >> 3);
                    c[6] = (Byte)((c1 & 0x001F) << 3);
                    c[7] = 255;

                    c[8] = (Byte)((2 * c[0] + c[4]) / 3);
                    c[9] = (Byte)((2 * c[1] + c[5]) / 3);
                    c[10] = (Byte)((2 * c[2] + c[6]) / 3);
                    c[11] = 255;

                    c[12] = (Byte)((c[0] + 2 * c[4]) / 3);
                    c[13] = (Byte)((c[1] + 2 * c[5]) / 3);
                    c[14] = (Byte)((c[2] + 2 * c[6]) / 3);
                    c[15] = 255;

                    Int32 Bytes = Data[Position++];
                    Bytes |= Data[Position++] << 8;
                    Bytes |= Data[Position++] << 16;
                    Bytes |= Data[Position++] << 24;

                    for (Int32 yy = 0; yy < 4; yy++)
                    {
                        for (Int32 xx = 0; xx < 4; xx++)
                        {
                            Int32 xPosition = x + xx;
                            Int32 yPosition = y + yy;
                            if (xPosition < Width && yPosition < Height)
                            {
                                Int32 Index = Bytes & 0x0003;
                                Index *= 4;
                                Byte Alpha = (Byte)a[aIndex & 0x07];
                                Int32 Pointer = yPosition * Width * 4 + xPosition * 4;
                                Buffer[Pointer + 0] = c[Index + 2]; // b
                                Buffer[Pointer + 1] = c[Index + 1]; // g
                                Buffer[Pointer + 2] = c[Index + 0]; // r
                                Buffer[Pointer + 3] = Alpha; // a
                            }
                            Bytes >>= 2;
                            aIndex >>= 3;
                        }
                    }
                }
            }
        }
    }
}