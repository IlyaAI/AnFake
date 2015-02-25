namespace AnFake.Api.Pipeline
{
	internal sealed class ParallelPipelineStep : PipelineStep
	{
		public readonly PipelineStep First;
		public readonly PipelineStep Second;

		internal ParallelPipelineStep(PipelineStep first, PipelineStep second)
		{
			First = first;
			Second = second;
		}

		public override void Prepare(Pipeline pipeline)
		{
			First.Prepare(pipeline);
			Second.Prepare(pipeline);
		}

		public override PipelineStepStatus Step(Pipeline pipeline)
		{
			var statusFirst = First.Step(pipeline);
			var statusSecond = Second.Step(pipeline);

			return statusFirst < statusSecond
				? statusFirst
				: statusSecond;
		}
	}
}