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
			Log.Error("", exception);
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
					Log.Error(message.ToString());
					break;

				case TraceMessageLevel.Warning:
					Log.Warn(message.ToString());
					break;

				default:
					Log.Debug(message.ToString());					
					break;
			}			
		}

		public static void TraceMessageFormat(TraceMessageLevel level, string format, params object[] args)
		{
			switch (level)
			{
				case TraceMessageLevel.Error:
					Log.ErrorFormat(format, args);
					break;

				case TraceMessageLevel.Warning:
					Log.WarnFormat(format, args);
					break;

				default:
					Log.DebugFormat(format, args);
					break;
			}
		}

		public static void TargetState(TargetState state, string message)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					Log.Info(message);
					break;
				case Core.TargetState.PartiallySucceeded:
					Log.Warn(message);
					break;
				case Core.TargetState.Failed:
					Log.Error(message);
					break;
			}
		}

		public static void TargetStateFormat(TargetState state, string format, params object[] args)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					Log.InfoFormat(format, args);
					break;
				case Core.TargetState.PartiallySucceeded:
					Log.WarnFormat(format, args);
					break;
				case Core.TargetState.Failed:
					Log.ErrorFormat(format, args);
					break;
			}
		}
	}
}