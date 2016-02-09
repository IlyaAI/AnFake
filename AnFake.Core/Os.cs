using System;
using System.Diagnostics.CodeAnalysis;

namespace AnFake.Core
{
	/// <summary>
	///		Provides facility to define file name with platform specific extensions (such as .exe/., .dll/.so, .bat/.sh).
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public static class Os
	{
		public enum TypeName
		{
			Windows,
			Linux
		}

		public static TypeName Type
		{
			get { return TypeName.Windows; }
		}

		public static string exe(string pattern)
		{
			switch (Type)
			{
				case TypeName.Windows:
					return pattern + ".exe";
				case TypeName.Linux:
					return pattern;
				default:
					throw new NotSupportedException();
			}
		}

		public static string dll(string pattern)
		{
			switch (Type)
			{
				case TypeName.Windows:
					return pattern + ".dll";
				case TypeName.Linux:
					return "lib" + pattern + ".so";
				default:
					throw new NotSupportedException();
			}
		}

		public static string bat(string pattern)
		{
			switch (Type)
			{
				case TypeName.Windows:
					return pattern + ".bat";
				case TypeName.Linux:
					return pattern + ".sh";
				default:
					throw new NotSupportedException();
			}
		}

		public static string ifAny(string pattern)
		{
			return pattern;
		}

		public static string ifWindows(string pattern)
		{
			return Type == TypeName.Windows ? pattern : FileSet.SkipPattern;
		}

		public static string ifLinux(string pattern)
		{
			return Type == TypeName.Linux ? pattern : FileSet.SkipPattern;
		}
	}
}
