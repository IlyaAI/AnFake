namespace AnFake.Api
{
	public sealed class NoopLogger : ILogger
	{
		public LogMessageLevel Threshold
		{
			get { return LogMessageLevel.Debug; }
			set { }
		}

		public void Write(LogMessageLevel level, string message)
		{
		}
	}
}