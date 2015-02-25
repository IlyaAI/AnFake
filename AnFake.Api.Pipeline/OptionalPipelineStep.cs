namespace AnFake.Api.Pipeline
{
	internal sealed class OptionalPipelineStep : PipelineStep
	{
		public readonly PipelineStep InnerStep;

		internal OptionalPipelineStep(PipelineStep innerStep)
		{
			InnerStep = innerStep;
		}

		public override void Prepare(Pipeline pipeline)
		{
			InnerStep.Prepare(pipeline);
		}

		public override PipelineStepStatus Step(Pipeline pipeline)
		{
			var status = InnerStep.Step(pipeline);

			return
				status == PipelineStepStatus.Failed
					? PipelineStepStatus.PartiallySucceeded
					: status;
		}
	}
}