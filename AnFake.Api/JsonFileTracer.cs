using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using Common.Logging;

namespace AnFake.Api
{
	public sealed class JsonFileTracer : ITracer
	{
		private static readonly ILog Log = LogManager.GetLogger<JsonFileTracer>();		

		private readonly string _logFile;
		private readonly XmlObjectSerializer _serializer;
		private TimeSpan _trackingInterval = TimeSpan.FromSeconds(15);
		private TimeSpan _retryInterval = TimeSpan.FromMilliseconds(10);
		private int _maxRetries = 10;
		private Thread _tracker;
		private int _externalErrors;
		private int _externalWarnings;

		public JsonFileTracer(string logFile, bool append)
		{
			if (String.IsNullOrEmpty(logFile))
				throw new ArgumentNullException("logFile");

			_logFile = InitLog(logFile, append);
			_serializer = InitSerializer();
		}

		public JsonFileTracer(Uri uri, bool append)
		{
			if (uri == null)
				throw new ArgumentNullException("uri");

			if (!uri.IsFile)
				throw new ArgumentException("JsonFileTracer supports file:// uri only.");

			_logFile = InitLog(uri.LocalPath, append);
			_serializer = InitSerializer();
		}		

		public Uri Uri
		{
			get { return new Uri("file://" + _logFile); }
		}

		public TimeSpan TrackingInterval
		{
			get { return _trackingInterval; }
			set { _trackingInterval = value; }
		}

		public TimeSpan RetryInterval
		{
			get { return _retryInterval; }
			set { _retryInterval = value; }
		}

		public int MaxRetries
		{
			get { return _maxRetries; }
			set { _maxRetries = value; }
		}

		public void Write(TraceMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message", "Tracer.Write(message): message must not be null");

			if (_tracker != null)
				throw new InvalidOperationException("Tracking of external messages is active. Hint: check parity of Start/StopTrackExternal methods.");

			if (MessageReceiving != null)
			{
				MessageReceiving.Invoke(this, message);
			}

			using (var log = OpenLog(FileMode.Append, FileAccess.Write, _maxRetries))
			{
				_serializer.WriteObject(log, message);

				log.WriteByte(0x0A);
			}

			if (MessageReceived != null)
			{
				MessageReceived.Invoke(this, message);
			}
		}		

		public void StartTrackExternal()
		{
			if (_tracker != null)
				throw new InvalidOperationException("Tracking of external messages activated already. Hint: check parity of Start/StopTrackExternal methods.");

			_externalErrors = 0;
			_externalWarnings = 0;

			long processedLength;
			using (var log = OpenLog(FileMode.Append, FileAccess.Write, _maxRetries))
			{
				processedLength = log.Length;
			}

			_tracker = new Thread(() =>
			{
				try
				{
					var sleepTime = _trackingInterval;
					var interrupted = false;
					while (!interrupted)
					{
						try
						{
							Thread.Sleep(sleepTime);
						}
						catch (ThreadInterruptedException)
						{
							interrupted = true;
						}

						var log = interrupted 
							? OpenLog(FileMode.Open, FileAccess.ReadWrite, MaxRetries)
							: TryOpenLog(FileMode.Open, FileAccess.ReadWrite);

						if (log == null)
						{
							sleepTime = _retryInterval;
							continue;
						}

						using (log)
						{
							if (processedLength >= log.Length)
								continue;							

							log.Position = processedLength;

							var reader = new JsonTraceReader(log);
							
							TraceMessage message;
							while ((message = reader.Read()) != null)
							{
								switch (message.Level)
								{
									case TraceMessageLevel.Warning:
										_externalWarnings++;
										break;

									case TraceMessageLevel.Error:
										_externalErrors++;
										break;
								}

								if (MessageReceived != null)
								{
									MessageReceived.Invoke(this, message);
								}								
							}							

							processedLength = log.Length;							
						}

						sleepTime = _trackingInterval;
					}
				}				
				catch (Exception e)
				{
					Log.Error("Tracking of external messages has failed. Some messages might be skipped.", e);					
				}				
			});		
	
			_tracker.Start();
		}

		public IToolExecutionResult StopTrackExternal()
		{
			if (_tracker == null)
				return new TrackingResult();

			_tracker.Interrupt();
			_tracker.Join(1000);
			_tracker = null;			

			return new TrackingResult(_externalErrors, _externalWarnings);
		}

		public event EventHandler<TraceMessage> MessageReceiving;

		public event EventHandler<TraceMessage> MessageReceived;

		private static XmlObjectSerializer InitSerializer()
		{
			return new DataContractJsonSerializer(
				typeof(TraceMessage),
				new DataContractJsonSerializerSettings { EmitTypeInformation = EmitTypeInformation.Always });
		}

		private static string InitLog(string logFile, bool append)
		{
			if (append || !File.Exists(logFile))
				return logFile;

			using (new FileStream(logFile, FileMode.Truncate, FileAccess.Write))
			{
			}

			return logFile;
		}

		private FileStream TryOpenLog(FileMode mode, FileAccess access)
		{
			try
			{
				return new FileStream(_logFile, mode, access);
			}
			catch (IOException e)
			{
				Log.WarnFormat("Unable to open log file. {0}", e.Message);
			}			

			return null;			
		}

		private FileStream OpenLog(FileMode mode, FileAccess access, int retries)
		{
			for (; retries > 0; retries--)
			{
				var log = TryOpenLog(mode, access);
				if (log != null)
					return log;

				Thread.Sleep(_retryInterval);
			}

			throw new IOException(String.Format("Log file is locked: {0}", _logFile));
		}
	}
}