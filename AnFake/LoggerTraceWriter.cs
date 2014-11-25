using AnFake.Core;

namespace AnFake
{
	internal sealed class LoggerTraceWriter : LoggerWriter
	{
		protected override void LogMessage(string message)
		{
			Logger.Trace(message);
		}
	}
}