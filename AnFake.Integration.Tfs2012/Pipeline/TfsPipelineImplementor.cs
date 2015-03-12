using System.Linq;
using AnFake.Api.Pipeline;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Integration.Tfs2012.Pipeline
{
	internal sealed class TfsPipelineImplementor : IPipelineImplementor
	{
		private readonly IBuildDetail _currentBuild;		
		private readonly IBuildServer _buildSvc;
		
		public TfsPipelineImplementor(IBuildDetail currentBuild)
		{
			_currentBuild = currentBuild;			
			_buildSvc = currentBuild.BuildServer;
		}

		public IPipelineBuild GetBuild(string name)
		{
			var buildDef = _buildSvc.GetBuildDefinition(_currentBuild.TeamProject, name);
			
			return new TfsPipelineBuild(buildDef);
		}

		public PipelineStepStatus QueueBuild(IPipelineBuild build, IPipelineBuild input)
		{
			var requestedFor = (string) null;
			
			var requestForCurrent = _currentBuild.Requests.FirstOrDefault();
			if (requestForCurrent != null)
			{
				requestedFor = requestForCurrent.RequestedFor;
			}

			((TfsPipelineBuild)build).Queue(_buildSvc, _currentBuild.SourceGetVersion, requestedFor, input);

			return build.Status;
		}						
	}
}