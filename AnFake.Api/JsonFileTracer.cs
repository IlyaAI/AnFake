using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;

namespace AnFake.Api
{
	public sealed class JsonFileTracer : ITracer
	{
		private readonly string _logFile;
		private readonly XmlObjectSerializer _serializer;
		private TraceMessageLevel _threshold = TraceMessageLevel.Info;
		private TimeSpan _trackingInterval = TimeSpan.FromSeconds(2);
		private TimeSpan _retryInterval = TimeSpan.FromMilliseconds(50);
		private int _maxRetries = 20;		
		
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

		public TraceMessageLevel Threshold
		{
			get { return _threshold; }
			set { _threshold = value; }
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
				throw new ArgumentException("ITracer.Write(message): message must not be null");			

			if (message.Level < _threshold)
				return;

			if (MessageReceiving != null)
			{
				MessageReceiving.Invoke(this, message);
			}

			var log = OpenLog(FileMode.Append, FileAccess.Write, _maxRetries);
			try
			{
				_serializer.WriteObject(log, message);

				log.WriteByte(0x0A);
			}
			finally
			{
				log.Close();

				if (MessageReceived != null)
				{
					MessageReceived.Invoke(this, message);
				}
			}
		}

		public bool TrackExternal(Action externalStart, Func<TimeSpan, bool> externalWait, TimeSpan timeout)
		{
			var processedLength = 0L;
			using (var log = OpenLog(FileMode.Append, FileAccess.Write, MaxRetries))
			{
				processedLength = log.Length;
			}

			externalStart();

			var totalTime = TimeSpan.Zero;
			var waitTime = _trackingInterval;
			var completed = false;
			while (!completed && totalTime < timeout)
			{
				completed = externalWait(waitTime);
				totalTime += waitTime;

				var log = completed
					? OpenLog(FileMode.Open, FileAccess.ReadWrite, MaxRetries)
					: TryOpenLog(FileMode.Open, FileAccess.ReadWrite);

				if (log == null)
				{
					waitTime = _retryInterval;
					continue;
				}

				var reader = new JsonTraceReader();
				using (log)
				{
					if (processedLength >= log.Length)
						continue;

					processedLength = reader.ReadFrom(log, processedLength);
				}

				if (MessageReceived != null)
				{
					TraceMessage message;
					while ((message = reader.Next()) != null)
					{
						if (message.Level >= _threshold)
						{
							MessageReceived.Invoke(this, message);
						}
					}
				}

				if (Idle != null)
				{
					Idle.Invoke(this, null);
				}

				waitTime = _trackingInterval;

				Interruption.CheckPoint();
			}

			return completed;
		}

		public event EventHandler<TraceMessage> MessageReceiving;

		public event EventHandler<TraceMessage> MessageReceived;

		public event EventHandler Idle;

		private static XmlObjectSerializer InitSerializer()
		{
			return new DataContractJsonSerializer(
				typeof(TraceMessage),
				new DataContractJsonSerializerSettings { EmitTypeInformation = EmitTypeInformation.AsNeeded });
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
			catch (IOException)
			{
				// just ignore				
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

				try
				{
					Thread.Sleep(_retryInterval);
				}
				catch (ThreadInterruptedException)
				{
					// ignore
				}				
			}

			throw new IOException(String.Format("Log file is locked: {0}", _logFile));
		}
	}
}