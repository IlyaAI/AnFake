namespace AnFake.Api
{
	public interface ILogger
	{
		LogMessageLevel Threshold { get; set; }

		void DisableConsoleEcho();

		void Write(LogMessageLevel level, string message);		
	}
}