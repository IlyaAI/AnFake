namespace AnFake.Core.Integration
{
	public interface IBuildServer
	{
		FileSystemPath DropLocation { get; }

		FileSystemPath LogsLocation { get; }
	}
}