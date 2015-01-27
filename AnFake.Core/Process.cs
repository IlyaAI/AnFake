using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using AnFake.Api;
using AnFake.Core.Exceptions;

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

		/// <summary>
		///		Process run parameters.
		/// </summary>
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
				OutputBufferCapacity = 24; // lines
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		/// <summary>
		///		Default process run parameters.
		/// </summary>
		public static Params Defaults { get; private set; }

		static Process()
		{
			Defaults = new Params();
		}

		/// <summary>
		///		Creates and starts new process with specified parameters.
		/// </summary>
		/// <remarks>
		///		The setParams action must provide at least FileName property.
		/// </remarks>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
		/// <returns><see cref="ProcessExecutionResult">ProcessExecutionResult</see></returns>
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

			if (!parameters.TrackExternalMessages)			
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
				
				var completed = parameters.TrackExternalMessages 
					? Api.Trace.TrackExternal(process.Wait, parameters.Timeout) 
					: process.Wait(parameters.Timeout);

				if (!completed)
				{
					process.Kill();
					throw new TimeoutException(
						String.Format(
							"Process isn't completed in specified time.\n  Executable: {0}\n  Timeout: {1}", 
							process.StartInfo.FileName,
							parameters.Timeout));
				}
			}
			catch (Win32Exception e)
			{
				if (e.NativeErrorCode == 2)
					throw new InvalidConfigurationException(String.Format("Unable to start process '{0}'. File not found.", parameters.FileName.Full));

				throw;
			}
			finally
			{
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

		private static bool Wait(this System.Diagnostics.Process process, TimeSpan timeout)
		{
			if (timeout != TimeSpan.MaxValue) 
				return process.WaitForExit((int) timeout.TotalMilliseconds);

			process.WaitForExit();
			return true;
		}
	}
}