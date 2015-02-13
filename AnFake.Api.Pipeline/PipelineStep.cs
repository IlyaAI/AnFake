namespace AnFake.Api.Pipeline
{
	public abstract class PipelineStep
	{
		public abstract PipelineStepStatus Step(IPipeline pipeline);
	}
}