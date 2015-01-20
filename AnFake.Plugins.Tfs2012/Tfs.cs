using System;
using AnFake.Core;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Plugins.Tfs2012
{
	/// <summary>
	///		Represents tools related to Team Foundation.
	/// </summary>
	public static class Tfs
	{
		private static bool _registered;

		/// <summary>
		///		Activates <c>Tfs</c> plugin.
		/// </summary>
		public static void PlugIn()
		{
			if (_registered)
			{
				Plugin.Get<TfsPlugin>(); // force instantiation
			}
			else
			{
				Plugin.Register<TfsPlugin>()
					.As<Core.Integration.IVersionControl>()
					.As<Core.Integration.IBuildServer>()
					.AsSelf();

				_registered = true;
			}			
		}

		/// <summary>
		///		Marks <c>Tfs</c> plugin to be activated later.
		/// </summary>
		/// <remarks>
		///		This method is intended for special cases only (see Extras/tf.fsx for example). Normally you should call <c>Tfs.PlugIn</c>.
		/// </remarks>
		public static void PlugInDeferred()
		{
			if (_registered)
				return;

			Plugin.RegisterDeferred<TfsPlugin>()
				.As<Core.Integration.IVersionControl>()
				.As<Core.Integration.IBuildServer>()
				.AsSelf();

			_registered = true;
		}

		/// <summary>
		///		Represents build details provided by Tfs.
		/// </summary>
		/// <remarks>
		///		Really, this is a subset of <c>Microsoft.TeamFoundation.Build.Client.IBuildDetail</c> interface.
		/// </remarks>
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

		/// <summary>
		///		Current build details.
		/// </summary>
		public static BuildDetail Build
		{
			get { return _build ?? (_build = new BuildDetail(Plugin.Get<TfsPlugin>().Build)); }
		}

		/// <summary>
		///		The last changeset number for build path.
		/// </summary>
		/// <returns></returns>
		public static int LastChangeset()
		{
			return LastChangesetOf("".AsPath());
		}

		/// <summary>
		///		The last changeset number for specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static int LastChangesetOf(FileSystemPath path)
		{
			return Plugin.Get<TfsPlugin>().LastChangesetOf(path);
		}		

		/// <summary>
		///		Creates <c>ServerPath</c> instance from string representation.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static ServerPath AsServerPath(this string path)
		{
			if (path == null)
				throw new ArgumentException("Tfs.AsServerPath(path): path must not be null");

			return new ServerPath(path, false);
		}
	}
}