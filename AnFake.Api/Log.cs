using System;

namespace AnFake.Api
{
	/// <summary>
	///     Represent build log.
	/// </summary>
	public static class Log
	{
		private static ILogger _logger = new NoopLogger();

		private static ILogger Logger
		{
			get { return _logger; }
		}

		public static ILogger Set(ILogger logger)
		{
			if (logger == null)
				throw new ArgumentException("Log.Set(logger): logger must not be null.");

			var prevLogger = _logger;
			_logger = logger;

			return prevLogger;
		}

		public static void Message(LogMessageLevel level, string message)
		{
			Logger.Write(level, message);
		}

		public static void Debug(string message)
		{
			Message(LogMessageLevel.Debug, message);
		}

		public static void DebugFormat(string format, params object[] args)
		{
			Message(LogMessageLevel.Debug, String.Format(format, args));
		}

		public static void Info(string message)
		{
			Message(LogMessageLevel.Info, message);
		}

		public static void InfoFormat(string format, params object[] args)
		{
			Message(LogMessageLevel.Info, String.Format(format, args));
		}

		public static void Summary(string message)
		{
			Message(LogMessageLevel.Summary, message);
		}

		public static void SummaryFormat(string format, params object[] args)
		{
			Message(LogMessageLevel.Summary, String.Format(format, args));
		}

		public static void Warn(string message)
		{
			Message(LogMessageLevel.Warning, message);
		}

		public static void WarnFormat(string format, params object[] args)
		{
			Message(LogMessageLevel.Warning, String.Format(format, args));
		}

		public static void Error(string message)
		{
			Message(LogMessageLevel.Error, message);
		}

		public static void Error(Exception exception)
		{
			Message(LogMessageLevel.Error, exception.ToString());
		}

		public static void Error(string message, Exception exception)
		{
			Message(LogMessageLevel.Error, String.Format("{0}\n{1}", message, exception));
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			Message(LogMessageLevel.Error, String.Format(format, args));
		}

		public static void ErrorFormat(string format, Exception e, params object[] args)
		{
			Message(LogMessageLevel.Error, String.Format(format, args) + "\n" + e);
		}

		public static void Success(string message)
		{
			Message(LogMessageLevel.Success, message);
		}

		public static void SuccessFormat(string format, params object[] args)
		{
			Message(LogMessageLevel.Success, String.Format(format, args));
		}

		public static void Text(string message)
		{
			Message(LogMessageLevel.Text, message);
		}

		public static void TextFormat(string format, params object[] args)
		{
			Message(LogMessageLevel.Text, String.Format(format, args));
		}

		public static void Details(string message)
		{
			Message(LogMessageLevel.Details, message);
		}

		public static void TraceMessage(TraceMessage message)
		{
			Message((LogMessageLevel)message.Level, message.ToString());
		}

		public static void TraceMessage(TraceMessage message, string format)
		{
			Message((LogMessageLevel)message.Level, message.ToString(format));
		}

		public static void TraceMessageFormat(TraceMessageLevel level, string format, params object[] args)
		{
			Message((LogMessageLevel)level, String.Format(format, args));
		}
	}
}