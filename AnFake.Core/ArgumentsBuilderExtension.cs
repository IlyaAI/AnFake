namespace AnFake.Core
{
	public static class ArgumentsBuilderExtension
	{
		public static ArgumentsBuilder Option(this ArgumentsBuilder args, string name, FileSystemPath path)
		{
			return args.Option(name, path != null ? path.Full : null);
		}
	}
}