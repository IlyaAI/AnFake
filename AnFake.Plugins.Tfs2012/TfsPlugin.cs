using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration.Tests;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsPlugin : Core.Integration.IVersionControl
	{
		private const string SectionKey = "AnFake";
		private const string SectionHeader = "AnFake Summary";
		private const int SectionPriority = 199;
		private const string LocalTemp = ".tmp";

		private readonly TfsTeamProjectCollection _teamProjectCollection;
		
		private readonly IBuildDetail _build;
		private readonly IBuildInformation _tracker;

		private VersionControlServer _vcs;
		private FileSystemPath _logsDropPath;
		private bool _hasDropErrors;
		private bool _hasBuildErrorsOrWarns;

		public TfsPlugin()
		{
			string tfsUri;
			string buildUri;
			string activityInstanceId;
			string privateDropLocation;

			var props = MyBuild.Current.Properties;

			if (!props.TryGetValue("Tfs.Uri", out tfsUri))
				throw new InvalidConfigurationException("TFS plugin requires 'Tfs.Uri' to be specified in build properties.");

			props.TryGetValue("Tfs.BuildUri", out buildUri);
			props.TryGetValue("Tfs.ActivityInstanceId", out activityInstanceId);
			props.TryGetValue("Tfs.PrivateDropLocation", out privateDropLocation);

			_teamProjectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsUri));

			if (!String.IsNullOrEmpty(buildUri))
			{
				if (String.IsNullOrEmpty(activityInstanceId))
					throw new InvalidConfigurationException("TFS plugin requires both 'Tfs.BuildUri' and 'Tfs.ActivityInstanceId' to be specified in build properties.");

				var buildServer = (IBuildServer)_teamProjectCollection.GetService(typeof(IBuildServer));

				_build = buildServer.QueryBuildsByUri(
					new[] { new Uri(buildUri) },
					new[] { "ActivityTracking" },
					QueryOptions.Definitions).Single();

				if (_build == null)
					throw new InvalidConfigurationException(String.Format("TFS plugin unable to find build '{0}'", buildUri));

				var activity = InformationNodeConverters.GetActivityTracking(_build, activityInstanceId);
				if (activity == null)
					throw new InvalidConfigurationException(String.Format("TFS plugin unable to find Activity with InstanceId='{0}'", activityInstanceId));
				
				_tracker = activity.Node.Children;

				if (String.IsNullOrEmpty(_build.DropLocation))
				{
					var dropLocationRoot = !String.IsNullOrEmpty(_build.DropLocationRoot)
						? _build.DropLocationRoot
						: !String.IsNullOrEmpty(privateDropLocation)
							? privateDropLocation
							: null;

					if (dropLocationRoot != null)
					{
						if (!dropLocationRoot.StartsWith(@"\\"))
							throw new InvalidConfigurationException(String.Format("Now UNC path only supported as DropLocation but provided '{0}'", dropLocationRoot));

						_build.DropLocation = (dropLocationRoot.AsPath() / _build.BuildDefinition.Name / _build.BuildNumber).Spec;
						_build.Save();
					}
				}

				Trace.MessageReceived += OnMessage;
				TestResultAware.Failed += OnTestFailed;
				Target.Finished += OnTargetFinished;

				MyBuild.Started += OnBuildStarted;
				MyBuild.Finished += OnBuildFinished;
			}
			else
			{
				_build = null;
				_tracker = null;
			}
		}		

		public TfsTeamProjectCollection TeamProjectCollection
		{
			get { return _teamProjectCollection; }
		}

		public IBuildDetail Build
		{
			get
			{
				if (_build == null)
					throw new InvalidConfigurationException("Build details are unavailable. Hint: you should specify 'Tfs.BuildUri' and 'Tfs.ActivityInstanceId' in build properties.");

				return _build;
			}
		}

		public VersionControlServer Vcs
		{
			get { return _vcs ?? (_vcs = _teamProjectCollection.GetService<VersionControlServer>()); }
		}

		public int LastChangesetOf(FileSystemPath path)
		{
			var ws = Vcs.GetWorkspace(path.Full);
			var queryParams = new QueryHistoryParameters(path.Full, RecursionType.Full)
			{
				VersionStart = new ChangesetVersionSpec(1),
				VersionEnd = new WorkspaceVersionSpec(ws),
				MaxResults = 1
			};

			var changeset = Vcs.QueryHistory(queryParams).FirstOrDefault();
			return changeset != null
				? changeset.ChangesetId
				: 0;
		}

		// IVersionControl members

		public string CurrentChangesetId
		{
			get
			{
				return LastChangesetOf("".AsPath())
					.ToString(CultureInfo.InvariantCulture);
			}
		}

		public Core.Integration.IChangeset GetChangeset(string changesetId)
		{
			return new TfsChangeset(Vcs.GetChangeset(Int32.Parse(changesetId)));
		}

		//

		private void OnMessage(object sender, TraceMessage message)
		{
			switch (message.Level)
			{
				case TraceMessageLevel.Debug:
					_tracker.AddBuildMessage(message.ToString("mfd"), BuildMessageImportance.Low, DateTime.Now);					
					break;

				case TraceMessageLevel.Info:
					_tracker.AddBuildMessage(message.ToString("mfd"), BuildMessageImportance.Normal, DateTime.Now);
					break;

				case TraceMessageLevel.Summary:
					_tracker.AddBuildMessage(message.ToString("mfd"), BuildMessageImportance.High, DateTime.Now);
					break;

				case TraceMessageLevel.Warning:
					_tracker.AddBuildWarning(FormatMessage(message), DateTime.Now);
					break;

				case TraceMessageLevel.Error:
					_tracker.AddBuildError(FormatMessage(message), DateTime.Now);
					break;
			}

			_tracker.Save();
		}

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			Folders.Create(LocalTemp);

			if (String.IsNullOrEmpty(_build.DropLocation))
				return;

			_logsDropPath = _build.DropLocation.AsPath() / "logs";
			if (SafeOp.Try(Folders.Clean, _logsDropPath))
				return;

			_logsDropPath = null;

			_build.Information.AddCustomSummaryInformation(
				"(!) Drop location is inaccessible, logs will be unavailable.",
				SectionKey, SectionHeader, SectionPriority);

			_build.Information.Save();
		}

		private void OnTestFailed(object sender, TestResult test)
		{
			if (String.IsNullOrEmpty(test.Output))
				return;

			try
			{
				var outFile = LocalTemp.AsPath() / String.Format("{0}.{1}", test.Suite, test.Name).MakeUnique(".txt");
				File.WriteAllText(outFile.Full, test.Output);

				test.Links.Add(new Hyperlink(outFile.Full, "Output"));
			}
			catch (Exception e)
			{
				Log.WarnFormat("TfsPlugin.OnTestFailed: {0}", e.Message);
			}					
		}

		private void OnTargetFinished(object sender, Target.RunDetails details)
		{
			var topTarget = (Target) sender;
			var summary = new StringBuilder(512);			

			foreach (var target in details.ExecutedTargets)
			{
				summary
					.Clear()
					.AppendFormat("{0}: {1,3} error(s) {2,3} warning(s) {3,3} messages(s)  {4}",
						target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount, 
						target.State.ToHumanReadable().ToUpperInvariant());

				_build.Information
					.AddCustomSummaryInformation(summary.ToString(), SectionKey, SectionHeader, SectionPriority);

				foreach (var message in target.Messages.Where(x => x.Level == TraceMessageLevel.Summary))
				{
					summary
						.Clear()
						.AppendFormat("    {0}", message.Message);

					JoinLinks(summary, message.Links);
					
					_build.Information
						.AddCustomSummaryInformation(summary.ToString(), SectionKey, SectionHeader, SectionPriority);
				}

				_hasBuildErrorsOrWarns |= 
					target.Messages.ErrorsCount > 0 || 
					target.Messages.WarningsCount > 0;
			}

			summary
				.Clear()
				.AppendFormat(
					"'{0}' {1}", 
					topTarget.Name, 
					topTarget.State.ToHumanReadable().ToUpperInvariant());

			_build.Information
				.AddCustomSummaryInformation(new string('=', 48), SectionKey, SectionHeader, SectionPriority);
			_build.Information
				.AddCustomSummaryInformation(summary.ToString(), SectionKey, SectionHeader, SectionPriority);						

			_build.Information
				.Save();			
		}		

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			if (_logsDropPath != null)
			{
				var buildLog = AppendLink(
					new StringBuilder(128),
					new Hyperlink(MyBuild.Current.LogFile.Path.Full, "build.log"),
					"build.log");

				if (buildLog.Length > 0)
				{
					_build.LogLocation = (_logsDropPath / "build.log").Full;
					_build.Information
						.AddCustomSummaryInformation(buildLog.ToString(), SectionKey, SectionHeader, SectionPriority);					
				}
			}			

			if (_logsDropPath != null)
			{
				if (_hasDropErrors)
				{
					_build.Information.AddCustomSummaryInformation(
						"(!) There are troubles accessing drop location, some logs are unavailable.",
						SectionKey, SectionHeader, SectionPriority);
				}
			}
			else
			{
				_build.Information.AddCustomSummaryInformation(
					"Hint: set up drop location to get access to complete build logs.",
					SectionKey, SectionHeader, SectionPriority);
			}

			if (_hasBuildErrorsOrWarns)
			{
				_build.Information.AddCustomSummaryInformation(
					"See the section below for error/warning list.",
					SectionKey, SectionHeader, SectionPriority);
			}

			_build.Information.Save();			
			_build.FinalizeStatus(AsTfsBuildStatus(details.Status));
		}

		private string DropLink(Hyperlink link, string newName = null)
		{
			if (_logsDropPath == null)
				return null;

			var href = (string)null;			
			var srcPath = link.Href.AsPath();
			var dstPath = _logsDropPath / (newName ?? srcPath.LastName);

			if (dstPath.AsFile().Exists() || SafeOp.Try(Files.Copy, srcPath, dstPath, false))
			{
				href = dstPath.Full;
			}
			else
			{
				_hasDropErrors = true;
			}

			return href;
		}

		private StringBuilder AppendLink(StringBuilder sb, Hyperlink link, string newName = null)
		{
			var href = DropLink(link, newName);
			return href != null
				? sb.Append('[').Append(link.Label).Append("](").Append(href).Append(')')
				: sb;
		}

		private StringBuilder JoinLinks(StringBuilder sb, List<Hyperlink> links, string prefix = " ", string separator = " | ")
		{
			if (links.Count == 0)
				return sb;

			sb.Append(prefix);
			AppendLink(sb, links[0]);

			for (var i = 1; i < links.Count; i++)
			{
				sb.Append(separator);
				AppendLink(sb, links[i]);
			}

			return sb;
		}

		private string FormatMessage(TraceMessage message)
		{
			var formatted = new StringBuilder(512)
				.Append(message.ToString("mfd"));

			JoinLinks(formatted, message.Links, "\n");

			return formatted.ToString();
		}

		private static BuildStatus AsTfsBuildStatus(MyBuild.Status status)
		{
			switch (status)
			{
				case MyBuild.Status.Succeeded:
					return BuildStatus.Succeeded;

				case MyBuild.Status.PartiallySucceeded:
					return BuildStatus.PartiallySucceeded;

				case MyBuild.Status.Failed:
					return BuildStatus.Failed;

				default:
					return BuildStatus.None;
			}
		}
	}
}