using AnFake.Api;

namespace AnFake.Core
{
	public static class ArgsExtension
	{
		public static Args Option(this Args args, string name, FileSystemPath path)
		{
			return path == null 
				? args 
				: args.Option(name, path.Full);
		}
	}
}