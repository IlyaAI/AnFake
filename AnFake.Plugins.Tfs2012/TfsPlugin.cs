using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration;
using AnFake.Core.Integration.Tests;
using AnFake.Integration.Tfs2012;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AnFake.Plugins.Tfs2012
{
	internal sealed class TfsPlugin : Core.Integration.IVersionControl, Core.Integration.IBuildServer, IDisposable
	{
		private readonly static TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

		private const string CustomInformationNode = "AnFake";
		private const string SummaryKey = "AnFakeSummary";
		private const string SummaryHeader = "AnFake Summary";
		private const int SummaryPriority = 199;
		private const string OverviewKey = "AnFakeOverview";
		private const string OverviewHeader = "Overview";
		private const int OverviewPriority = 150;		
		
		private readonly Queue<TraceMessage> _messages = new Queue<TraceMessage>();
		private DateTime _lastFlushed;
		
		private readonly TfsTeamProjectCollection _teamProjectCollection;
		
		private readonly IBuildDetail _build;
		private readonly IBuildInformation _tracker;
		
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
			_teamProjectCollection.Connect(ConnectOptions.IncludeServices);

			if (!String.IsNullOrEmpty(buildUri))
			{
				_build = GetBuildByUri(new Uri(buildUri));
				
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

					Trace.MessageReceived += OnTraceMessage;
					Trace.Idle += OnTraceIdle;

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
			Trace.MessageReceived -= OnTraceMessage;
			Trace.Idle -= OnTraceIdle;

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

		public IBuildDetail GetBuildByUri(Uri uri)
		{
			var buildSvc = _teamProjectCollection.GetService<Microsoft.TeamFoundation.Build.Client.IBuildServer>();

			var build = buildSvc.QueryBuildsByUri(
				new[] { uri },
				new[] { "ActivityTracking", CustomInformationNode },
				QueryOptions.Definitions).Single();

			if (build == null)
				throw new InvalidConfigurationException(String.Format("TFS plugin unable to find build '{0}'", uri));

			return build;
		}

		public static string GetBuildCustomField(IBuildDetail build, string name, string defValue)
		{
			var node = build.Information
				.GetNodesByType(CustomInformationNode)
				.FirstOrDefault();

			if (node == null)
				return defValue;

			string value;
			return node.Fields.TryGetValue(name, out value)
				? value
				: defValue;			
		}

		public static void SetBuildCustomField(IBuildDetail build, string name, string value)
		{			
			var node = build.Information
				.GetNodesByType(CustomInformationNode)
				.FirstOrDefault();

			if (node == null)
			{
				node = build.Information.CreateNode();
				node.Type = CustomInformationNode;
			}

			node.Fields[name] = value;
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

		public bool CanExposeArtifacts
		{
			get
			{
				return
					_build != null
						? !String.IsNullOrEmpty(_build.DropLocation)
						: BuildServer.Local.CanExposeArtifacts;
			}
		}

		public Uri ExposeArtifact(FileItem file, ArtifactType type)
		{
			if (_build == null)
				return BuildServer.Local.ExposeArtifact(file, type);

			EnsureCanExpose();

			Trace.InfoFormat("TfsPlugin: Exposing file '{0}'...", file);
			
			var dstPath = _build.DropLocation.AsPath()/type.ToString()/file.Name;
			Files.Copy(file, dstPath);
			
			return new Uri(dstPath.Full);
		}

		public Uri ExposeArtifact(FolderItem folder, ArtifactType type)
		{
			if (_build == null)
				return BuildServer.Local.ExposeArtifact(folder, type);

			EnsureCanExpose();

			Trace.InfoFormat("TfsPlugin: Exposing folder '{0}'...", folder);

			var dstPath = _build.DropLocation.AsPath()/type.ToString()/folder.Name;
			Robocopy.Copy(folder.Path, dstPath, p => p.Recursion = Robocopy.RecursionMode.All);			

			return new Uri(dstPath.Full);
		}

		public Uri ExposeArtifact(string name, string content, Encoding encoding, ArtifactType type)
		{
			if (_build == null)
				return BuildServer.Local.ExposeArtifact(name, content, encoding, type);

			EnsureCanExpose();

			Trace.InfoFormat("TfsPlugin: Exposing text content '{0}'...", name);

			var dstFile = (_build.DropLocation.AsPath()/type.ToString()/name).AsFile();
			Text.WriteTo(dstFile, content, encoding);

			return new Uri(dstFile.Path.Full);
		}

		public void ExposeArtifacts(FileSet files, ArtifactType type)
		{
			if (_build == null)
			{
				BuildServer.Local.ExposeArtifacts(files, type);
				return;
			}

			EnsureCanExpose();

			Trace.InfoFormat("TfsPlugin: Exposing files {{{0}}}...", files.ToFormattedString());

			var dstPath = _build.DropLocation.AsPath()/type.ToString();
			Files.Copy(files, dstPath);
		}		

		private void EnsureCanExpose()
		{
			if (!CanExposeArtifacts)
				throw new InvalidConfigurationException("Unable to expose artifact: drop location isn't specified.");
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

		//
		// To prevent too active TFS accessing we save messages just once in FlushIntervalMs
		// See alse OnTraceIdle and FlushMessages
		//
		private void OnTraceMessage(object sender, TraceMessage message)
		{
			_messages.Enqueue(message);

			FlushMessagesIfNeccessary();
		}

		private void OnTraceIdle(object sender, EventArgs dummy)
		{
			FlushMessagesIfNeccessary();
		}

		private void FlushMessagesIfNeccessary()
		{
			var now = DateTime.UtcNow;
			if (now - _lastFlushed < FlushInterval)
				return;

			FlushMessages();

			_lastFlushed = now;
		}

		private void FlushMessages()
		{
			Log.DebugFormat("TfsPlugin: flushing {0} queued message(s)...", _messages.Count);

			if (_tracker == null)
			{
				_messages.Clear();
				return;
			}

			while (_messages.Count > 0)
			{
				var message = _messages.Dequeue();

				_tracker.TraceMessage(message);
			}

			_tracker.Save();			
		}

		private TfsBuildSummarySection GetOverviewSection()
		{
			return new TfsBuildSummarySection(_build, OverviewKey, OverviewHeader, OverviewPriority);
		}

		private TfsBuildSummarySection GetSummarySection()
		{
			return new TfsBuildSummarySection(_build, SummaryKey, SummaryHeader, SummaryPriority);
		}

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			Trace.Info(">>> TfsPlugin.OnBuildStarted");

			var rdpFile = String.Format("{0}.rdp", Environment.MachineName).AsFile();
			Text.WriteTo(rdpFile, String.Format("full address:s:{0}", Environment.MachineName));

			var rdpUri = CanExposeArtifacts
				? ExposeArtifact(rdpFile, ArtifactType.Other)
				: rdpFile.Path.ToUnc().ToUri();

			FlushMessages();

			var overview = GetOverviewSection();
			overview.Append("Sources Version: ").Append(_build.SourceGetVersion).Push();
			overview.Append("Build Agent: ").AppendLink(Environment.MachineName, rdpUri).Push();
			overview.Append("Build Folder: ").AppendLink(MyBuild.Current.Path.Full, MyBuild.Current.Path.ToUnc().ToUri()).Push();
			
			overview.Append("Drop Folder: ");
			if (!String.IsNullOrEmpty(_build.DropLocation))
			{
				overview.AppendLink(new Uri(_build.DropLocation));
			}
			else
			{
				overview.Append("(none)");				
			}
			overview
				.Push()
				.Save();						
			
			Trace.Info("<<< TfsPlugin.OnBuildStarted");
		}

		private void OnTestFailed(object sender, TestResult test)
		{
			if (String.IsNullOrEmpty(test.Output))
				return;

			if (!CanExposeArtifacts)
				return;

			try
			{
				var output = new StringBuilder(test.Output.Length + 512);
				output
					.Append('*', 48).Append(" TEST OUTPUT ").Append('*', 48).AppendLine()					
					.Append(test.Suite).Append('.').Append(test.Name).AppendLine()
					.Append('*', 109).AppendLine()
					.AppendLine()
					.Append(test.Output);

				var uri = ExposeArtifact(
					"Output".MakeUnique(".txt"),
					output.ToString(),
					Encoding.UTF8,
					ArtifactType.TestResults);

				test.Links.Add(new Hyperlink(uri, "Output"));
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
			
			Trace.Info("<<< TfsPlugin.OnTargetFinished");
		}		

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			Trace.Info(">>> TfsPlugin.OnBuildFinished");			

			var summary = GetSummarySection();
			
			if (!String.IsNullOrEmpty(_build.DropLocation))
			{
				// Visual Studio expects predefined name 'build.log' so we need to copy with new name.
				var buildLog = _build.DropLocation.AsPath() / ArtifactType.Logs.ToString() / "build.log";
				Files.Copy(MyBuild.Current.LogFile.Path, buildLog, true);
				
				summary.AppendLink("build.log", buildLog.ToUri()).Push();				
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
	}
}