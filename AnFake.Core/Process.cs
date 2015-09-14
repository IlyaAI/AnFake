using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using AnFake.Api;
using AnFake.Core.Exceptions;

namespace AnFake.Core
{
	/// <summary>
	///		Represents external process.
	/// </summary>
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

			public void Input(string line)
			{
				while (_buffer.Count >= _capacity)
					_buffer.Dequeue();

				_buffer.Enqueue(line);
			}

			public override string ToString()
			{
				return String.Join("\n", _buffer);
			}
		}

		private sealed class StringTranscoder
		{
			private readonly object _mutex;
			private readonly Encoding _inEncoding;
			private Encoding _outEncoding;

			public StringTranscoder(object mutex, Encoding inEncoding)
			{
				_mutex = mutex;
				_inEncoding = inEncoding;
			}

			public event Action<string> Output;

			public void Input(object sender, DataReceivedEventArgs evt)
			{
				var line = evt.Data;

				if (line == null)
					return;

				if (_outEncoding == null)
				{
					_outEncoding = DetectOutEncoding(ref line);					
				} 
				else if (!ReferenceEquals(_inEncoding, _outEncoding))
				{
					line = _outEncoding.GetString(_inEncoding.GetBytes(line));
				}

				if (Output != null && !String.IsNullOrWhiteSpace(line))
				{
					lock (_mutex)
					{
						Output.Invoke(line);
					}					
				}
			}			

			private Encoding DetectOutEncoding(ref string line)
			{
				var bytes = _inEncoding.GetBytes(line);

				if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
				{
					line = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
					return Encoding.UTF8;
				}

				return _inEncoding;
			}
		}

		/// <summary>
		///		Process run parameters.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Path to executable.
			/// </summary>
			public FileSystemPath FileName;

			/// <summary>
			///		Command line arguments.
			/// </summary>
			public string Arguments;

			/// <summary>
			///		Working directory.
			/// </summary>
			public FileSystemPath WorkingDirectory;

			/// <summary>
			///		Timeout.
			/// </summary>
			public TimeSpan Timeout;

			/// <summary>
			///		Whether track external messages via ITracer (true) or via std output (false).
			/// </summary>
			public bool TrackExternalMessages;

			/// <summary>
			///		Custom action called when line written in std output.
			/// </summary>
			public Action<string> OnStdOut;

			/// <summary>
			///		Custom action called when line written in std error.
			/// </summary>
			public Action<string> OnStdErr;

			/// <summary>
			///		Output buffer capacity.
			/// </summary>
			/// <remarks>
			///		Output buffer contains last n lines from stndard error/output.
			/// </remarks>
			public int OutputBufferCapacity;

			internal Params()
			{
				WorkingDirectory = "".AsPath();
				Timeout = TimeSpan.MaxValue;
				OutputBufferCapacity = 24; // lines
			}

			/// <summary>
			///		Clones Params structure.
			/// </summary>
			/// <returns></returns>
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
			
			var mutex = new Object();
			var encoding = Console.OutputEncoding;
			var outTranscoder = new StringTranscoder(mutex, encoding);
			var errTranscoder = new StringTranscoder(mutex, encoding);
			
			var process = new System.Diagnostics.Process
			{				
				StartInfo =
				{
					FileName = parameters.FileName.Full,
					WorkingDirectory = parameters.WorkingDirectory.Full,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					StandardOutputEncoding = encoding,
					StandardErrorEncoding = encoding
				}
			};

			process.OutputDataReceived += outTranscoder.Input;
			process.ErrorDataReceived += errTranscoder.Input;

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
				outTranscoder.Output += parameters.OnStdOut;
			}

			if (parameters.OnStdErr != null)
			{
				errTranscoder.Output += parameters.OnStdErr;
			}

			var outputBuffer = new OutputBuffer(parameters.OutputBufferCapacity);
			outTranscoder.Output += outputBuffer.Input;
			errTranscoder.Output += outputBuffer.Input;

			try
			{
				var processStart = new Action(() =>
				{
					process.Start();

					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
				});
				
				bool completed;
				if (parameters.TrackExternalMessages)
				{
					completed = Api.Trace.TrackExternal(processStart, process.Wait, parameters.Timeout);
				}
				else
				{
					processStart();

					completed = false;
					var totalTime = TimeSpan.Zero;
					var spinTime = TimeSpan.FromSeconds(1);

					while (!completed && totalTime < parameters.Timeout)
					{
						completed = process.Wait(spinTime);
						totalTime += spinTime;

						Interruption.CheckPoint();
					}					
				}

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
			catch (Interruption.BuildInterruptedException)
			{
				if (!process.HasExited)
				{
					process.Kill();
				}

				throw;
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