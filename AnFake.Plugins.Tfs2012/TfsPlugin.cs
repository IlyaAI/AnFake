using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using AnFake.Core.Integration.Tests;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsPlugin : Core.Integration.IVersionControl, Core.Integration.IBuildServer, IDisposable
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
					new[] { "ActivityTracking", CustomInformationNode },
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

		// IDisposable members

		public void Dispose()
		{
			Trace.MessageReceived -= OnMessage;
			TestResultAware.Failed -= OnTestFailed;
			Target.Finished -= OnTargetFinished;

			MyBuild.Started -= OnBuildStarted;
			MyBuild.Finished -= OnBuildFinished;

			Snapshot.FileSaved -= OnFileSaved;
			Snapshot.FileReverted -= OnFileReverted;

			if (Disposed != null)
			{
				SafeOp.Try(Disposed);
			}
		}

		public event Action Disposed;

		// Internal API

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

		public bool HasBuild
		{
			get { return _build != null; }
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

		private VersionControlServer _vcs;

		public VersionControlServer Vcs
		{
			get { return _vcs ?? (_vcs = _teamProjectCollection.GetService<VersionControlServer>()); }
		}

		private ILinking _linking;

		public ILinking Linking
		{
			get { return _linking ?? (_linking = _teamProjectCollection.GetService<ILinking>()); }
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

		// IBuildServer members

		public bool IsLocal
		{
			get { return _build == null; }
		}

		public bool HasDropLocation
		{
			get
			{
				if (_build == null)
					return BuildServer.Local.HasDropLocation;

				return !String.IsNullOrEmpty(_build.DropLocation);
			}
		}

		public FileSystemPath DropLocation
		{
			get
			{
				if (_build == null)
					return BuildServer.Local.DropLocation;

				if (String.IsNullOrEmpty(_build.DropLocation))
					throw new InvalidConfigurationException("Drop location is not specified.");

				return _build.DropLocation.AsPath();
			}
		}

		public bool HasLogsLocation
		{
			get
			{
				if (_build == null)
					return BuildServer.Local.HasLogsLocation;

				return _logsDropPath != null;
			}
		}

		public FileSystemPath LogsLocation
		{
			get
			{
				if (_build == null)
					return BuildServer.Local.DropLocation;

				if (_logsDropPath == null)
					throw new InvalidConfigurationException("Drop location is not specified.");

				return _logsDropPath;
			}
		}		

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

		private TfsBuildSummarySection GetOverviewSection()
		{
			return new TfsBuildSummarySection(_build, OverviewKey, OverviewHeader, OverviewPriority, _logsDropPath);
		}

		private TfsBuildSummarySection GetSummarySection()
		{
			return new TfsBuildSummarySection(_build, SummaryKey, SummaryHeader, SummaryPriority, _logsDropPath);
		}

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			Trace.Info(">>> TfsPlugin.OnBuildStarted");

			Folders.Create(LocalTemp);

			var rdpPath = LocalTemp.AsPath() / Environment.MachineName + ".rdp";
			Text.WriteTo(rdpPath.AsFile(), String.Format("full address:s:{0}", Environment.MachineName));

			FlushMessages();

			// flusher isn't started yet so we need not lock here

			var overview = GetOverviewSection();

			overview.Append("Build Agent: ").AppendLink(Environment.MachineName, rdpPath.ToUnc()).Push();
			overview.Append("Build Folder: ").AppendLink(MyBuild.Current.Path.Full, MyBuild.Current.Path.ToUnc()).Push();
			
			overview.Append("Drop Folder: ");
			if (!String.IsNullOrEmpty(_build.DropLocation))
			{
				overview.AppendLink(_build.DropLocation);
			}
			else
			{
				overview.Append("(none)");				
			}
			overview
				.Push()
				.Save();

			if (!String.IsNullOrEmpty(_build.DropLocation))
			{
				_logsDropPath = _build.DropLocation.AsPath() / "logs";
				if (!SafeOp.Try(Folders.Clean, _logsDropPath))
				{
					_logsDropPath = null;

					GetSummarySection()
						.Append("(!) Drop location is inaccessible, logs will be unavailable.")
						.Push()
						.Save();
				}
			}			
			
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
				var summary = GetSummarySection();

				foreach (var target in details.ExecutedTargets)
				{
					summary						
						.AppendFormat(@"{0}: {1,3} error(s) {2,3} warning(s) {3,3} messages(s)  {4:hh\:mm\:ss}  {5}",
							target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount,
							target.RunTime, target.State.ToHumanReadable().ToUpperInvariant())
						.Push();

					foreach (var message in target.Messages.Where(x => x.Level == TraceMessageLevel.Summary))
					{
						summary							
							.AppendFormat("    {0}", message.Message)
							.AppendLinks(message.Links)
							.Push();						
					}

					_hasBuildErrorsOrWarns |=
						target.Messages.ErrorsCount > 0 ||
						target.Messages.WarningsCount > 0;
				}

				summary.Append(new String('=', 48)).Push();
				summary
					.AppendFormat(
						"'{0}' {1}",
						topTarget.Name,
						topTarget.State.ToHumanReadable().ToUpperInvariant())
					.Push();
				summary.Append(" ").Push()
					.Save();

				_hasDropErrors |= summary.HasDropErrors;
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

			var summary = GetSummarySection();
			
			if (_logsDropPath != null)
			{
				// Visual Studio expects predefined name 'build.log' so we need to copy with new name.
				var buildLog = _logsDropPath / "build.log";
				if (SafeOp.Try(Files.Copy, MyBuild.Current.LogFile.Path, buildLog, false))
				{
					summary.AppendLink("build.log", buildLog).Push();
				}
				else
				{
					_hasDropErrors = true;
				}

				if (_hasDropErrors)
				{
					summary.Append("(!) There are troubles accessing drop location, some logs are unavailable.").Push();
				}
			}
			else
			{
				summary.Append("Hint: set up drop location to get access to complete build logs.").Push();
			}

			if (_hasBuildErrorsOrWarns)
			{
				summary.Append("See the section below for error/warning list.").Push();
			}

			summary.Save();

			Trace.Info("<<< TfsPlugin.OnBuildFinished");

			FlushMessages();

			_build.FinalizeStatus(details.Status.AsTfsBuildStatus());
		}

		private string FormatMessage(TraceMessage message)
		{
			var formatted = new TfsMessageBuilder(_logsDropPath)
				.Append(message.ToString("mfd"))
				.AppendLinks(message.Links, "\n");

			_hasDropErrors |= formatted.HasDropErrors;

			return formatted.ToString();
		}		
	}
}