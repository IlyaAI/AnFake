using System.IO;
using ICSharpCode.SharpZipLib.Tar;

namespace AnFake.Core.Impl
{
	internal class TarArchiveReader : IArchiveReader
	{
		private class TarArchiveEntry : IArchiveEntry
		{
			private readonly TarInputStream _tarStream;
			private readonly TarEntry _entry;

			public TarArchiveEntry(TarInputStream tarStream, TarEntry entry)
			{
				_tarStream = tarStream;
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
				return _tarStream;
			}
		}

		private readonly TarInputStream _tarStream;

		public TarArchiveReader(FileItem archive)
		{
			_tarStream = new TarInputStream(new FileStream(archive.Path.Full, FileMode.Open, FileAccess.Read));
		}

		public TarArchiveReader(Stream stream, string name)
		{
			_tarStream = new TarInputStream(stream);
		}

		public void Dispose()
		{
			_tarStream.Dispose();
		}

		public IArchiveEntry NextEntry()
		{
			var entry = _tarStream.GetNextEntry();
			return entry != null
				? new TarArchiveEntry(_tarStream, entry)
				: null;
		}
	}
}
