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
		///		Represents build details provided by Tfs.
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

			public Uri Uri
			{
				get { return new Uri(TfsBuild.Impl.Linking.GetArtifactUrl(_impl.Uri.ToString())); }
			}

			public Uri NativeUri
			{
				get { return _impl.Uri; }
			}

			public string BuildNumber
			{
				get { return _impl.BuildNumber; }
			}

			public string BuildDefinitionName
			{
				get { return _impl.BuildDefinition.Name; }
			}

			public string TeamProject
			{
				get { return _impl.TeamProject; }
			}

			public string SourceVersion
			{
				get { return _impl.SourceGetVersion; }
			}

			public bool HasDropLocation
			{
				get
				{
					return !String.IsNullOrEmpty(_impl.DropLocation);
				}
			}

			public FileSystemPath DropLocation
			{
				get
				{
					if (String.IsNullOrEmpty(_impl.DropLocation))
						throw new InvalidConfigurationException("Drop location isn't specified.");

					return _impl.DropLocation.AsPath();
				}
			}

			public string Quality
			{
				get { return _impl.Quality; }
				set { _impl.Quality = value; }
			}

			public MyBuild.Status Status
			{
				get { return _impl.Status.AsMyBuildStatus(); }
			}

			public DateTime StartTime
			{
				get { return _impl.StartTime; }
			}

			public DateTime FinishTime
			{
				get { return _impl.FinishTime; }
			}

			public string GetCustomField(string name)
			{
				var value = TfsBuild.Impl.GetBuildCustomField(name, null);
				if (value == null)
					throw new InvalidConfigurationException(String.Format("Build '{0}' doesn't contain custom field '{1}'.", _impl.BuildNumber, name));

				return value;
			}

			public string GetCustomField(string name, string defValue)
			{
				return TfsBuild.Impl.GetBuildCustomField(name, defValue);
			}

			public void SetCustomField(string name, string value)
			{
				TfsBuild.Impl.SetBuildCustomField(name, value);
			}

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

		public static TfsBuildSummarySection GetSummarySection(string key, string header)
		{
			return GetSummarySection(key, header, 150);
		}

		public static TfsBuildSummarySection GetSummarySection(string key, string header, int priority)
		{
			return new TfsBuildSummarySection(Impl.Build, key, header, priority, Impl.HasLogsLocation ? Impl.LogsLocation : null);
		}

		public static IEnumerable<BuildDetail> QueryAll(int limit)
		{
			return QueryAll(Current.BuildDefinitionName, limit);
		}

		public static IEnumerable<BuildDetail> QueryAll(string definitionName, int limit)
		{
			var buildSvc = Current.Impl.BuildServer;

			var definition = buildSvc.GetBuildDefinition(Current.TeamProject, definitionName);
			var spec = buildSvc.CreateBuildDetailSpec(definition);
			spec.MaxBuildsPerDefinition = limit;
			spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
			spec.Status = BuildStatus.Succeeded | BuildStatus.PartiallySucceeded | BuildStatus.Failed;
			spec.QueryDeletedOption = QueryDeletedOption.ExcludeDeleted;			

			var results = buildSvc.QueryBuilds(spec);
			return results.Builds.Select(x => new BuildDetail(x));
		}

		public static IEnumerable<BuildDetail> QueryByQuality(string quality, int limit)
		{
			return QueryByQuality(Current.BuildDefinitionName, quality, limit);
		}

		public static IEnumerable<BuildDetail> QueryByQuality(string definitionName, string quality, int limit)
		{
			var buildSvc = Current.Impl.BuildServer;

			var definition = buildSvc.GetBuildDefinition(Current.TeamProject, definitionName);
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