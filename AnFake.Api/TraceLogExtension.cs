using Common.Logging;

namespace AnFake.Api
{
	/// <summary>
	///     Extends ILog with TraceMessage method which maps TraceMessageLevel to regular ILog methods.
	/// </summary>
	public static class TraceLogExtension
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
				case TraceMessageLevel.Info:
					log.Debug(message.ToString());
					break;

				default:
					log.Trace(message.ToString());
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
				case TraceMessageLevel.Info:
					log.DebugFormat(format, args);
					break;

				default:
					log.TraceFormat(format, args);
					break;
			}
		}
	}
}