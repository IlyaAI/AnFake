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

		private static TypeName _type = TypeName.Windows;

		public static TypeName Type
		{
			get { return _type; }
			internal set { _type = value; }
		}

		/// <summary>
		///		Returns &lt;pattern> + '.exe' on Windows and &lt;pattern> on Linux.
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public static string exe(string pattern)
		{
			if (String.IsNullOrWhiteSpace(pattern))
				throw new ArgumentException("Os.exe(pattern): pattern must not be null or empty");

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

		/// <summary>
		///		Returns &lt;pattern> + '.dll' on Windows and 'lib' + &lt;pattern> + '.so' on Linux.
		/// </summary>
		/// <remarks>
		///		If pattern contains slash or back slash then 'lib' prefix will be added to file name (just after last slash).
		/// </remarks>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public static string dll(string pattern)
		{
			if (String.IsNullOrWhiteSpace(pattern))
				throw new ArgumentException("Os.dll(pattern): pattern must not be null or empty");

			switch (Type)
			{
				case TypeName.Windows:
					return pattern + ".dll";
				case TypeName.Linux:
				{
					var lastSlash = pattern.LastIndexOfAny(new[] {'/', '\\'});
					return lastSlash >= 0
						? pattern.Substring(0, lastSlash + 1) + "lib" + pattern.Substring(lastSlash + 1) + ".so"
						: "lib" + pattern + ".so";
				}					
				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		///		Returns &lt;pattern> + '.bat' on Windows and &lt;pattern> + '.sh' on Linux.
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public static string bat(string pattern)
		{
			if (String.IsNullOrWhiteSpace(pattern))
				throw new ArgumentException("Os.bat(pattern): pattern must not be null or empty");

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

		/// <summary>
		///		Returns &lt;pattern> on both Windows and Linux.
		/// </summary>
		/// <remarks>
		///		Actually does nothing, intended for consistency only.
		/// </remarks>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public static string ifAny(string pattern)
		{
			if (String.IsNullOrWhiteSpace(pattern))
				throw new ArgumentException("Os.ifAny(pattern): pattern must not be null or empty");

			return pattern;
		}

		/// <summary>
		///		Returns &lt;pattern> on Windows and nothing on Linux.
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public static string ifWindows(string pattern)
		{
			if (String.IsNullOrWhiteSpace(pattern))
				throw new ArgumentException("Os.ifWindows(pattern): pattern must not be null or empty");

			return Type == TypeName.Windows ? pattern : FileSet.SkipPattern;
		}

		/// <summary>
		///		Returns &lt;pattern> on Linux and nothing on Windows.
		/// </summary>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public static string ifLinux(string pattern)
		{
			if (String.IsNullOrWhiteSpace(pattern))
				throw new ArgumentException("Os.ifLinux(pattern): pattern must not be null or empty");

			return Type == TypeName.Linux ? pattern : FileSet.SkipPattern;
		}
	}
}
