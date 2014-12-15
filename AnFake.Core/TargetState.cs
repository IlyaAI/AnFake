namespace AnFake.Core
{
	public enum TargetState
	{
		/* Order is important. Do not change. */
		None,
		PreQueued,
		Queued,
		Started,
		Succeeded,
		PartiallySucceeded,
		Failed
	}
}