namespace AnFake.Api.Pipeline
{
	public enum PipelineStepStatus
	{
		// ORDER IS IMPORTANT!
		// DO NOT CHANGE
		None,
		Queued,
		InProgress,		
		Failed,		
		PartiallySucceeded,
		Succeeded
	}
}