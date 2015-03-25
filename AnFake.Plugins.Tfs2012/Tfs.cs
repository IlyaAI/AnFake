using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Core;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Server;

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

		/// <summary>
		///		Checks TFS connection. Throws if something wrong.
		/// </summary>
		/// <param name="tfsUri">TFS connection URI</param>
		public static void CheckConnection(string tfsUri)
		{
			using (var teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsUri)))
			{
				teamProjectCollection.Connect(ConnectOptions.None);
			}			
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
		///     Current team project name.
		/// </summary>		
		public static string TeamProject
		{
			get { return Impl.TeamProject; }
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
		///		Gets list of available projects.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetTeamProjects()
		{
			return Impl.TeamProjectCollection
				.GetService<ICommonStructureService>()
				.ListProjects()
				.Select(x => x.Name);
		}

		/// <summary>
		///		Returns true if project with specified name exists in collection and false other wise.
		/// </summary>
		/// <param name="name">team project name</param>
		/// <returns>true if exists</returns>
		public static bool HasTeamProject(string name)
		{
			return GetTeamProjects().Any(x => x == name);
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