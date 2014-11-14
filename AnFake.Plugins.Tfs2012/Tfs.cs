using System;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Plugins.Tfs2012
{
	public static class Tfs
	{
		public static Uri Uri { get; internal set; }

		public static IBuildDetail BuildDetail { get; internal set; }

		public static void UseIt()
		{
			// do nothing
		}
	}
}