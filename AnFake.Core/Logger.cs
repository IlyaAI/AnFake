using System;
using AnFake.Api;
using Common.Logging;

namespace AnFake.Core
{
	public static class Logger
	{
		private static readonly ILog Log = LogManager.GetLogger("AnFake.Build");		

		public static void Debug(string message)
		{			
			Log.Debug(message);
		}

		public static void DebugFormat(string format, params object[] args)
		{
			Log.DebugFormat(format, args);
		}

		public static void Info(string message)
		{
			Log.Info(message);
		}

		public static void InfoFormat(string format, params object[] args)
		{
			Log.InfoFormat(format, args);
		}

		public static void Warn(string message)
		{
			Log.Warn(message);
		}

		public static void WarnFormat(string format, params object[] args)
		{
			Log.WarnFormat(format, args);
		}

		public static void Error(string message)
		{
			Log.Error(message);
		}

		public static void Error(Exception exception)
		{
			Log.Error(exception);
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			Log.ErrorFormat(format, args);
		}

		public static void ErrorFormat(string format, Exception exception, params object[] args)
		{
			Log.ErrorFormat(format, exception, args);
		}
		
		public static void TraceMessage(TraceMessage message)
		{
			Log.TraceMessage(message);
		}

		public static void TraceMessageFormat(TraceMessageLevel level, string format, params object[] args)
		{
			Log.TraceMessageFormat(level, format, args);
		}

		public static void TargetState(TargetState state, string message)
		{
			Log.TargetState(state, message);
		}

		public static void TargetStateFormat(TargetState state, string format, params object[] args)
		{
			Log.TargetStateFormat(state, format, args);
		}
	}
}