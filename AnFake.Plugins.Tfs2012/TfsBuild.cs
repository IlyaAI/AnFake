using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Core;
using AnFake.Core.Exceptions;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Plugins.Tfs2012
{
	/// <summary>
	///		Represents tools related to Team Build.
	/// </summary>
	public static class TfsBuild
	{
		private static TfsPlugin _impl;

		private static TfsPlugin Impl
		{
			get
			{
				if (_impl != null)
					return _impl;

				_impl = Plugin.Get<TfsPlugin>();
				_impl.Disposed += () =>
				{
					_impl = null;
					_current = null;
				};

				return _impl;
			}
		}

		/// <summary>
		///		Represents build details provided by TFS.
		/// </summary>
		/// <remarks>
		///		Really, this is simplified subset of <c>Microsoft.TeamFoundation.Build.Client.IBuildDetail</c> interface.
		/// </remarks>
		public sealed class BuildDetail
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private readonly IBuildDetail _impl;

			internal BuildDetail(IBuildDetail impl)
			{
				_impl = impl;
			}

			/// <summary>
			///		http(s):// URI of this build (read-only).
			/// </summary>			
			/// <seealso cref="NativeUri"/>
			public Uri Uri
			{
				get { return new Uri(TfsBuild.Impl.Linking.GetArtifactUrl(_impl.Uri.ToString())); }
			}

			/// <summary>
			///		vstfs:// URI of this build (read-only).
			/// </summary>
			public Uri NativeUri
			{
				get { return _impl.Uri; }
			}

			/// <summary>
			///		Current build name (read-only).
			/// </summary>
			/// <remarks>
			///		Normally build number formed as build definition name concatenated with current date and build index.
			/// </remarks>
			public string BuildNumber
			{
				get { return _impl.BuildNumber; }
			}

			/// <summary>
			///		Build definition name (read-only).
			/// </summary>
			public string BuildDefinitionName
			{
				get { return _impl.BuildDefinition.Name; }
			}

			/// <summary>
			///		Team project name (read-only).
			/// </summary>
			public string TeamProject
			{
				get { return _impl.TeamProject; }
			}

			/// <summary>
			///		Source version in TFS version spec format (read-only), e.g. C100 means changeset 100.
			/// </summary>
			public string SourceVersion
			{
				get { return _impl.SourceGetVersion; }
			}

			/// <summary>
			///		Whether drop location specified? (read-only)
			/// </summary>
			public bool HasDropLocation
			{
				get
				{
					return !String.IsNullOrEmpty(_impl.DropLocation);
				}
			}

			/// <summary>
			///		Drop location (not null, read-only). Throws if not specified.
			/// </summary>
			public FileSystemPath DropLocation
			{
				get
				{
					if (String.IsNullOrEmpty(_impl.DropLocation))
						throw new InvalidConfigurationException("Drop location isn't specified.");

					return _impl.DropLocation.AsPath();
				}
			}

			/// <summary>
			///		Build quality.
			/// </summary>
			/// <remarks>
			///		Don't forget to call <c>Save()</c> after changing.
			/// </remarks>
			public string Quality
			{
				get { return _impl.Quality; }
				set { _impl.Quality = value; }
			}

			/// <summary>
			///		Build status (read-only).
			/// </summary>
			/// <seealso cref="MyBuild.Status"/>
			public MyBuild.Status Status
			{
				get { return _impl.Status.AsMyBuildStatus(); }
			}

			/// <summary>
			///		Build start date/time (read-only).
			/// </summary>
			public DateTime StartTime
			{
				get { return _impl.StartTime; }
			}

			/// <summary>
			///		Build finish date/time (read-only).
			/// </summary>
			public DateTime FinishTime
			{
				get { return _impl.FinishTime; }
			}

			/// <summary>
			///		Returns subfolder inside drop location for specified artifact type. Throws if drop location not specified.
			/// </summary>
			public FileSystemPath GetDropLocationOf(ArtifactType type)
			{
				return DropLocation/type.ToString();
			}

			/// <summary>
			///		Gets custom info filed value. Throws if no such field.
			/// </summary>
			/// <param name="name">field name (not null or empty)</param>
			/// <returns>field value (not null)</returns>
			public string GetCustomField(string name)
			{
				if (String.IsNullOrEmpty(name))
					throw new ArgumentException("TfsBuild.BuildDetail.GetCustomField(name): name must not be null or empty");

				var value = TfsPlugin.GetBuildCustomField(_impl, name, null);
				if (value == null)
					throw new InvalidConfigurationException(String.Format("Build '{0}' doesn't contain custom field '{1}'.", _impl.BuildNumber, name));

				return value;
			}

			/// <summary>
			///		Gets custom info filed value. Returns given default value if no such field.
			/// </summary>
			/// <param name="name">field name (not null or empty)</param>
			/// <param name="defValue">default value</param>
			/// <returns>field value</returns>
			public string GetCustomField(string name, string defValue)
			{
				if (String.IsNullOrEmpty(name))
					throw new ArgumentException("TfsBuild.BuildDetail.GetCustomField(name, defValue): name must not be null or empty");

				return TfsPlugin.GetBuildCustomField(_impl, name, defValue);
			}

			/// <summary>
			///		Sets custom info field value.
			/// </summary>
			/// <remarks>
			///		Don't forget to call <c>Save()</c> after setting custom field.
			/// </remarks>
			/// <param name="name">field name (not null or empty)</param>
			/// <param name="value">field value (not null)</param>
			public void SetCustomField(string name, string value)
			{
				if (String.IsNullOrEmpty(name))
					throw new ArgumentException("TfsBuild.BuildDetail.SetCustomField(name, value): name must not be null or empty");
				if (value == null)
					throw new ArgumentException("TfsBuild.BuildDetail.SetCustomField(name, value): value must not be null");

				TfsPlugin.SetBuildCustomField(_impl, name, value);
			}

			/// <summary>
			///		Saves changes.
			/// </summary>
			public void Save()
			{
				_impl.Information.Save();
				_impl.Save();
			}

			// ReSharper disable once MemberHidesStaticFromOuterClass
			internal IBuildDetail Impl
			{
				get { return _impl; }
			}
		}

		private static BuildDetail _current;

		/// <summary>
		///		Current build details.
		/// </summary>
		/// <remarks>
		///		An exception is thrown if build details unavailable.
		/// </remarks>
		public static BuildDetail Current
		{
			get { return _current ?? (_current = new BuildDetail(Impl.Build)); }
		}

		/// <summary>
		///		Is TFS build details available?
		/// </summary>
		public static bool HasCurrent
		{
			get { return Impl.HasBuild; }
		}

		private static BuildDetail _pipeIn;

		/// <summary>
		///		Is this build part of pipeline?
		/// </summary>
		public static bool IsPipelined
		{
			get { return MyBuild.HasProp("Tfs.PipeIn"); }
		}

		/// <summary>
		///		Input build from previous pipeline step.
		/// </summary>
		public static BuildDetail PipeIn
		{
			get { return _pipeIn ?? (_pipeIn = new BuildDetail(Impl.GetBuildByUri(new Uri(MyBuild.GetProp("Tfs.PipeIn"))))); }
		}

		/// <summary>
		///		Returns instance of <c>TfsBuildSummarySection</c> which represents section in summary view displayd by Visual Studio after build completion.
		/// </summary>
		/// <param name="key">section key (alpha-numeric, no dots or hyphens)</param>
		/// <param name="header">section header which will be displayed in VS</param>
		/// <returns><c>TfsBuildSummarySection</c> instance</returns>
		/// <seealso cref="TfsBuildSummarySection"/>
		public static TfsBuildSummarySection GetSummarySection(string key, string header)
		{
			return GetSummarySection(key, header, 150);
		}

		/// <summary>
		///		Returns instance of <c>TfsBuildSummarySection</c> which represents section in summary view displayd by Visual Studio after build completion.
		/// </summary>
		/// <param name="key">section key (alpha-numeric, no dots or hyphens)</param>
		/// <param name="header">section header which will be displayed in VS</param>
		/// <param name="priority">relative order of appearance</param>
		/// <returns><c>TfsBuildSummarySection</c> instance</returns>
		/// <seealso cref="TfsBuildSummarySection"/>
		public static TfsBuildSummarySection GetSummarySection(string key, string header, int priority)
		{
			return new TfsBuildSummarySection(Impl.Build, key, header, priority);
		}

		/// <summary>
		///		Queries all builds related to current's one build definition.
		/// </summary>
		/// <param name="limit">how many builds to return</param>
		/// <returns>set of builds</returns>
		public static IEnumerable<BuildDetail> QueryAll(int limit)
		{
			return QueryAll(Current.BuildDefinitionName, limit);
		}

		/// <summary>
		///		Queries all builds related to specified build definition.
		/// </summary>
		/// <param name="definitionName">build definition name</param>
		/// <param name="limit">how many builds to return</param>
		/// <returns>set of build</returns>
		public static IEnumerable<BuildDetail> QueryAll(string definitionName, int limit)
		{
			var buildSvc = Impl.TeamProjectCollection.GetService<IBuildServer>();

			var definition = buildSvc.GetBuildDefinition(Impl.TeamProject, definitionName);
			var spec = buildSvc.CreateBuildDetailSpec(definition);
			spec.MaxBuildsPerDefinition = limit;
			spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
			spec.Status = BuildStatus.Succeeded | BuildStatus.PartiallySucceeded | BuildStatus.Failed;
			spec.QueryDeletedOption = QueryDeletedOption.ExcludeDeleted;			

			var results = buildSvc.QueryBuilds(spec);
			return results.Builds.Select(x => new BuildDetail(x));
		}

		/// <summary>
		///		Queries builds related to current's one build definition with specified quality.
		/// </summary>
		/// <param name="quality">build quality</param>
		/// <param name="limit">how many builds to return</param>
		/// <returns>set of builds</returns>
		public static IEnumerable<BuildDetail> QueryByQuality(string quality, int limit)
		{
			return QueryByQuality(Current.BuildDefinitionName, quality, limit);
		}

		/// <summary>
		///		Queries builds related to given build definition with specified quality.
		/// </summary>
		/// <param name="definitionName">build definition name</param>
		/// <param name="quality">build quality</param>
		/// <param name="limit">how many builds to return</param>
		/// <returns>set of builds</returns>
		public static IEnumerable<BuildDetail> QueryByQuality(string definitionName, string quality, int limit)
		{
			var buildSvc = Impl.TeamProjectCollection.GetService<IBuildServer>();

			var definition = buildSvc.GetBuildDefinition(Impl.TeamProject, definitionName);
			var spec = buildSvc.CreateBuildDetailSpec(definition);
			spec.MaxBuildsPerDefinition = limit;
			spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
			spec.Status = BuildStatus.Succeeded | BuildStatus.PartiallySucceeded;
			spec.QueryDeletedOption = QueryDeletedOption.ExcludeDeleted;
			spec.Quality = quality;

			var results = buildSvc.QueryBuilds(spec);
			return results.Builds.Select(x => new BuildDetail(x));
		}		
	}
}