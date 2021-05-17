using System.IO;

namespace uSource.Formats.Source.VPK
{
	internal class VPKFilePart
	{
		public uint Size { get; set; }
		public int Index { get; set; }
		public string Filename { get; set; }
		public Stream PartStream { get; set; }

		public VPKFilePart(uint size, int index, string filename, Stream filestream)
		{
			Size = size;
			Index = index;
			Filename = filename;
			PartStream = filestream;
		}

		public VPKFilePart(int index, string filename, Stream filestream)
		{
			Index = index;
			Filename = filename;
			PartStream = filestream;
		}
	}
}
