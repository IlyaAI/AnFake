using System;
using System.IO;

namespace AnFake.Api
{
	/// <summary>
	///     Represent build trace - the main facility to report build progress, warnings, errors and so on.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Build trace is not just a text log. It is a persistent collection of typed message objects which can be
	///         processed by integration plugins for example to bypass to build server or produce nice HTML report.
	///     </para>
	///     <para>
	///         All message written to build trace are also duplicated to ILog which in turn attached to console and file.
	///     </para>
	/// </remarks>
	public static class Trace
	{
		private static ITracer _tracer = new NoopTracer();

		private static ITracer Tracer
		{
			get { return _tracer; }
		}

		public static ITracer Set(ITracer tracer)
		{
			if (tracer == null)
				throw new ArgumentException("Trace.Set(tracer): tracer must not be null.");

			var prevTracer = _tracer;
			_tracer = tracer;

			return prevTracer;
		}

		public static Uri Uri
		{
			get { return Tracer.Uri; }
		}

		public static void Message(TraceMessage message)
		{
			Tracer.Write(message);
			Log.TraceMessage(message);
		}

		public static void Debug(string message)
		{
			Message(new TraceMessage(TraceMessageLevel.Debug, message));
		}

		public static void DebugFormat(string format, params object[] args)
		{
			Message(new TraceMessage(TraceMessageLevel.Debug, String.Format(format, args)));
		}

		public static void Info(string message)
		{
			Message(new TraceMessage(TraceMessageLevel.Info, message));
		}

		public static void InfoFormat(string format, params object[] args)
		{
			Message(new TraceMessage(TraceMessageLevel.Info, String.Format(format, args)));
		}

		public static void Summary(string message)
		{
			Message(new TraceMessage(TraceMessageLevel.Summary, message));
		}

		public static void SummaryFormat(string format, params object[] args)
		{
			Message(new TraceMessage(TraceMessageLevel.Summary, String.Format(format, args)));
		}

		public static void Warn(string message)
		{
			Message(new TraceMessage(TraceMessageLevel.Warning, message));
		}

		public static void WarnFormat(string format, params object[] args)
		{
			Message(new TraceMessage(TraceMessageLevel.Warning, String.Format(format, args)));
		}

		public static void Error(string message)
		{
			Message(new TraceMessage(TraceMessageLevel.Error, message));
		}

		public static void Error(Exception exception)
		{
			Message(new TraceMessage(TraceMessageLevel.Error, exception.Message) {Details = exception.StackTrace});
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			Message(new TraceMessage(TraceMessageLevel.Error, String.Format(format, args)));
		}

		public static void ErrorFormat(string format, Exception e, params object[] args)
		{
			Message(new TraceMessage(TraceMessageLevel.Error, String.Format(format, args)) {Details = e.StackTrace});
		}

		public static void StartTrackExternal()
		{
			Tracer.MessageReceived += OnMessageReceived;
			try
			{
				Tracer.StartTrackExternal();
			}
			catch (Exception)
			{
				Tracer.MessageReceived -= OnMessageReceived;
				throw;
			}
		}

		public static IToolExecutionResult StopTrackExternal()
		{
			Tracer.MessageReceived -= OnMessageReceived;

			return Tracer.StopTrackExternal();
		}

		public static event EventHandler<TraceMessage> MessageReceiving
		{
			add { Tracer.MessageReceiving += value; }
			remove { Tracer.MessageReceiving -= value; }
		}

		public static event EventHandler<TraceMessage> MessageReceived
		{
			add { Tracer.MessageReceived += value; }
			remove { Tracer.MessageReceived -= value; }
		}

		public static ITracer NewTracer(Uri uri)
		{
			if (!uri.IsFile)
				throw new NotSupportedException("Only file based tracer supported now.");

			var logPath = uri.LocalPath.TrimEnd('/', '\\');
			if (!".jsx".Equals(Path.GetExtension(logPath), StringComparison.OrdinalIgnoreCase))
				throw new NotSupportedException("Only JSON file tracer supported now.");

			return new JsonFileTracer(logPath, true);
		}

		private static void OnMessageReceived(object sender, TraceMessage msg)
		{
			Log.TraceMessage(msg);
		}
	}
}