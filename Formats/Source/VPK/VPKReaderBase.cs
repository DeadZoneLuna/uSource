using System;
using System.IO;

namespace uSource.Formats.Source.VPK
{
	internal class VPKReaderBase : uReader
	{
		public VPKReaderBase(Stream InputStream) 
			: base(InputStream)
		{
			this.InputStream = InputStream;

			if (!InputStream.CanRead)
				throw new FileLoadException("Can't read unreadable archive!");
		}

		public void ReadDirectories(VPKFile RootArchive)
		{
			while (true)
			{
				String Extension = ReadNullTerminatedString();
				if (String.IsNullOrEmpty(Extension))
					break;

				while (true)
				{
					String Path = ReadNullTerminatedString();
					if (String.IsNullOrEmpty(Path))
						break;

					ReadEntries(RootArchive, Extension, Path);
				}
			}
		}

		public void ReadEntries(VPKFile RootArchive, String Extension, String Path)
		{
			while (true)
			{
				String FileName = ReadNullTerminatedString();
				if (String.IsNullOrEmpty(FileName))
					break;

				UInt32 CRC = ReadUInt32();
				UInt16 PreloadBytes = ReadUInt16();
				UInt16 ArchiveIndex = ReadUInt16();
				UInt32 EntryOffset = ReadUInt32();
				UInt32 EntryLength = ReadUInt32();
				// skip terminator
				ReadUInt16();
				UInt32 preloadDataOffset = (UInt32)BaseStream.Position;
				if (PreloadBytes > 0)
				{
					BaseStream.Position += PreloadBytes;
				}

				ArchiveIndex = ArchiveIndex == 32767 ? (UInt16)0 : ArchiveIndex;

				Path = Path.ToLower();
				FileName = FileName.ToLower();
				Extension = Extension.ToLower();

				RootArchive.Entries.Add(String.Format("{0}/{1}.{2}", Path, FileName, Extension), new VPKEntry(RootArchive, CRC, PreloadBytes, preloadDataOffset, ArchiveIndex, EntryOffset, EntryLength));
			}
		}
	}
}

