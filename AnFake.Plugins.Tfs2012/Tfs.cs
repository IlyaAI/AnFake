using System;
using System.Linq;
using AnFake.Core;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

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

			public string SourceVersion
			{
				get { return _build.SourceGetVersion; }
			}

			public FileSystemPath DropLocation
			{
				get { return _build.DropLocation.AsPath(); }
			}
		}

		private static BuildDetail _build;

		public static BuildDetail Build
		{
			get { return _build ?? (_build = new BuildDetail(Plugin.Get<TfsPlugin>().Build)); }
		}

		public static int LastChangeset()
		{
			return LastChangesetOf("".AsPath());
		}

		public static int LastChangesetOf(FileSystemPath path)
		{
			var vcs = Plugin.Get<TfsPlugin>()
				.TeamProjectCollection
				.GetService<VersionControlServer>();

			var ws = vcs.GetWorkspace(path.Full);
			var queryParams = new QueryHistoryParameters(path.Full, RecursionType.Full)
			{					
				VersionStart = new ChangesetVersionSpec(1),
				VersionEnd = new WorkspaceVersionSpec(ws),
				MaxResults = 1
			};

			var changeset = vcs.QueryHistory(queryParams).FirstOrDefault();
			return changeset != null 
				? changeset.ChangesetId 
				: 0;			
		}		

		public static void UseIt()
		{
			Plugin.Register(new TfsPlugin(MyBuild.Defaults));
		}

		public static ServerPath AsServerPath(this string path)
		{
			if (path == null)
				throw new ArgumentException("Tfs.AsServerPath(path): path must not be null");

			return new ServerPath(path, false);
		}
	}
}