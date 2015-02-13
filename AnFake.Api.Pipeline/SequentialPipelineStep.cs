namespace AnFake.Api.Pipeline
{
	public sealed class SequentialPipelineStep : PipelineStep
	{
		public readonly PipelineStep First;
		public readonly PipelineStep Second;

		internal SequentialPipelineStep(PipelineStep first, PipelineStep second)
		{
			First = first;
			Second = second;
		}

		public override PipelineStepStatus Step(IPipeline pipeline)
		{
			var first = First.Step(pipeline);
			if (first == PipelineStepStatus.InProgress || first == PipelineStepStatus.Failed)
				return first;
			
			var second = Second.Step(pipeline);			
			return first < second
				? first
				: second;
		}
	}
}