namespace AnFake.Api
{
	public interface ILogger
	{
		LogMessageLevel Threshold { get; set; }		

		void Write(LogMessageLevel level, string message);		
	}
}