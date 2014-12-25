using System;
using System.Collections.Generic;
using System.Diagnostics;
using AnFake.Api;

namespace AnFake.Core
{
	public static class Process
	{
		private sealed class OutputBuffer
		{
			private readonly int _capacity;
			private readonly Queue<string> _buffer;

			public OutputBuffer(int capacity)
			{
				_capacity = capacity;
				_buffer = new Queue<string>(capacity);
			}

			public void OnDataReceived(object sender, DataReceivedEventArgs evt)
			{
				if (String.IsNullOrWhiteSpace(evt.Data))
					return;

				while (_buffer.Count >= _capacity)
					_buffer.Dequeue();

				_buffer.Enqueue(evt.Data);
			}

			public override string ToString()
			{
				return String.Join("\n", _buffer);
			}
		}

		public sealed class Params
		{
			public FileSystemPath FileName;
			public string Arguments;
			public FileSystemPath WorkingDirectory;
			public TimeSpan Timeout;
			public bool TrackExternalMessages;
			public Action<string> OnStdOut;
			public Action<string> OnStdErr;
			public int OutputBufferCapacity;

			internal Params()
			{
				WorkingDirectory = "".AsPath();
				Timeout = TimeSpan.MaxValue;
				OutputBufferCapacity = 48; // lines
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
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
				throw new ArgumentException("Process.Run(setParams): setParams must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.FileName == null)
				throw new ArgumentException("Process.Params.FileName: must not be null");
			if (parameters.WorkingDirectory == null)
				throw new ArgumentException("Process.Params.WorkingDirectory: must not be null");

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

			Api.Trace.InfoFormat("Starting process...\n  Executable: {0}\n  Arguments: {1}\n  WorkingDirectory: {2}",
				process.StartInfo.FileName, process.StartInfo.Arguments, process.StartInfo.WorkingDirectory);

			var external = new TraceMessageCounter();
			Api.Trace.MessageReceived += external.OnMessage;

			if (parameters.TrackExternalMessages)
			{
				Api.Trace.StartTrackExternal();
			}
			else
			{
				if (parameters.OnStdOut == null)				
					parameters.OnStdOut = Api.Trace.Debug;
				
				if (parameters.OnStdErr == null)				
					parameters.OnStdErr = Api.Trace.Error;				
			}

			if (parameters.OnStdOut != null)
			{
				process.OutputDataReceived += 
					(sender, evt) => { if (!String.IsNullOrWhiteSpace(evt.Data)) parameters.OnStdOut(evt.Data); };
			}

			if (parameters.OnStdErr != null)
			{
				process.ErrorDataReceived +=
					(sender, evt) => { if (!String.IsNullOrWhiteSpace(evt.Data)) parameters.OnStdErr(evt.Data); };
			}

			var outputBuffer = new OutputBuffer(parameters.OutputBufferCapacity);
			process.OutputDataReceived += outputBuffer.OnDataReceived;
			process.ErrorDataReceived += outputBuffer.OnDataReceived;

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
					throw new TimeoutException(String.Format("Process isn't completed in specified time.\n  Executable: {0}\n  Timeout: {1}", process.StartInfo.FileName,
						parameters.Timeout));
				}
			}
			finally
			{
				if (parameters.TrackExternalMessages)
				{
					Api.Trace.StopTrackExternal();
				}

				Api.Trace.MessageReceived -= external.OnMessage;
			}

			Api.Trace.InfoFormat("Process finished. ExitCode = {0} Errors = {1} Warnings = {2} Time = {3}",
				process.ExitCode, external.ErrorsCount, external.WarningsCount, process.ExitTime - process.StartTime);

			return new ProcessExecutionResult(
				process.ExitCode, 
				external.ErrorsCount, 
				external.WarningsCount,
				outputBuffer.ToString());
		}
	}
}