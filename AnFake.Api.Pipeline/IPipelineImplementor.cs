namespace AnFake.Api.Pipeline
{
	public interface IPipelineImplementor
	{
		IPipelineBuild GetBuild(string name);

		PipelineStepStatus QueueBuild(IPipelineBuild build, IPipelineBuild input, string[] @params);
	}
}