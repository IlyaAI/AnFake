using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration.Builds;
using AnFake.Core.Integration.Tests;
using JetBrains.TeamCity.ServiceMessages.Write;

namespace AnFake.Plugins.TeamCity
{
	internal sealed class TeamCityPlugin : IBuildServer2, IDisposable
	{		
		private readonly Stack<string> _blockNames = new Stack<string>();
		private readonly ServiceMessageFormatter _formatter;
		private readonly Rest.TeamCityClient _apiClient;
		private readonly string _tcUri;
		private readonly string _tcBuildId;
		private readonly string _tcBuildTypeId;
		private readonly FileSystemPath _tcCheckoutFolder;		
		private readonly int _tcBuildCounter;
		private readonly string _tcBuildVcsNumber;
		private string _tcBuildNumber;
		private int _errorsCount;
		private int _warningsCount;
		private bool _skipErrors;
		private bool _failIfAnyWarning;

		public TeamCityPlugin()
		{
			_formatter = new ServiceMessageFormatter();

			_tcUri = MyBuild.GetProp("TeamCity.Uri", null);
			_tcBuildId = MyBuild.GetProp("TeamCity.BuildId", null);
			_tcBuildTypeId = MyBuild.GetProp("TeamCity.BuildTypeId", null);
			_tcCheckoutFolder = MyBuild.GetProp("TeamCity.CheckoutFolder", "").AsPath();			

			if (_tcBuildId != null && _tcBuildTypeId == null || _tcBuildId == null && _tcBuildTypeId != null)
				throw new InvalidConfigurationException("TeamCity plugin requires both 'TeamCity.BuildId' and 'TeamCity.BuildTypeId' to be specified in build properties.");

			if (_tcBuildId != null && _tcUri == null)
				throw new InvalidConfigurationException("TeamCity plugin requires 'TeamCity.Uri' to be specified in build properties.");

			if (!Int32.TryParse(MyBuild.GetProp("TeamCity.BuildCounter", "0"), out _tcBuildCounter))
				throw new InvalidConfigurationException("TeamCity plugin requires 'TeamCity.BuildCounter' property to be an integer.");

			if (_tcBuildCounter == 0 && _tcBuildId != null)
				throw new InvalidConfigurationException("TeamCity plugin requires 'TeamCity.BuildCounter' to be specified in build properties.");

			_tcBuildVcsNumber = MyBuild.GetProp("TeamCity.BuildVcsNumber", null);
			if (_tcBuildVcsNumber == null && _tcBuildId != null)
				throw new InvalidConfigurationException("TeamCity plugin requires 'TeamCity.BuildVcsNumber' to be specified in build properties.");

			_tcBuildNumber = MyBuild.GetProp("TeamCity.BuildNumber", null);
			if (_tcBuildNumber == null && _tcBuildId != null)
				throw new InvalidConfigurationException("TeamCity plugin requires 'TeamCity.BuildNumber' to be specified in build properties.");

			if (_tcUri != null)
			{				
				Log.DisableConsoleEcho();
				Trace.MessageReceived += OnTraceMessage;

				TestSetAware.Processed += OnTestSetProcessed;

				Target.TopStarted += OnTopTargetStarted;
				Target.TopFinished += OnTopTargetFinished;
				Target.PhaseStarted += OnTargetPhaseStarted;
				Target.PhaseFinished += OnTargetPhaseFinished;

				MyBuild.Started += OnBuildStarted;
				MyBuild.Finished += OnBuildFinished;

				if (MyBuild.HasProp("TeamCity.User"))
				{
					_apiClient = new Rest.TeamCityClient(new Uri(_tcUri), MyBuild.GetProp("TeamCity.User"), MyBuild.GetProp("TeamCity.Password"));
				}
			}
		}

		// IDisposable members

		public void Dispose()
		{
			MyBuild.Finished -= OnBuildFinished;

			Target.TopStarted -= OnTopTargetStarted;
			Target.TopFinished -= OnTopTargetFinished;
			Target.PhaseStarted -= OnTargetPhaseStarted;
			Target.PhaseFinished -= OnTargetPhaseFinished;

			TestSetAware.Processed -= OnTestSetProcessed;

			Trace.MessageReceived -= OnTraceMessage;

			if (_apiClient != null)
			{
				_apiClient.Dispose();
			}

			if (Disposed != null)
			{
				SafeOp.Try(Disposed);
			}
		}

		public event Action Disposed;

		// IBuildServer members

		public bool IsLocal
		{
			get { return _tcBuildId == null; }
		}

		public int CurrentChangesetId
		{
			get
			{
				if (_tcBuildVcsNumber != null)
				{
					int val;
					if (Int32.TryParse(_tcBuildVcsNumber, out val))
						return val;
				}
				else
				{
					if (_tcBuildId == null) // is local?
						return 0;
				}

				throw new InvalidConfigurationException("VCS doesn't provide integer changeset identifier. Use CurrentChangesetHash instead.");
			}
		}

		public string CurrentChangesetHash
		{
			get
			{
				if (_tcBuildVcsNumber != null)
					return _tcBuildVcsNumber;
				
				if (_tcBuildId == null) // is local?
					return "";

				throw new InvalidOperationException("Inconsistency detected: TeamCity plugin requires 'TeamCity.BuildVcsNumber' for non-local builds but it is undefined.");
			}
		}

		public int CurrentBuildCounter
		{
			get { return _tcBuildCounter; }
		}

		public string CurrentBuildNumber
		{
			get { return _tcBuildNumber; }
		}

		public string CurrentConfigurationName
		{
			get
			{
				if (String.IsNullOrEmpty(_tcBuildTypeId))
					throw new InvalidConfigurationException("Current configuration name isn't provided.");

				return _tcBuildTypeId;
			}			
		}

		public bool CanExposeArtifacts
		{
			get { return true; }
		}

		public Uri ExposeArtifact(FileItem file, string targetFolder)
		{
			if (IsLocal)
				return BuildServer.Local.ExposeArtifact(file, targetFolder);

			var srcPath = file.Path.ToRelative(_tcCheckoutFolder);
			var dstPath = targetFolder.AsPath();
			WritePublishArtifacts(srcPath.ToUnix(), dstPath.ToUnix());

			return MakeArtifactUri(dstPath / file.Name);
		}

		public Uri ExposeArtifact(FolderItem folder, string targetFolder)
		{
			if (IsLocal)
				return BuildServer.Local.ExposeArtifact(folder, targetFolder);

			var srcPath = folder.Path.ToRelative(_tcCheckoutFolder);
			var dstPath = targetFolder.AsPath();
			WritePublishArtifacts(srcPath.ToUnix(), dstPath.ToUnix());

			return MakeArtifactUri("".AsPath()); // TeamCity doesn't support links to artifacts folder
		}

		public Uri ExposeArtifact(string name, string content, Encoding encoding, string targetFolder)
		{
			if (IsLocal)
				return BuildServer.Local.ExposeArtifact(name, content, encoding, targetFolder);

			Folders.Create(MyBuild.Current.LocalTemp);
			var file = (MyBuild.Current.LocalTemp / name).AsFile();
			Text.WriteTo(file, content, encoding);

			var srcPath = file.Path.ToRelative(_tcCheckoutFolder);
			var dstPath = targetFolder.AsPath();
			WritePublishArtifacts(srcPath.ToUnix(), dstPath.ToUnix());

			return MakeArtifactUri(dstPath / file.Name);
		}

		public void ExposeArtifacts(FileSet files, string targetFolder)
		{
			if (IsLocal)
				return;

			var dstPath = targetFolder.AsPath();
			foreach (var file in files)
			{
				var srcPath = file.Path.ToRelative(_tcCheckoutFolder);				
				WritePublishArtifacts(srcPath.ToUnix(), (dstPath / file.RelPath.Parent).ToUnix());
			}
		}

		public void SetCurrentBuildNumber(string value)
		{
			_tcBuildNumber = value;
			WriteBuildNumber(value);
		}

		private void EnsureApiClient()
		{
			if (_apiClient == null)
				throw new InvalidConfigurationException("Extended build server interface isn't available. Hint: specify TeamCity.Uri, TeamCity.User and TeamCity.Password in build properties.");
		}

		public IBuild FindLastGoodBuild(string configurationName)
		{
			Trace.InfoFormat("Querying last good build of '{0}'...", configurationName);

			EnsureApiClient();
			
			var build = _apiClient.FindLastGoodBuild(configurationName);
			if (build != null)
			{
				Trace.InfoFormat("...found #{0}", build.Number);
				return new TeamCityBuild(_apiClient, build);
			}
			else
			{
				Trace.Info("...not found.");
				return null;
			}			
		}

		public IBuild FindLastTaggedBuild(string configurationName, string[] tags)
		{			
			Trace.InfoFormat("Querying last build of '{0}' tagged as '{1}'...", configurationName, String.Join(",", tags));

			EnsureApiClient();

			var build = _apiClient.FindLastTaggedBuild(configurationName, tags.Select(t => new Rest.Tag(t)).ToArray());
			if (build != null)
			{
				Trace.InfoFormat("...found #{0}", build.Number);
				return new TeamCityBuild(_apiClient, build);
			}
			else
			{
				Trace.Info("...not found.");
				return null;
			}
		}

		// Event Handlers

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			_errorsCount = 0;
			_warningsCount = 0;
		}

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			WriteBlockOpened("= SUMMARY =");
			WriteSummary(details.ExecutedTargets);
			WriteBlockClosed("= SUMMARY =");

			WriteBuildStatus(
				String.Format(
					"{0} error(s) {1} warning(s) {2} | {{build.status.text}}", 
					_errorsCount, _warningsCount, details.Status.ToHumanReadable().ToUpperInvariant()));
		}

		private void OnTestSetProcessed(object sender, TestSet testSet)
		{
			if (TeamCity.DoNotImportTestTrace)
			{
				WriteTestSuiteStarted(testSet.Name);

				foreach (var test in testSet.Tests)
				{
					var fullName = test.Suite + '.' + test.Name;
					if (test.Status == TestStatus.Skipped || test.Status == TestStatus.Unknown)
					{
						WriteTestIgnored(fullName, test.ErrorMessage);
					}
					else
					{
						WriteTestStarted(fullName);
						if (!String.IsNullOrWhiteSpace(test.Output))
						{
							WriteTestStdOut(fullName, test.Output);
						}
						if (test.Status == TestStatus.Failed)
						{
							WriteTestFailed(fullName, test.ErrorMessage, test.ErrorStackTrace);
						}
						WriteTestFinished(fullName, test.RunTime);
					}
				}

				WriteTestSuiteFinished(testSet.Name);
			}
			else
			{
				WriteImportData(testSet.RunnerType.ToLowerInvariant(), testSet.TraceFile.Path.ToRelative(_tcCheckoutFolder).Spec);

				if (TeamCity.WaitAfterTestTraceImport != TimeSpan.Zero)
				{
					// Give a little time to TeamCity to process trace file.
					// This is neccessary to prevent TeamCity's log cloging.
					Thread.Sleep(TeamCity.WaitAfterTestTraceImport);
				}
			}						
		}

		private void OnTargetPhaseStarted(object sender, string phase)
		{
			var target = (Target) sender;
			_skipErrors = target.IsSkipErrors;
			_failIfAnyWarning = target.IsFailIfAnyWarning;

			WriteBlockOpened(String.Format("{0}.{1}", target.Name, phase));
		}

		private void OnTargetPhaseFinished(object sender, string phase)
		{
			_skipErrors = false;
			_failIfAnyWarning = false;

			WriteBlockClosed(String.Format("{0}.{1}", ((Target) sender).Name, phase));
		}

		private void OnTopTargetStarted(object sender, Target.RunDetails details)
		{
			var topTarget = (Target) sender;
			WriteBlockOpened(topTarget.Name);
		}

		private void OnTopTargetFinished(object sender, Target.RunDetails details)
		{
			var topTarget = (Target) sender;

			foreach (var target in details.ExecutedTargets)
			{
				_errorsCount += target.Messages.ErrorsCount;
				_warningsCount += target.Messages.WarningsCount;

				// Intentially skipping run-time here to prevent TeamCity thinking this is new problem each time.
				var msg = String.Format(@"{0}: {1} error(s) {2} warning(s) {3} messages(s) {4}",
					target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount,
					target.State.ToHumanReadable().ToUpperInvariant());

				switch (target.State)
				{
					case TargetState.Failed:
						WriteError(msg, null);					
						WriteBuildProblem(msg);
						break;

					case TargetState.PartiallySucceeded:
						WriteWarning(msg);
						break;

					default:
						WriteNormal(msg);
						break;
				}								
			}			

			WriteBlockClosed(topTarget.Name);
		}

		private void OnTraceMessage(object sender, TraceMessage message)
		{
			if (message.Category == TraceMessageCategory.TestTrace)
				return;

			switch (message.Level)
			{
				case TraceMessageLevel.Warning:
					WriteWarning(message.ToString("nmlf"));
					if (_failIfAnyWarning && !_skipErrors)
					{
						WriteBuildProblem(message.ToString("ap"));
					}
					break;

				case TraceMessageLevel.Error:
					WriteError(message.ToString("nmlf"), message.Details);
					if (!_skipErrors)
					{
						WriteBuildProblem(message.ToString("apd"));
					}					
					break;

				default:
					WriteNormal(message.ToString("nmlfd"));
					break;
			}
		}

		[SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
		private void WriteSummary(IEnumerable<Target> executedTargets)
		{
			var index = 0;

			foreach (var target in executedTargets)
			{
				var msg = String.Format(@"{0}: {1} error(s) {2} warning(s) {3} messages(s) {4:hh\:mm\:ss} {5}",
					target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount,
					target.RunTime, target.State.ToHumanReadable().ToUpperInvariant());

				if (target.Messages.ErrorsCount > 0)
				{
					WriteError(msg, null);					
				}
				else if (target.Messages.WarningsCount > 0)
				{
					WriteWarning(msg);
				}
				else
				{
					WriteNormal(msg);
				}
								
				foreach (var message in target.Messages)
				{
					msg = String.Format("[{0,4}] {1}", ++index, message.ToString("apld"));					
					switch (message.Level)
					{
						case TraceMessageLevel.Error:
							WriteError(msg, null);
							break;
						case TraceMessageLevel.Warning:
							WriteWarning(msg);
							break;
						default:
							WriteNormal(msg);
							break;
					}
				}
			}
		}

		private void WriteNormal(string text)
		{
			Console.WriteLine(
				_formatter.FormatMessage("message", new
				{					
					status = "NORMAL",
					text
				}));
		}

		private void WriteWarning(string text)
		{
			Console.WriteLine(
				_formatter.FormatMessage("message", new
				{					
					status = "WARNING",
					text
				}));
		}

		private void WriteError(string text, string errorDetails)
		{
			Console.WriteLine(
				_formatter.FormatMessage("message", new
				{					
					status = "ERROR",
					text,
					errorDetails = errorDetails ?? string.Empty
				}));
		}

		public void WriteBlockOpened(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("blockOpened", new {name}));

			_blockNames.Push(name);

			Console.WriteLine(_formatter.FormatMessage("progressMessage", String.Join(" > ", _blockNames.Reverse())));
		}

		public void WriteBlockClosed(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("blockClosed", new {name}));

			_blockNames.Pop();
		}

		private void WriteImportData(string type, string path)
		{
			Console.WriteLine(_formatter.FormatMessage("importData", new {type, path}));
		}

		private void WritePublishArtifacts(string from, string to)
		{
			Console.WriteLine(
				_formatter.FormatMessage(
					"publishArtifacts", 
					String.Format("{0}=>{1}", from, to)
				)
			);
		}

		public void WriteTestSuiteStarted(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("testSuiteStarted", new { name }));
		}

		public void WriteTestSuiteFinished(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("testSuiteFinished", new { name }));
		}

		public void WriteTestStarted(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("testStarted", new { name }));
		}

		public void WriteTestFinished(string name, TimeSpan duration)
		{
			Console.WriteLine(_formatter.FormatMessage("testFinished", new { name, duration = (long)duration.TotalMilliseconds }));
		}

		public void WriteTestIgnored(string name, string message)
		{
			Console.WriteLine(_formatter.FormatMessage("testIgnored", new { name, message }));
		}

		public void WriteTestFailed(string name, string message, string details)
		{
			Console.WriteLine(_formatter.FormatMessage("testFailed", new { name, message, details }));
		}

		public void WriteTestStdOut(string name, string @out)
		{
			Console.WriteLine(_formatter.FormatMessage("testStdOut", new { name, @out }));
		}

		public void WriteTestStdErr(string name, string @out)
		{
			Console.WriteLine(_formatter.FormatMessage("testStdErr", new { name, @out }));
		}

		private void WriteBuildProblem(string description)
		{
			Console.WriteLine(_formatter.FormatMessage("buildProblem", new {description}));
		}

		private void WriteBuildStatus(string text)
		{
			Console.WriteLine(_formatter.FormatMessage("buildStatus", new {text}));
		}

		private void WriteBuildNumber(string value)
		{
			Console.WriteLine(_formatter.FormatMessage("buildNumber", value));
		}

		private Uri MakeArtifactUri(FileSystemPath path)
		{
			if (_tcBuildId == null)
				return path.ToUnc().ToUri();

			var uriBuilder = new UriBuilder(_tcUri);
			uriBuilder.Path += String.Format("repository/download/{0}/{1}:id/{2}", _tcBuildTypeId, _tcBuildId, path.ToUnix());

			return uriBuilder.Uri;
		}
	}
}