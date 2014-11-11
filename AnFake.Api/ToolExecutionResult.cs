namespace AnFake.Api
{
	public sealed class ToolExecutionResult : IToolExecutionResult
	{
		public ToolExecutionResult()
		{
		}

		public ToolExecutionResult(int errorsCount, int warningsCount)
		{
			ErrorsCount = errorsCount;
			WarningsCount = warningsCount;
		}

		public int ErrorsCount { get; private set; }

		public int WarningsCount { get; private set; }

		public static ToolExecutionResult operator +(ToolExecutionResult a, IToolExecutionResult b)
		{
			return new ToolExecutionResult(a.ErrorsCount + b.ErrorsCount, a.WarningsCount + b.WarningsCount);
		}
	}
}