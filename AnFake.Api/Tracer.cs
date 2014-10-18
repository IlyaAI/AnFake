using System;
using System.IO;

namespace AnFake.Api
{
	public static class Tracer
	{
		private static ITracer _instance;

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

		public static void StartTrackExternal()
		{
			Instance.StartTrackExternal();
		}

		public static IToolExecutionResult StopTrackExternal()
		{
			return Instance.StopTrackExternal();
		}

		public static event EventHandler<TraceMessage> MessageReceived
		{
			add { Instance.MessageReceived += value; }
			remove { Instance.MessageReceived -= value; }
		}

		public static ITracer Create(Uri uri, bool append)
		{
			if (!uri.IsFile)
				throw new NotSupportedException("Only file based tracer supported now.");

			var logPath = uri.LocalPath.TrimEnd('/', '\\');
			if (!".jsx".Equals(Path.GetExtension(logPath), StringComparison.InvariantCultureIgnoreCase))
				throw new NotSupportedException("Only JSON file tracer supported now.");

			return new JsonFileTracer(logPath, append);
		}
	}
}