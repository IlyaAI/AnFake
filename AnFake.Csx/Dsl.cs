using System.IO;

namespace AnFake.Csx
{
	// ReSharper disable InconsistentNaming
	public static class Dsl
	{
		public static string slash(this string path1, string path2)
		{
			return Path.Combine(path1, path2);
		}
	}
	// ReSharper restore InconsistentNaming
}