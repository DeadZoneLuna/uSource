using System;
using System.IO;
using System.Collections.Generic;

namespace uSource.Formats.Source.VPK
{
	internal class ArchiveParsingException : Exception
	{
		public ArchiveParsingException()
		{
		}

		public ArchiveParsingException(String message)
			: base(message)
		{
		}

		public ArchiveParsingException(String message, Exception innerException)
			: base(message, innerException)
		{
		}
	}

	public sealed class VPKFile : IDisposable
	{
		public Boolean Loaded { get; private set; }
		public Boolean IsMultiPart
		{
			get
			{
				return Parts.Count > 1;
			}
		}

		private VPKReaderBase Reader { get; set; }
		private Boolean Disposed { get; set; } // To detect redundant calls

		public Dictionary<String, VPKEntry> Entries = new Dictionary<String, VPKEntry>();
		internal Dictionary<Int32, VPKFilePart> Parts { get; } = new Dictionary<Int32, VPKFilePart>();
		internal VPKFilePart MainPart
		{
			get
			{
				return Parts[MainPartIndex];
			}
		}

		internal const Int32 MainPartIndex = -1;

		/// <summary>
		/// Loads the specified vpk archive by filename, if it's a _dir.vpk file it'll load related numbered vpks automatically
		/// </summary>
		/// <param name="FileName">A vpk archive ending in _dir.vpk</param>
		public VPKFile(String FileName)
		{
			Load(new FileStream(FileName, FileMode.Open, FileAccess.Read), FileName);
		}

		/// <summary>
		/// Loads the specified vpk archive by filename, if it's a _dir.vpk file it'll load related numbered vpks automatically
		/// </summary>
		/// <param name="FileName">A vpk archive ending in _dir.vpk</param>
		public void Load(String FileName)
		{
			Load(new FileStream(FileName, FileMode.Open, FileAccess.Read), FileName);
		}

		/// <summary>
		/// The main Load function, the related parts need to be numbered correctly as "archivename_01.vpk" and so forth
		/// </summary>
		/// <param name="Stream"></param>
		/// <param name="FileName"></param>
		public void Load(Stream Stream, String FileName = "")
		{
			if (Loaded)
				throw new NotSupportedException("Tried to call Load on a VpkArchive that is already loaded, dispose and create a new one instead");

			if (String.IsNullOrEmpty(FileName))
				throw new FileLoadException("File name is empty!!!");

			Reader = new VPKReaderBase(Stream);

			UInt32 Signature = Reader.ReadUInt32();
			UInt32 Version = Reader.ReadUInt32();

			if (Signature != 0x55aa1234 && (Version > 2 || Version < 1))
			{
				Dispose();
				throw new ArchiveParsingException("Invalid archive header");
			}

			// skip unneeded bytes
			if (Version == 1 || Version == 2)
			{
				Reader.ReadUInt32(); // - TreeSize;
				if (Version == 2)
					Reader.ReadBytes(16);
			}

			AddMainPart(FileName, Stream);

			//TODO:
			//OPTIMIZE PARSING
			String Folder = Path.GetDirectoryName(FileName) ?? "";
			String NameWithoutExtension = Path.GetFileNameWithoutExtension(FileName) ?? "";
			//String Extension = Path.GetExtension(FileName);

			String BaseName = NameWithoutExtension.Substring(0, NameWithoutExtension.Length - 4);

			String[] MatchingFiles = Directory.GetFiles(Folder, BaseName + "_???.vpk");
			foreach (String MatchedFile in MatchingFiles)
			{
				var fileName = Path.GetFileNameWithoutExtension(MatchedFile);
				UInt16 Index;
				if (UInt16.TryParse(fileName.Substring(fileName.Length - 3), out Index))
				{
					AddPart(MatchedFile, new FileStream(MatchedFile, FileMode.Open, FileAccess.Read), Index);
				}
			}

			Reader.ReadDirectories(this);

			Loaded = true;
		}

		private void AddMainPart(String filename, Stream stream = null)
		{
			if (stream == null)
			{
				stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			}
			AddPart(filename, stream, MainPartIndex);
		}

		private void AddPart(String filename, Stream stream, Int32 index)
		{
			Parts.Add(index, new VPKFilePart(index, filename, stream));
		}

		#region IDisposable Support

		private void Dispose(Boolean disposing)
		{
			if (!Disposed)
			{
				if (disposing)
				{
					foreach (var partkv in Parts)
					{
						partkv.Value.PartStream?.Dispose();
					}
					Parts.Clear();
					Entries.Clear();
				}
				Reader.Dispose();
				Reader.Close();
				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				Disposed = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(Boolean disposing) above.
			Dispose(true);
			GC.Collect();
		}
		#endregion
	}
}