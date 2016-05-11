using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace AnFake.Core.Impl
{
	internal class ZipArchiveReader : IArchiveReader
	{
		private class ZipArchiveEntry : IArchiveEntry
		{
			private readonly ZipInputStream _zipStream;
			private readonly ZipEntry _entry;

			public ZipArchiveEntry(ZipInputStream zipStream, ZipEntry entry)
			{
				_zipStream = zipStream;
				_entry = entry;
			}

			public string Name
			{
				get { return _entry.Name; }
			}

			public bool IsDirectory
			{
				get { return _entry.IsDirectory; }
			}

			public Stream AsStream()
			{
				return _zipStream;
			}
		}

		private readonly ZipInputStream _zipStream;

		public ZipArchiveReader(FileItem archive)
		{
			_zipStream = new ZipInputStream(new FileStream(archive.Path.Full, FileMode.Open, FileAccess.Read));
		}

		public ZipArchiveReader(Stream stream, string name)
		{
			_zipStream = new ZipInputStream(stream);
		}

		public void Dispose()
		{
			_zipStream.Dispose();
		}

		public IArchiveEntry NextEntry()
		{
			var entry = _zipStream.GetNextEntry();
			return entry != null 
				? new ZipArchiveEntry(_zipStream, entry)
				: null;
		}
	}
}
