using System.IO;

namespace AnFake.Core.Impl
{
	internal interface IArchiveEntry
	{
		string Name { get; }

		bool IsDirectory { get; }

		Stream AsStream();
	}
}