using AnFake.Core;

namespace AnFake
{
	internal sealed class LoggerDebugWriter : LoggerWriter
	{
		protected override void LogMessage(string message)
		{
			Logger.Debug(message);
		}
	}
}