using System.IO;
using ICSharpCode.SharpZipLib.GZip;

namespace AnFake.Core.Impl
{
	internal class GZipArchiveReader : IArchiveReader
	{
		private class GZipArchiveEntry : IArchiveEntry
		{
			private readonly GZipInputStream _gzipStream;
			private readonly string _name;
			
			public GZipArchiveEntry(GZipInputStream gzipStream, string name)
			{
				_gzipStream = gzipStream;
				_name = name;
			}

			public string Name
			{
				get { return _name; }
			}

			public bool IsDirectory
			{
				get { return false; }
			}

			public Stream AsStream()
			{
				return _gzipStream;
			}
		}

		private readonly GZipInputStream _gzipStream;
		private GZipArchiveEntry _entry;

		public GZipArchiveReader(FileItem archive)
		{
			_gzipStream = new GZipInputStream(new FileStream(archive.Path.Full, FileMode.Open, FileAccess.Read));
			_entry = new GZipArchiveEntry(_gzipStream, archive.NameWithoutExt);
		}

		public GZipArchiveReader(Stream stream, string name)
		{
			_gzipStream = new GZipInputStream(stream);
			_entry = new GZipArchiveEntry(_gzipStream, Path.GetFileNameWithoutExtension(name));
		}

		public void Dispose()
		{
			_gzipStream.Dispose();
		}

		public IArchiveEntry NextEntry()
		{
			var entry = _entry;
			_entry = null;
			return entry;
		}
	}
}
