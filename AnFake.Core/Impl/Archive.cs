using System.IO;

namespace AnFake.Core.Impl
{
	internal static class Archive
	{
		public static IArchiveReader Open(FileItem archive)
		{
			switch (archive.Ext.ToLowerInvariant())
			{
				case ".tar":
					return new TarArchiveReader(archive);
				case ".gz":
					return new GZipArchiveReader(archive);					
				default:
					return new ZipArchiveReader(archive);					
			}
		}

		public static IArchiveReader Open(Stream stream, string name)
		{
			var ext = Path.GetExtension(name) ?? "";
			switch (ext.ToLowerInvariant())
			{
				case ".tar":
					return new TarArchiveReader(stream, name);
				case ".gz":
					return new GZipArchiveReader(stream, name);
				default:
					return new ZipArchiveReader(stream, name);
			}
		}		
	}
}
