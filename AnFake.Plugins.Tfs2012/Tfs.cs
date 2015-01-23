using System;
using AnFake.Core;
using Microsoft.TeamFoundation.Client;

namespace AnFake.Plugins.Tfs2012
{
	/// <summary>
	///     Represents tools related to Team Foundation.
	/// </summary>
	public static class Tfs
	{
		private static bool _registered;

		/// <summary>
		///     Activates <c>Tfs</c> plugin.
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
		///     Marks <c>Tfs</c> plugin to be activated later.
		/// </summary>
		/// <remarks>
		///     This method is intended for special cases only (see Extras/tf.fsx for example). Normally you should call
		///     <c>Tfs.PlugIn</c>.
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

		private static TfsPlugin _impl;

		private static TfsPlugin Impl
		{
			get { return _impl ?? (_impl = Plugin.Get<TfsPlugin>()); }
		}

		/// <summary>
		///     Sources root server path.
		/// </summary>
		/// <returns></returns>
		public static ServerPath SourcesRoot
		{
			get { return Impl.SourcesRoot; }
		}

		/// <summary>
		///     The current changeset number for build path.
		/// </summary>
		/// <returns></returns>
		public static int CurrentChangesetId
		{
			get { return Impl.CurrentChangesetId; }			
		}

		/// <summary>
		///     The current changeset number for specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static int CurrentChangesetOf(FileSystemPath path)
		{
			return Impl.CurrentChangesetOf(path);
		}

		/// <summary>
		///     Creates <c>ServerPath</c> instance from string representation.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static ServerPath AsServerPath(this string path)
		{
			if (path == null)
				throw new ArgumentException("Tfs.AsServerPath(path): path must not be null");

			return new ServerPath(path, false);
		}

		/// <summary>
		///     Access point to native Team Foundation API.
		/// </summary>
		/// <remarks>
		///     Use this only if no appropriate high-level tools.
		/// </remarks>
		// ReSharper disable once InconsistentNaming
		public static TfsTeamProjectCollection __TeamProjectCollection
		{
			get { return Impl.TeamProjectCollection; }
		}
	}
}