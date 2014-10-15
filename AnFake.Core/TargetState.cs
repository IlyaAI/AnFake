namespace AnFake.Core
{
	internal enum TargetState
	{
		None,
		PreQueued,
		Queued,
		Started,
		Succeeded,
		PartiallySucceeded,
		Failed
	}
}