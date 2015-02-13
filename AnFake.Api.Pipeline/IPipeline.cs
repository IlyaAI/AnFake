using System.Collections.Generic;

namespace AnFake.Api.Pipeline
{
	public interface IPipeline
	{
		PipelineStepStatus GetBuildStatus(object instanceId);

		object StartBuild(string buildName, string pipeIn, string pipeOut, IDictionary<string, object> parameters);
	}
}