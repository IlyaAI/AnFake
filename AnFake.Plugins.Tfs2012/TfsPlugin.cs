using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using AnFake.Core.Integration.Tests;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsPlugin : Core.Integration.IVersionControl, Core.Integration.IBuildServer
	{
		private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

		private const string CustomInformationNode = "AnFake";
		private const string SummaryKey = "AnFakeSummary";
		private const string SummaryHeader = "AnFake Summary";
		private const int SummaryPriority = 199;
		private const string OverviewKey = "AnFakeOverview";
		private const string OverviewHeader = "Overview";
		private const int OverviewPriority = 150;
		private const string LocalTemp = ".tmp";
		
		private readonly Object _mutex = new Object();
		private readonly Queue<TraceMessage> _messages = new Queue<TraceMessage>();
		private Timer _flusher;

		private readonly TfsTeamProjectCollection _teamProjectCollection;
		
		private readonly IBuildDetail _build;
		private readonly IBuildInformation _tracker;

		private VersionControlServer _vcs;
		private FileSystemPath _logsDropPath;
		private bool _hasDropErrors;
		private bool _hasBuildErrorsOrWarns;
		private bool _isInfoCacheUpToDate;

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
				var buildServer = (Microsoft.TeamFoundation.Build.Client.IBuildServer)_teamProjectCollection.GetService(typeof(Microsoft.TeamFoundation.Build.Client.IBuildServer));

				_build = buildServer.QueryBuildsByUri(
					new[] { new Uri(buildUri) },
					new[] { "ActivityTracking" },
					QueryOptions.Definitions).Single();

				if (_build == null)
					throw new InvalidConfigurationException(String.Format("TFS plugin unable to find build '{0}'", buildUri));

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

				if (!String.IsNullOrEmpty(activityInstanceId))
				{
					var activity = InformationNodeConverters.GetActivityTracking(_build, activityInstanceId);
					if (activity == null)
						throw new InvalidConfigurationException(String.Format("TFS plugin unable to find activity with InstanceId='{0}'", activityInstanceId));

					_tracker = activity.Node.Children;					

					Trace.MessageReceived += OnMessage;
					TestResultAware.Failed += OnTestFailed;
					Target.Finished += OnTargetFinished;

					MyBuild.Started += OnBuildStarted;
					MyBuild.Finished += OnBuildFinished;
				}
			}
			else
			{
				_build = null;
				_tracker = null;
			}

			Snapshot.FileSaved += OnFileSaved;
			Snapshot.FileReverted += OnFileReverted;
		}		

		public TfsTeamProjectCollection TeamProjectCollection
		{
			get { return _teamProjectCollection; }
		}

		public string TeamProject
		{
			get
			{
				return _build != null
					? _build.TeamProject
					: MyBuild.GetProp("Tfs.TeamProject");
			}
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

		public bool HasBuild
		{
			get { return _build != null; }
		}

		public VersionControlServer Vcs
		{
			get { return _vcs ?? (_vcs = _teamProjectCollection.GetService<VersionControlServer>()); }
		}

		public ServerPath SourcesRoot
		{
			get
			{
				var buildPath = MyBuild.Current.Path;
				var ws = GetWorkspace(buildPath);

				return ws.GetServerItemForLocalItem(buildPath.Full).AsServerPath();
			}
		}

		public Workspace FindWorkspace(FileSystemPath localPath)
		{
			var ws = Vcs.TryGetWorkspace(localPath.Full);
			if (ws != null || _isInfoCacheUpToDate)
				return ws;

			Trace.InfoFormat("TfsPlugin: unable to find workspace for local path, trying to update info cache...\n  LocalPath: {0}", localPath.Full);

			Workstation.Current.UpdateWorkspaceInfoCache(Vcs, User.Current);
			_isInfoCacheUpToDate = true;

			ws = Vcs.TryGetWorkspace(localPath.Full);

			Trace.Info(
				ws != null 
					? "TfsPlugin: workspace info cache successfully updated." 
					: "TfsPlugin: workspace info cache updated but this didn't help.");

			return ws;
		}

		public Workspace GetWorkspace(FileSystemPath localPath)
		{
			var ws = FindWorkspace(localPath);
			if (ws == null)
				throw new InvalidConfigurationException(String.Format("There is no working folder mapping for '{0}'.", localPath.Full));

			return ws;
		}

		public Workspace FindWorkspace(string workspaceName)
		{
			try
			{
				var ws = Vcs.GetWorkspace(workspaceName, User.Current);

				if (!ws.IsDeleted && ws.MappingsAvailable)
					return ws;
			}
			catch (WorkspaceNotFoundException)
			{
			}

			return null;
		}		

		public int CurrentChangesetOf(FileSystemPath path)
		{
			var ws = GetWorkspace(path);
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

		public string GetBuildCustomField(string name, string defValue)
		{
			lock (_mutex)
			{
				var node = _build.Information
					.GetNodesByType(CustomInformationNode)
					.FirstOrDefault();

				if (node == null)
					return defValue;

				string value;
				return node.Fields.TryGetValue(name, out value)
					? value
					: defValue;
			}			
		}

		public void SetBuildCustomField(string name, string value)
		{
			lock (_mutex)
			{
				var node = _build.Information
					.GetNodesByType(CustomInformationNode)
					.FirstOrDefault();

				if (node == null)
				{
					node = _build.Information.CreateNode();
					node.Type = CustomInformationNode;
				}

				node.Fields[name] = value;

				_build.Information.Save();
			}			
		}

		public void Save(IBuildDetail build)
		{
			lock (_mutex)
			{
				build.Save();
			}
		}

		// IVersionControl members

		public int CurrentChangesetId
		{
			get
			{
				return CurrentChangesetOf(MyBuild.Current.Path);
			}
		}

		public Core.Integration.IChangeset GetChangeset(int changesetId)
		{
			return new TfsChangeset(Vcs.GetChangeset(changesetId));
		}

		//

		// IBuildServer members

		public bool IsLocal
		{
			get { return _build == null; }
		}

		public FileSystemPath DropLocation
		{
			get
			{
				return _build != null 
					? _build.DropLocation.AsPath() 
					: BuildServer.Local.DropLocation;
			}
		}

		public FileSystemPath LogsLocation
		{
			get
			{
				return _build != null
					? _logsDropPath
					: BuildServer.Local.LogsLocation;
			}
		}

		//

		//
		// TFS is too "smart" and treats files as changed even its content fully identical to server's item,
		// so we query pending changes when file is saved to snapshot and remember it if no such changes. 
		// Then when file is reverted we perfrom TFS Undo operation if it hadn't changes before.
		//

		private readonly ISet<Snapshot.SavedFile> _unchangedFiles = new HashSet<Snapshot.SavedFile>();

		private void OnFileSaved(object sender, Snapshot.SavedFile savedFile)
		{
			var ws = Vcs.TryGetWorkspace(savedFile.Path.Full);
			if (ws == null)
				return;

			var pendingSets = ws.QueryPendingSets(
				new[] { savedFile.Path.Full },
				RecursionType.None,
				ws.Name,
				ws.OwnerName,
				false);

			if (pendingSets.Length == 0)
			{
				_unchangedFiles.Add(savedFile);
			}
		}

		private void OnFileReverted(object sender, Snapshot.SavedFile savedFile)
		{
			if (!_unchangedFiles.Remove(savedFile))
				return;

			var ws = Vcs.TryGetWorkspace(savedFile.Path.Full);
			if (ws == null)
				return;

			ws.Undo(savedFile.Path.Full, RecursionType.None);			
		}		

		//

		private void OnMessage(object sender, TraceMessage message)
		{
			//
			// To prevent too active TFS accessing we save messages just once in FlushInterval
			//
			lock (_mutex)
			{
				_messages.Enqueue(message);
			}
		}

		private void FlushMessages()
		{			
			lock (_mutex)
			{
				if (_tracker == null)
				{
					_messages.Clear();
					return;
				}

				while (_messages.Count > 0)
				{
					var message = _messages.Dequeue();

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
				}

				_tracker.Save();
			}
		}

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			Trace.Info(">>> TfsPlugin.OnBuildStarted");

			Folders.Create(LocalTemp);

			var rdpPath = LocalTemp.AsPath() / Environment.MachineName + ".rdp";
			Text.WriteTo(rdpPath.AsFile(), String.Format("full address:s:{0}", Environment.MachineName));

			FlushMessages();

			// flusher isn't started yet so we need not lock here

			_build.Information.AddCustomSummaryInformation(
				String.Format("Build Agent: [{0}]({1})", Environment.MachineName, rdpPath.ToUnc()),
				OverviewKey, OverviewHeader, OverviewPriority);

			_build.Information.AddCustomSummaryInformation(
				String.Format("Build Folder: [{0}]({1})", MyBuild.Current.Path.Full, MyBuild.Current.Path.ToUnc()),
				OverviewKey, OverviewHeader, OverviewPriority);

			if (!String.IsNullOrEmpty(_build.DropLocation))
			{
				_build.Information.AddCustomSummaryInformation(
					String.Format("Drop Folder: [{0}]({0})", _build.DropLocation),
					OverviewKey, OverviewHeader, OverviewPriority);
			}
			else
			{
				_build.Information.AddCustomSummaryInformation(
					"Drop Folder: (none)",
					OverviewKey, OverviewHeader, OverviewPriority);
			}

			if (!String.IsNullOrEmpty(_build.DropLocation))
			{
				_logsDropPath = _build.DropLocation.AsPath() / "logs";
				if (!SafeOp.Try(Folders.Clean, _logsDropPath))
				{
					_logsDropPath = null;

					_build.Information.AddCustomSummaryInformation(
						"(!) Drop location is inaccessible, logs will be unavailable.",
						SummaryKey, SummaryHeader, SummaryPriority);
				}
			}

			_build.Information.Save();
			
			if (_tracker != null)
			{
				_flusher = new Timer(x => FlushMessages(), null, FlushInterval, FlushInterval);
			}			

			Trace.Info("<<< TfsPlugin.OnBuildStarted");
		}

		private void OnTestFailed(object sender, TestResult test)
		{
			if (String.IsNullOrEmpty(test.Output))
				return;

			try
			{
				var outPath = LocalTemp.AsPath() / String.Format("{0}.{1}", test.Suite, test.Name).MakeUnique(".txt");
				Text.WriteTo(outPath.AsFile(), test.Output);

				test.Links.Add(new Hyperlink(outPath.Full, "Output"));
			}
			catch (Exception e)
			{
				Log.WarnFormat("TfsPlugin.OnTestFailed: {0}", AnFakeException.ToString(e));
			}					
		}

		private void OnTargetFinished(object sender, Target.RunDetails details)
		{
			Trace.Info(">>> TfsPlugin.OnTargetFinished");

			FlushMessages();

			lock (_mutex) // flusher is started so we must lock
			{
				var topTarget = (Target) sender;
				var summary = new StringBuilder(512);

				foreach (var target in details.ExecutedTargets)
				{
					summary
						.Clear()
						.AppendFormat(@"{0}: {1,3} error(s) {2,3} warning(s) {3,3} messages(s)  {4:hh\:mm\:ss}  {5}",
							target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount,
							target.RunTime, target.State.ToHumanReadable().ToUpperInvariant());

					_build.Information
						.AddCustomSummaryInformation(summary.ToString(), SummaryKey, SummaryHeader, SummaryPriority);

					foreach (var message in target.Messages.Where(x => x.Level == TraceMessageLevel.Summary))
					{
						summary
							.Clear()
							.AppendFormat("    {0}", message.Message);

						JoinLinks(summary, message.Links);

						_build.Information
							.AddCustomSummaryInformation(summary.ToString(), SummaryKey, SummaryHeader, SummaryPriority);
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
					.AddCustomSummaryInformation(new string('=', 48), SummaryKey, SummaryHeader, SummaryPriority);
				_build.Information
					.AddCustomSummaryInformation(summary.ToString(), SummaryKey, SummaryHeader, SummaryPriority);
				_build.Information
					.AddCustomSummaryInformation(" ", SummaryKey, SummaryHeader, SummaryPriority);

				_build.Information.Save();
			}

			Trace.Info("<<< TfsPlugin.OnTargetFinished");
		}		

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			Trace.Info(">>> TfsPlugin.OnBuildFinished");

			if (_flusher != null)
			{
				var waiter = new ManualResetEvent(false);
				_flusher.Dispose(waiter);
				_flusher = null;
				waiter.WaitOne();
			}

			// flusher is stopped already so we need not lock here
			
			if (_logsDropPath != null)
			{
				var buildLog = AppendLink(
					new StringBuilder(128),
					new Hyperlink(MyBuild.Current.LogFile.Path.Full, "build.log"),
					"build.log");

				if (buildLog.Length > 0)
				{
					_build.LogLocation = (_logsDropPath/"build.log").Full;
					_build.Information
						.AddCustomSummaryInformation(buildLog.ToString(), SummaryKey, SummaryHeader, SummaryPriority);
				}
			}

			if (_logsDropPath != null)
			{
				if (_hasDropErrors)
				{
					_build.Information.AddCustomSummaryInformation(
						"(!) There are troubles accessing drop location, some logs are unavailable.",
						SummaryKey, SummaryHeader, SummaryPriority);
				}
			}
			else
			{
				_build.Information.AddCustomSummaryInformation(
					"Hint: set up drop location to get access to complete build logs.",
					SummaryKey, SummaryHeader, SummaryPriority);
			}

			if (_hasBuildErrorsOrWarns)
			{
				_build.Information.AddCustomSummaryInformation(
					"See the section below for error/warning list.",
					SummaryKey, SummaryHeader, SummaryPriority);
			}

			_build.Information.Save();

			Trace.Info("<<< TfsPlugin.OnBuildFinished");

			FlushMessages();

			_build.FinalizeStatus(details.Status.AsTfsBuildStatus());
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

			var startedAt = sb.Length;
			sb.Append(prefix);

			var prevLength = sb.Length;
			AppendLink(sb, links[0]);

			for (var i = 1; i < links.Count; i++)
			{
				if (sb.Length > prevLength)
				{
					sb.Append(separator);
				}

				prevLength = sb.Length;
				AppendLink(sb, links[i]);
			}

			// if no one link generated then remove prefix
			if (sb.Length - startedAt == prefix.Length)
			{
				sb.Remove(startedAt, sb.Length - startedAt);
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
	}
}