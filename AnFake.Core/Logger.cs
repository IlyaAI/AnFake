using System;
using AnFake.Api;
using Common.Logging;

namespace AnFake.Core
{
	public static class Logger
	{
		private static readonly ILog Log = LogManager.GetLogger("AnFake.Build");

		public static void Debug(object message)
		{
			Log.Debug(message);
		}

		public static void DebugFormat(string format, params object[] args)
		{
			Log.DebugFormat(format, args);
		}

		public static void Info(object message)
		{
			Log.Info(message);
		}

		public static void InfoFormat(string format, params object[] args)
		{
			Log.InfoFormat(format, args);
		}

		public static void Warn(object message)
		{
			Log.Warn(message);
		}

		public static void WarnFormat(string format, params object[] args)
		{
			Log.WarnFormat(format, args);
		}

		public static void Error(object message)
		{
			Log.Error(message);
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			Log.ErrorFormat(format, args);
		}

		public static void ErrorFormat(string format, Exception exception, params object[] args)
		{
			Log.ErrorFormat(format, exception, args);
		}

		[Obsolete("For test purpose to see a compilation warning")]
		public static void TraceMessage(TraceMessage message)
		{
			switch (message.Level)
			{
				case TraceMessageLevel.Error:
					Log.Error(message.Message);
					break;

				case TraceMessageLevel.Warning:
					Log.Warn(message.Message);
					break;

				case TraceMessageLevel.Info:
					Log.Debug(message.Message);					
					break;

				default:
					return;
			}

			if (!String.IsNullOrWhiteSpace(message.Details))
			{
				Log.Debug(message.Details);
			}
		}
	}
}