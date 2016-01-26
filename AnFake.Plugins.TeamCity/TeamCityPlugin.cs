using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration.Tests;
using JetBrains.TeamCity.ServiceMessages.Write;

namespace AnFake.Plugins.TeamCity
{
	internal sealed class TeamCityPlugin : Core.Integration.IBuildServer, IDisposable
	{
		private const string ArtifactsFolder = ".artifacts";

		private readonly IList<string> _summary = new List<string>();
		private readonly ServiceMessageFormatter _formatter;
		private readonly string _tcUri;
		private readonly string _tcBuildId;
		private readonly string _tcBuildTypeId;
		private int _errorsCount;
		private int _warningsCount;

		public TeamCityPlugin()
		{
			_formatter = new ServiceMessageFormatter();

			_tcUri = MyBuild.GetProp("TeamCity.Uri", null);
			_tcBuildId = MyBuild.GetProp("TeamCity.BuildId", null);
			_tcBuildTypeId = MyBuild.GetProp("TeamCity.BuildTypeId", null);

			if (_tcBuildId != null && _tcBuildTypeId == null || _tcBuildId == null && _tcBuildTypeId != null)
				throw new InvalidConfigurationException("TeamCity plugin requires both 'TeamCity.BuildId' and 'TeamCity.BuildTypeId' to be specified in build properties.");

			if (_tcBuildId != null && _tcUri == null)
				throw new InvalidConfigurationException("TeamCity plugin requires 'TeamCity.Uri' to be specified in build properties.");

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
				MyBuild.Current.DoNotExposeTestResults = true;
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

		public bool CanExposeArtifacts
		{
			get { return true; }
		}

		public Uri ExposeArtifact(FileItem file, ArtifactType type)
		{
			var dstPath = MakeArtifactPath(type, file.Name);
			Files.Copy(file, ArtifactsFolder.AsPath()/dstPath, true);

			return MakeArtifactUri(dstPath);
		}

		public Uri ExposeArtifact(FolderItem folder, ArtifactType type)
		{
			var dstPath = MakeArtifactPath(type, folder.Name);
			Robocopy.Copy(folder.Path, ArtifactsFolder.AsPath()/dstPath, p => p.Recursion = Robocopy.RecursionMode.All);

			return MakeArtifactUri(dstPath);
		}

		public Uri ExposeArtifact(string name, string content, Encoding encoding, ArtifactType type)
		{
			var dstPath = MakeArtifactPath(type, name);
			var dstFile = (ArtifactsFolder.AsPath()/dstPath).AsFile();
			Text.WriteTo(dstFile, content, encoding);

			return MakeArtifactUri(dstPath);
		}

		public void ExposeArtifacts(FileSet files, ArtifactType type)
		{
			Files.Copy(files, ArtifactsFolder.AsPath()/type.ToString(), true);
		}

		// Event Handlers

		private void OnBuildStarted(object sender, MyBuild.RunDetails details)
		{
			_errorsCount = 0;
			_warningsCount = 0;
		}

		private void OnBuildFinished(object sender, MyBuild.RunDetails details)
		{
			WriteBlockOpened("SUMMARY");
			foreach (var line in _summary)
			{
				WriteNormal(line);
			}
			WriteBlockClosed("SUMMARY");

			WriteBuildStatus(
				String.Format("{{build.status.text}} | {0} error(s) {1} warning(s) total", _errorsCount, _warningsCount),
				details.Status == MyBuild.Status.Succeeded || details.Status == MyBuild.Status.PartiallySucceeded);
		}

		private void OnTestSetProcessed(object sender, TestSet testSet)
		{
			WriteImportData(testSet.RunnerType.ToLowerInvariant(), testSet.TraceFile.Path.ToRelative(MyBuild.Current.Path).Spec);

			if (TeamCity.WaitAfterImport != TimeSpan.Zero)
			{
				// Give a little time to TeamCity to process trace file.
				// This is neccessary to prevent log cloging.
				Thread.Sleep(TeamCity.WaitAfterImport);
			}			
		}

		private void OnTargetPhaseStarted(object sender, string phase)
		{
			WriteBlockOpened(String.Format("{0}.{1}", ((Target) sender).Name, phase));
		}

		private void OnTargetPhaseFinished(object sender, string phase)
		{
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

				if (target.Messages.ErrorsCount > 0)
				{
					WriteError(msg, null);
					WriteBuildProblem(msg);
				}
				else if (target.Messages.WarningsCount > 0)
				{
					WriteWarning(msg);
				}
				else
				{
					WriteNormal(msg);
				}

				msg = String.Format(@"{0}: {1} error(s) {2} warning(s) {3} messages(s) {4:hh\:mm\:ss} {5}",
					target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount,
					target.RunTime, target.State.ToHumanReadable().ToUpperInvariant());
				_summary.Add(msg);

				foreach (var message in target.Messages.Where(x => x.Level == TraceMessageLevel.Summary))
				{
					_summary.Add(String.Format("    {0}", message.Message));
					foreach (var link in message.Links)
					{
						_summary.Add(
							String.Format("    {0} {1}", link.Label, link.Href));
					}
				}
			}

			_summary.Add(new String('=', 48));
			_summary.Add(
				String.Format(
					"'{0}' {1}",
					topTarget.Name,
					topTarget.State.ToHumanReadable().ToUpperInvariant()));

			WriteBlockClosed(topTarget.Name);
		}

		private void OnTraceMessage(object sender, TraceMessage message)
		{
			if (message.Category == TraceMessageCategory.TestTrace)
				return;

			switch (message.Level)
			{
				case TraceMessageLevel.Warning:
					WriteWarning(message.ToString("mlf"));
					break;

				case TraceMessageLevel.Error:
					WriteError("ERROR " + message.ToString("mlf"), message.Details);
					WriteBuildProblem(message.ToString("apd"));
					break;

				default:
					WriteNormal(message.ToString("mlfd"));
					break;
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

		private void WriteBlockOpened(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("blockOpened", new {name}));
		}

		private void WriteBlockClosed(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("blockClosed", new {name}));
		}

		private void WriteImportData(string type, string path)
		{
			Console.WriteLine(_formatter.FormatMessage("importData", new {type, path}));
		}

		private void WriteBuildProblem(string description)
		{
			Console.WriteLine(_formatter.FormatMessage("buildProblem", new {description}));
		}

		private void WriteBuildStatus(string text, bool succeeded)
		{
			Console.WriteLine(
				succeeded
					? _formatter.FormatMessage("buildStatus", new {text, status = "SUCCESS"})
					: _formatter.FormatMessage("buildStatus", new {text}));
		}

		private FileSystemPath MakeArtifactPath(ArtifactType type, string name)
		{
			return type.ToString().AsPath()/name;
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