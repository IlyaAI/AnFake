using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using JetBrains.TeamCity.ServiceMessages.Write;

namespace AnFake.Plugins.TeamCity
{
	internal sealed class TeamCityPlugin : /*Core.Integration.IBuildServer,*/ IDisposable
	{
		private readonly IList<string> _summary = new List<string>();
		private readonly ServiceMessageFormatter _formatter;
		private int _errorsCount;
		private int _warningsCount;

		public TeamCityPlugin()
		{
			_formatter = new ServiceMessageFormatter();

			var loggingEnabled = "true".Equals(MyBuild.GetProp("TeamCity.Logging", ""), StringComparison.OrdinalIgnoreCase);
			if (loggingEnabled)
			{
				Log.DisableConsoleEcho();
				Trace.MessageReceived += OnTraceMessage;

				Target.Started += OnTargetStarted;
				Target.Finished += OnTargetFinished;

				MyBuild.Started += OnBuildStarted;
				MyBuild.Finished += OnBuildFinished;
			}
		}

		// IDisposable members

		public void Dispose()
		{
			MyBuild.Finished -= OnBuildFinished;

			Target.Started -= OnTargetStarted;
			Target.Finished -= OnTargetFinished;

			Trace.MessageReceived -= OnTraceMessage;

			if (Disposed != null)
			{
				SafeOp.Try(Disposed);
			}
		}

		public event Action Disposed;

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
				String.Format("{{build.status.text}} {0} error(s) {1} warning(s)", _errorsCount, _warningsCount),
				details.Status == MyBuild.Status.Succeeded || details.Status == MyBuild.Status.PartiallySucceeded);
		}

		private void OnTargetStarted(object sender, Target.RunDetails details)
		{
			var topTarget = (Target) sender;
			WriteBlockOpened(topTarget.Name);
		}

		private void OnTargetFinished(object sender, Target.RunDetails details)
		{
			var topTarget = (Target) sender;

			foreach (var target in details.ExecutedTargets)
			{
				_errorsCount += target.Messages.ErrorsCount;
				_warningsCount += target.Messages.WarningsCount;

				var msg = String.Format(@"{0}: {1} error(s) {2} warning(s) {3} messages(s)  {4:hh\:mm\:ss}  {5}",
						target.Name, target.Messages.ErrorsCount, target.Messages.WarningsCount, target.Messages.SummariesCount,
						target.RunTime, target.State.ToHumanReadable().ToUpperInvariant());

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

				_summary.Add(new String('=', 48));
				_summary.Add(
					String.Format(
						"'{0}' {1}",
						topTarget.Name,
						topTarget.State.ToHumanReadable().ToUpperInvariant()));
			}

			WriteBlockClosed(topTarget.Name);
		}

		private void OnTraceMessage(object sender, TraceMessage message)
		{
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
			Console.WriteLine(_formatter.FormatMessage("blockOpened", new { name }));
		}

		private void WriteBlockClosed(string name)
		{
			Console.WriteLine(_formatter.FormatMessage("blockClosed", new { name }));
		}

		private void WriteBuildProblem(string description)
		{
			Console.WriteLine(_formatter.FormatMessage("buildProblem", new {description}));
		}

		private void WriteBuildStatus(string text, bool succeeded)
		{
			Console.WriteLine(
				succeeded 
					? _formatter.FormatMessage("buildStatus", new {text, status="SUCCESS"})
					: _formatter.FormatMessage("buildStatus", new {text}));
		}
	}
}