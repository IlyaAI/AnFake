using AnFake.Api;
using Common.Logging;

namespace AnFake.Core
{
	internal static class LoggerExtension
	{
		public static void TraceMessage(this ILog log, TraceMessage message)
		{
			switch (message.Level)
			{
				case TraceMessageLevel.Error:
					log.Error(message.ToString());
					break;

				case TraceMessageLevel.Warning:
					log.Warn(message.ToString());
					break;

				case TraceMessageLevel.Summary:
					log.Info(message.ToString());
					break;

				default:
					log.Debug(message.ToString());
					break;
			}
		}

		public static void TraceMessageFormat(this ILog log, TraceMessageLevel level, string format, params object[] args)
		{
			switch (level)
			{
				case TraceMessageLevel.Error:
					log.ErrorFormat(format, args);
					break;

				case TraceMessageLevel.Warning:
					log.WarnFormat(format, args);
					break;

				case TraceMessageLevel.Summary:
					log.InfoFormat(format, args);
					break;

				default:
					log.DebugFormat(format, args);
					break;
			}
		}

		public static void TargetState(this ILog log, TargetState state, string message)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					log.Info(message);
					break;
				case Core.TargetState.PartiallySucceeded:
					log.Warn(message);
					break;
				case Core.TargetState.Failed:
					log.Error(message);
					break;
			}
		}

		public static void TargetStateFormat(this ILog log, TargetState state, string format, params object[] args)
		{
			switch (state)
			{
				case Core.TargetState.Succeeded:
					log.InfoFormat(format, args);
					break;
				case Core.TargetState.PartiallySucceeded:
					log.WarnFormat(format, args);
					break;
				case Core.TargetState.Failed:
					log.ErrorFormat(format, args);
					break;
			}
		}
	}
}