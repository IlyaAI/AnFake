using Common.Logging;

namespace AnFake.Logging
{
	internal sealed class LogErrorWriter : LogWriter
	{
		public LogErrorWriter(ILog log) : base(log)
		{
		}

		protected override void LogMessage(ILog log, string message)
		{
			log.Error(message);
		}
	}
}