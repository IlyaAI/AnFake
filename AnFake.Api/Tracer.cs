using System;
using System.IO;

namespace AnFake.Api
{
	public static class Tracer
	{
		private static ITracer _instance;

		public static bool IsInitialized
		{
			get { return _instance != null; }
		}

		public static ITracer Instance
		{
			get
			{
				if (_instance == null)
					throw new InvalidOperationException("Tracer not initialized. Hint: tracer should be used inside target definition only.");

				return _instance;
			}
			set
			{
				_instance = value;
			}
		}		

		public static Uri Uri
		{
			get { return Instance.Uri; }
		}		

		public static void Write(TraceMessage message)
		{
			Instance.Write(message);
		}

		public static void Debug(string message)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Debug, message));
		}

		public static void DebugFormat(string format, params object[] args)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Debug, String.Format(format, args)));
		}

		public static void Info(string message)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Info, message));
		}

		public static void InfoFormat(string format, params object[] args)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Info, String.Format(format, args)));
		}

		public static void Warn(string message)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Warning, message));
		}

		public static void WarnFormat(string format, params object[] args)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Warning, String.Format(format, args)));
		}

		public static void Error(string message)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Error, message));
		}

		public static void Error(Exception exception)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Error, exception.Message) { Details = exception.StackTrace });
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Error, String.Format(format, args)));
		}

		public static void ErrorFormat(string format, Exception e, params object[] args)
		{
			Instance.Write(new TraceMessage(TraceMessageLevel.Error, String.Format(format, args)) { Details = e.StackTrace });
		}

		public static void StartTrackExternal()
		{
			Instance.StartTrackExternal();
		}

		public static IToolExecutionResult StopTrackExternal()
		{
			return Instance.StopTrackExternal();
		}

		public static event EventHandler<TraceMessage> MessageReceiving
		{
			add { Instance.MessageReceiving += value; }
			remove { Instance.MessageReceiving -= value; }
		}

		public static event EventHandler<TraceMessage> MessageReceived
		{
			add { Instance.MessageReceived += value; }
			remove { Instance.MessageReceived -= value; }
		}

		public static ITracer Create(Uri uri)
		{
			if (!uri.IsFile)
				throw new NotSupportedException("Only file based tracer supported now.");

			var logPath = uri.LocalPath.TrimEnd('/', '\\');
			if (!".jsx".Equals(Path.GetExtension(logPath), StringComparison.InvariantCultureIgnoreCase))
				throw new NotSupportedException("Only JSON file tracer supported now.");

			return new JsonFileTracer(logPath, true);
		}
	}
}