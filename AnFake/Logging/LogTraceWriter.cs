using Common.Logging;

namespace AnFake.Logging
{
	internal sealed class LogTraceWriter : LogWriter
	{
		public LogTraceWriter(ILog log) : base(log)
		{
		}

		protected override void LogMessage(ILog log, string message)
		{
			log.Trace(message);
		}
	}
}