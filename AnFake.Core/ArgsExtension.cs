using System;
using AnFake.Api;

namespace AnFake.Core
{
	public static class ArgsExtension
	{
		public static Args Param(this Args args, FileSystemPath path)
		{
			if (path == null)
				throw new ArgumentException("Args.Param(path): path must not be null");

			return args.Param(path.Full);
		}

		public static Args Option(this Args args, string name, FileSystemPath path)
		{
			return path == null 
				? args 
				: args.Option(name, path.Full);
		}
	}
}