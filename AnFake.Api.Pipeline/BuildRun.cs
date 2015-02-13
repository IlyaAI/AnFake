using System.Collections.Generic;

namespace AnFake.Api.Pipeline
{
	public sealed class BuildRun : PipelineStep
	{
		private readonly IDictionary<string, object> _parameters = new Dictionary<string, object>();
		private readonly string _name;
		private readonly string _pipeIn;
		private readonly string _pipeOut;

		private object _instanceId;

		public BuildRun(string name, string pipeIn, string pipeOut)
		{
			_name = name;
			_pipeIn = pipeIn;
			_pipeOut = pipeOut;
		}

		public string Name
		{
			get { return _name; }
		}

		public string PipeIn
		{
			get { return _pipeIn; }
		}

		public string PipeOut
		{
			get { return _pipeOut; }
		}

		public IDictionary<string, object> Parameters
		{
			get { return _parameters; }
		}

		public object InstanceId
		{
			get { return _instanceId; }
			private set { _instanceId = value; }
		}

		public bool IsStarted
		{
			get { return _instanceId != null; }
		}

		public override PipelineStepStatus Step(IPipeline pipeline)
		{
			if (IsStarted)
				return pipeline.GetBuildStatus(InstanceId);

			InstanceId = pipeline.StartBuild(Name, PipeIn, PipeOut, Parameters);

			return PipelineStepStatus.InProgress;
		}
	}
}