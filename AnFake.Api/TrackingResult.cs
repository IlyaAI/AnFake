namespace AnFake.Api
{
	internal sealed class TrackingResult : IToolExecutionResult
	{
		public TrackingResult()
		{
		}

		public TrackingResult(int errorsCount, int warningsCount)
		{
			ErrorsCount = errorsCount;
			WarningsCount = warningsCount;
		}

		public int ErrorsCount { get; private set; }

		public int WarningsCount { get; private set; }
	}
}