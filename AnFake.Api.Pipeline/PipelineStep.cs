namespace AnFake.Api.Pipeline
{
	internal abstract class PipelineStep
	{
		public abstract void Prepare(Pipeline pipeline);

		public abstract PipelineStepStatus Step(Pipeline pipeline);
	}
}