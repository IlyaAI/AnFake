using System;
using AnFake.Api;
using AnFake.Core.Exceptions;
using Common.Logging;

namespace AnFake.Core
{
	public static class Process
	{
		private static readonly ILog Log = LogManager.GetLogger("AnFake.Process");

		public sealed class Params
		{
			public FileSystemPath FileName;
			public string Arguments;
			public FileSystemPath WorkingDirectory;
			public TimeSpan Timeout;
			public ILog Logger;			
			public bool TrackExternalMessages;

			internal Params()
			{
				WorkingDirectory = "".AsPath();
				Timeout = TimeSpan.MaxValue;
				Logger = Log;
			}

			public Params Clone()
			{
				return (Params)MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static Process()
		{
			Defaults = new Params();
		}

		public static ProcessExecutionResult Run(Action<Params> setParams)
		{
			if (setParams == null)
				throw new AnFakeArgumentException("Process.Run(setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.FileName == null)
				throw new AnFakeArgumentException("Process.Params.FileName: must not be null");
			if (parameters.WorkingDirectory == null)
				throw new AnFakeArgumentException("Process.Params.WorkingDirectory: must not be null");
			if (parameters.Logger == null)
				throw new AnFakeArgumentException("Process.Params.Logger: must not be null");

			var process = new System.Diagnostics.Process
			{
				StartInfo =
				{
					FileName = parameters.FileName.Full,
					WorkingDirectory = parameters.WorkingDirectory.Full,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};

			if (!String.IsNullOrEmpty(parameters.Arguments))
			{
				process.StartInfo.Arguments = parameters.Arguments;
			}

			Tracer.InfoFormat("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
			Log.DebugFormat("Starting process...\n  Executable: {0}\n  Arguments: {1}\n  WorkingDirectory: {2}", 
				process.StartInfo.FileName, process.StartInfo.Arguments, process.StartInfo.WorkingDirectory);

			var external = new TraceMessageCollector();
			Tracer.MessageReceived += external.OnMessage;

			if (parameters.TrackExternalMessages)
			{
				Tracer.MessageReceived += OnTraceMessage;
				Tracer.StartTrackExternal();
			}
			else
			{
				process.OutputDataReceived += (sender, evt) =>
				{
					if (String.IsNullOrWhiteSpace(evt.Data))
						return;
					
					Tracer.Debug(evt.Data);
					parameters.Logger.Debug(evt.Data);
				};
				process.ErrorDataReceived += (sender, evt) =>
				{
					if (String.IsNullOrWhiteSpace(evt.Data))
						return;

					Tracer.Error(evt.Data);
					parameters.Logger.Error(evt.Data);
				};
			}
			try
			{
				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				if (parameters.Timeout == TimeSpan.MaxValue)
				{
					process.WaitForExit();
				}
				else if (!process.WaitForExit((int) parameters.Timeout.TotalMilliseconds))
				{
					process.Kill();
					throw new TimeoutException(String.Format("Process isn't completed in specified time.\n  Executable: {0}\n  Timeout: {1}", process.StartInfo.FileName, parameters.Timeout));
				}
			}
			finally
			{
				if (parameters.TrackExternalMessages)
				{
					Tracer.StopTrackExternal();
					Tracer.MessageReceived -= OnTraceMessage;
				}

				Tracer.MessageReceived -= external.OnMessage;
			}

			Tracer.InfoFormat("Finished. ExitCode = {0}", process.ExitCode);
			Log.DebugFormat("Process finished. ExitCode = {0} Errors = {1} Warnings = {2} Time = {3}", 
				process.ExitCode, external.ErrorsCount, external.WarningsCount, process.ExitTime - process.StartTime);

			return new ProcessExecutionResult(process.ExitCode, external.ErrorsCount, external.WarningsCount);
		}		

		private static void OnTraceMessage(object sender, TraceMessage message)
		{
			Logger.TraceMessage(message);
		}
	}
}