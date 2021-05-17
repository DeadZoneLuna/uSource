namespace uSource.Formats.Source.VTF
{
    public enum VTFResourceType : uint
    {
        LowResImage = 0x01,
        Image = 0x30,
        Sheet = 0x10,
        CRC = 'C' | ('R' << 8) | ('C' << 16) | (0x02 << 24),
        TextureLodSettings = 'L' | ('O' << 8) | ('D' << 16) | (0x02 << 24),
        TextureSettingsEx = 'T' | ('S' << 8) | ('O' << 16) | (0x02 << 24),
        KeyValueData = 'K' | ('V' << 8) | ('D' << 16),
    }

    public class VTFResource
    {
        public VTFResourceType Type { get; set; }
        public uint Data { get; set; }
        //public byte[] Data { get; set; }
    }
}