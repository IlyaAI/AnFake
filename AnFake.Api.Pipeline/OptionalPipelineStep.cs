namespace AnFake.Api.Pipeline
{
	public sealed class OptionalPipelineStep : PipelineStep
	{
		public readonly PipelineStep InnerStep;

		internal OptionalPipelineStep(PipelineStep innerStep)
		{
			InnerStep = innerStep;
		}

		public override PipelineStepStatus Step(IPipeline pipeline)
		{
			var status = InnerStep.Step(pipeline);

			return
				status == PipelineStepStatus.Failed
					? PipelineStepStatus.PartiallySucceeded
					: status;
		}
	}
}