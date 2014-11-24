namespace AnFake.Integration.Tfs2012
{
	public static class ServerPathUtils
	{
		public const char SeparatorChar = '/';
		public const char AltSeparatorChar = '\\';
		public const string RepoRoot = "$";

		public static bool IsRooted(string path)
		{
			return path.StartsWith(RepoRoot);
		}

		public static string Normalize(string path)
		{
			return path.Replace(AltSeparatorChar, SeparatorChar);
		}

		public static string Combine(string basePath, string subPath)
		{
			if (subPath.StartsWith(RepoRoot))
				return subPath;

			var index = 0;
			while (index < subPath.Length && subPath[index] == SeparatorChar) index++;

			return basePath + SeparatorChar + subPath.Substring(index);
		}
	}
}