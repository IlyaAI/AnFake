using System;
using AnFake.Core;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Plugins.Tfs2012
{
	public static class Tfs
	{
		public sealed class BuildDetail
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private readonly IBuildDetail _build;

			internal BuildDetail(IBuildDetail build)
			{
				_build = build;
			}

			public Uri Uri
			{
				get { return _build.Uri; }
			}
		}

		private static BuildDetail _build;

		public static BuildDetail Build
		{
			get { return _build ?? (_build = new BuildDetail(Plugin.Get<TfsPlugin>().Build)); }
		}

		public static void UseIt()
		{
			Plugin.Register(new TfsPlugin(MyBuild.Defaults));
		}
	}
}