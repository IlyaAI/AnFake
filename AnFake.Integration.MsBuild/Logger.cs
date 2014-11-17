using System;
using System.Text;
using AnFake.Api;
using Microsoft.Build.Framework;

namespace AnFake.Integration.MsBuild
{
	public sealed class Logger : ILogger
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
			Tracer.Instance = Tracer.Create(TracerUri);

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

			Trace(level, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}

		private void OnWarning(object sender, BuildWarningEventArgs e)
		{
			Trace(TraceMessageLevel.Warning, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}

		private void OnError(object sender, BuildErrorEventArgs e)
		{
			Trace(TraceMessageLevel.Error, e.ProjectFile, e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
		}

		private void Trace(TraceMessageLevel level, string project, string file, int line, int col, string code, string message)
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

			Tracer.Write(new TraceMessage(level, formattedMsg.ToString())
			{
				Code = code,
				File = file,
				Project = project,
				Line = line,
				Column = col,
				Target = AnFakeTarget
			});
		}
	}
}