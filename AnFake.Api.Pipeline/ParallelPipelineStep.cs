namespace AnFake.Api.Pipeline
{
	public sealed class ParallelPipelineStep : PipelineStep
	{
		public readonly PipelineStep First;
		public readonly PipelineStep Second;

		internal ParallelPipelineStep(PipelineStep first, PipelineStep second)
		{
			First = first;
			Second = second;
		}

		public override PipelineStepStatus Step(IPipeline pipeline)
		{
			var first = First.Step(pipeline);
			var second = Second.Step(pipeline);

			return first < second
				? first
				: second;
		}
	}
}