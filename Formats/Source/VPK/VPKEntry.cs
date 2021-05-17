using System.IO;

namespace uSource.Formats.Source.VPK
{
	public class VPKEntry
	{
		public bool HasPreloadData { get; set; }
		public uint Length => EntryLength;

		internal uint CRC;
		internal ushort PreloadBytes;
		internal uint PreloadDataOffset;
		internal ushort ArchiveIndex;
		internal uint EntryOffset;
		internal uint EntryLength;
		internal VPKFile ParentArchive;

		internal VPKEntry(VPKFile parentArchive, uint crc, ushort preloadBytes, uint preloadDataOffset, ushort archiveIndex, uint entryOffset, uint entryLength)
		{
			ParentArchive = parentArchive;
			CRC = crc;
			PreloadBytes = preloadBytes;
			PreloadDataOffset = preloadDataOffset;
			ArchiveIndex = archiveIndex;
			EntryOffset = entryOffset;
			EntryLength = entryLength;
			HasPreloadData = preloadBytes > 0;
		}

		public Stream ReadPreloadDataStream()
		{
			MemoryStream memStream = new MemoryStream();
			CopyPreloadDataStreamTo(memStream);
			memStream.Seek(0, SeekOrigin.Begin);
			return memStream;
		}

		public bool CopyPreloadDataStreamTo(Stream outputStream)
		{
			if (HasPreloadData)
			{
				var fs = ParentArchive.MainPart.PartStream;
				fs.Seek(PreloadDataOffset, SeekOrigin.Begin);
				fs.CopyToLimited(outputStream, PreloadBytes);
				return true;
			}
			return false;
		}

		public Stream ReadDataStream()
		{
			MemoryStream memStream = new MemoryStream();
			CopyDataStreamTo(memStream);
			memStream.Seek(0, SeekOrigin.Begin);
			return memStream;
		}

		public bool CopyDataStreamTo(Stream outputStream)
		{
			var partFile = ParentArchive.Parts[ArchiveIndex];
			if (partFile != null && !HasPreloadData)
			{
				var fs = partFile.PartStream;
				fs.Seek(EntryOffset, SeekOrigin.Begin);
				fs.CopyToLimited(outputStream, (int)EntryLength);
				return true;
			}

			return false;
		}

		public Stream ReadAnyDataStream()
		{
			if (HasPreloadData)
			{
				return ReadPreloadDataStream();
			}
			else
			{
				return ReadDataStream();
			}
		}

	}
}
