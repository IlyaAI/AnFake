using System;
using System.Text;
using AnFake.Api;
using Microsoft.Build.Framework;

namespace AnFake.Integration.MsBuild
{
	public sealed class Logger : ILogger
	{
		public LoggerVerbosity Verbosity { get; set; }

		public string Parameters { get; set; }

		public Uri TracerUri
		{
			get { return new Uri(Parameters); }
		}

		public void Initialize(IEventSource eventSource)
		{
			Tracer.Instance = Tracer.Create(TracerUri, true);

			eventSource.MessageRaised += OnMessage;
			eventSource.WarningRaised += OnWarning;
			eventSource.ErrorRaised += OnError;
		}

		public void Shutdown()
		{
		}

		private void OnMessage(object sender, BuildMessageEventArgs e)
		{
			switch (e.Importance)
			{
				case MessageImportance.Low:
					if (Verbosity < LoggerVerbosity.Diagnostic)
						return;
					break;
				case MessageImportance.Normal:
					if (Verbosity < LoggerVerbosity.Detailed)
						return;
					break;
				case MessageImportance.High:
					if (Verbosity < LoggerVerbosity.Normal)
						return;
					break;
			}

			Trace(TraceMessageLevel.Info, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}

		private void OnWarning(object sender, BuildWarningEventArgs e)
		{
			Trace(TraceMessageLevel.Warning, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}

		private void OnError(object sender, BuildErrorEventArgs e)
		{
			Trace(TraceMessageLevel.Error, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}

		private static void Trace(TraceMessageLevel level, string project, string file, int line, int col, string code, string message)
		{
			var formattedMsg = new StringBuilder();

			if (!String.IsNullOrEmpty(code))
			{
				formattedMsg.Append(code).Append(": ");
			}

			formattedMsg.AppendLine(message);

			if (!String.IsNullOrEmpty(file))
			{
				formattedMsg
					.Append("    ").Append(file).AppendFormat(" Ln: {0} Col: {1}", line, col).AppendLine();
			}

			if (!String.IsNullOrEmpty(project))
			{
				formattedMsg
					.Append("    ").AppendLine(project);
			}

			Tracer.Write(new TraceMessage(level, formattedMsg.ToString()));
		}
	}
}