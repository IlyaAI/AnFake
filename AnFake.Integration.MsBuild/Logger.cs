using System;
using AnFake.Api;
using Microsoft.Build.Framework;

namespace AnFake.Integration.MsBuild
{
	public sealed class Logger : Microsoft.Build.Framework.ILogger
	{
		private string _parameters;

		public LoggerVerbosity Verbosity { get; set; }

		public string Parameters
		{
			get { return _parameters; }
			set
			{
				if (_parameters == value)
					return;

				_parameters = value;

				if (!String.IsNullOrEmpty(value))
				{
					var components = value.Split('#');
					TracerUri = new Uri(components[0]);
					AnFakeTarget = components.Length > 1 ? components[1] : null;
				}
				else
				{
					TracerUri = null;
					AnFakeTarget = null;
				}
			}
		}

		public Uri TracerUri { get; private set; }

		public string AnFakeTarget { get; private set; }

		public void Initialize(IEventSource eventSource)
		{
			Trace.Set(Trace.NewTracer(TracerUri));

			eventSource.MessageRaised += OnMessage;
			eventSource.WarningRaised += OnWarning;
			eventSource.ErrorRaised += OnError;
		}

		public void Shutdown()
		{
		}

		private void OnMessage(object sender, BuildMessageEventArgs e)
		{
			var level = TraceMessageLevel.Debug;

			switch (e.Importance)
			{
				case MessageImportance.High:
					if (Verbosity < LoggerVerbosity.Normal)
						return;
					level = TraceMessageLevel.Info;
					break;

				case MessageImportance.Normal:
					if (Verbosity < LoggerVerbosity.Detailed)
						return;
					break;

				default:
					if (Verbosity < LoggerVerbosity.Diagnostic)
						return;
					break;
			}

			/* Mono specific: the commented properties doesn't exist under mono */
			TraceMessage(
				level,
				null,	// ProjectFile
				null,	// File
				0,		// ColumnNumber
				0,		// LineNumber
				null,	// Code
				e.Message,
				e.BuildEventContext != null ? e.BuildEventContext.NodeId : 0);			
		}

		private void OnWarning(object sender, BuildWarningEventArgs e)
		{			
			TraceMessage(
				TraceMessageLevel.Warning,
				e.ProjectFile,
				e.File,
				e.LineNumber,
				e.ColumnNumber,
				e.Code,
				e.Message,
				e.BuildEventContext != null ? e.BuildEventContext.NodeId : 0);
		}

		private void OnError(object sender, BuildErrorEventArgs e)
		{
			TraceMessage(
				TraceMessageLevel.Error, 
				e.ProjectFile, 
				e.File, 
				e.LineNumber, 
				e.ColumnNumber, 
				e.Code, 
				e.Message,
				e.BuildEventContext != null ? e.BuildEventContext.NodeId : 0);
		}

		private void TraceMessage(TraceMessageLevel level, string project, string file, int line, int col, string code, string message, int nodeId)
		{
			try
			{
				Trace.Message(new TraceMessage(level, message)
				{
					Code = code,
					File = file,
					Project = project,
					Line = line,
					Column = col,
					Target = AnFakeTarget,
					NodeId = nodeId
				});
			}
				// ReSharper disable once EmptyGeneralCatchClause
			catch (Exception)
			{
				// skip
			}
		}
	}
}