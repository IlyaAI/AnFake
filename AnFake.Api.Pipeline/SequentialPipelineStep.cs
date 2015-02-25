namespace AnFake.Api.Pipeline
{
	internal sealed class SequentialPipelineStep : PipelineStep
	{
		public readonly PipelineStep First;
		public readonly PipelineStep Second;

		internal SequentialPipelineStep(PipelineStep first, PipelineStep second)
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
			if (statusFirst <= PipelineStepStatus.Failed)
				return statusFirst;
			
			var statusSecond = Second.Step(pipeline);			
			return statusFirst < statusSecond
				? statusFirst
				: statusSecond;
		}
	}
}