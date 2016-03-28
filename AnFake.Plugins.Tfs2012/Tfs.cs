using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnFake.Api;
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
		[Flags]
		public enum Role
		{
			/// <summary>
			///		In Self role you can use TFS with explicit API (TfsWorkspace, TfsBuild, etc) but unified API such as <c>BuildServer</c> or <c>VersionControl</c> will be unavailable.
			/// </summary>
			Self					= 0x00,

			/// <summary>
			///		In VersionControl role you can use TFS with explicit API (TfsWorkspace, TfsBuild, etc) and with unified API <c>VersionControl</c> but <c>BuildServer</c> will be unavailable.
			/// </summary>
			VersionControl			= 0x01,

			/// <summary>
			///		In ContinuousIntegration role you can use TFS with explicit API (TfsWorkspace, TfsBuild, etc) and with unified API <c>BuildServer</c> but <c>VersionControl</c> will be unavailable.
			/// </summary>
			ContinuousIntegration	= 0x02,

			/// <summary>
			///		Reserved for future use.
			/// </summary>
			Tracking				= 0x04,

			/// <summary>
			///		In All role you can use TFS with both explicit API (TfsWorkspace, TfsBuild, etc) and unified API-s <c>BuildServer</c> and <c>VersionControl</c>.
			/// </summary>
			All						= 0xFF
		}

		// Assembly resolution stuff
		private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			var asmName = new AssemblyName(args.Name);

			if (!asmName.Name.StartsWith("Microsoft.TeamFoundation", StringComparison.OrdinalIgnoreCase))
				return null;

			if (asmName.Version != new Version(11, 0, 0, 0))
				return null;

			Log.DebugFormat("TFS assembly '{0}' not found. Trying alternative versions...", args.Name);

			try
			{
				asmName.Version = new Version(12, 0, 0, 0);
				var asm = Assembly.Load(asmName);

				Log.DebugFormat("Loaded: '{0}'.", asmName);

				return asm;
			}
			catch (Exception)
			{
				// skip it
			}

			Log.Debug("There is no alternative versions found.");

			return null;
		}

		static Tfs()
		{
			AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
		}
		// /////////////////////////		

		/// <summary>
		///     Connects to Team Foundation Server.
		/// </summary>
		/// <remarks>
		///		Normally you needn't call this method, connection will be established automatically on first request.
		/// </remarks>
		public static void Connect()
		{
			Plugin.Get<TfsPlugin>();			
		}

		/// <summary>
		///     Activates <c>Tfs</c> plugin.
		/// </summary>
		public static void PlugIn()
		{
			PlugIn(Role.All);
		}

		/// <summary>
		///     Activates <c>Tfs</c> plugin with specified roles only.
		/// </summary>
		/// <seealso cref="Tfs.Role"/>
		public static void PlugIn(Role roles)
		{
			SetUpRoles(Plugin.Register<TfsPlugin>().AsSelf(), roles);

						
		}

		/// <summary>
		///     Marks <c>Tfs</c> plugin to be activated on-demand.
		/// </summary>
		/// <remarks>
		///     This method is intended for special cases only (see Extras/tf.fsx for example). Normally you should call
		///     <c>Tfs.PlugIn</c>.
		/// </remarks>
		public static void PlugInOnDemand()
		{
			PlugInOnDemand(Role.All);

		}

		/// <summary>
		///     Marks <c>Tfs</c> plugin to be activated on-demand with specified roles only.
		/// </summary>
		/// <remarks>
		///     This method is intended for special cases only (see Extras/tf.fsx for example). Normally you should call
		///     <c>Tfs.PlugIn</c>.
		/// </remarks>
		public static void PlugInOnDemand(Role roles)
		{
			SetUpRoles(Plugin.RegisterOnDemand<TfsPlugin>().AsSelf(), roles);
			
		}

		private static void SetUpRoles(Plugin.Registrator<TfsPlugin> registrator, Role roles)
		{
			if ((roles & Role.VersionControl) != 0)
			{
				registrator.As<Core.Integration.IVersionControl>();
			}

			if ((roles & Role.ContinuousIntegration) != 0)
			{
				registrator.As<Core.Integration.IBuildServer>();
			}
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