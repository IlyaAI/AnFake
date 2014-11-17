using AnFake.Core;

namespace AnFake
{
	internal sealed class LoggerErrorWriter : LoggerWriter
	{
		protected override void LogMessage(string message)
		{
			Logger.Error(message);
		}
	}
}