using System;

namespace AnFake.Core
{
	public static class VersionExtension
	{
		public static Version AsVersion(this string version)
		{
			return new Version(version);
		}
	}
}