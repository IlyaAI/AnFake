namespace AnFake.Api
{
	public interface IToolExecutionResult
	{
		int ErrorsCount { get; }

		int WarningsCount { get; }
	}
}