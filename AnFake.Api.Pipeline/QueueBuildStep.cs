using System;
using System.Collections.Generic;
using System.Linq;

namespace AnFake.Api.Pipeline
{
	internal sealed class QueueBuildStep : PipelineStep
	{
		private readonly List<string> _parameters = new List<string>();
		private readonly string _name;
		private readonly string _pipeIn;
		private readonly string _pipeOut;

		private IPipelineBuild _build;
		private PipelineStepStatus _status;
		private bool _idle;

		public QueueBuildStep(string name, string pipeIn, string pipeOut)
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

		public List<string> Parameters
		{
			get { return _parameters; }
		}

		public override void Prepare(Pipeline pipeline)
		{
			_build = pipeline.GetBuild(_name);
			_status = PipelineStepStatus.None;
			_idle = false;

			if (_pipeIn != null)
			{
				_build.EnsureInputSupported();
			}				

			if (_pipeOut != null)
			{
				_build.EnsureOutputSupported();

				pipeline.RegisterAlias(_build, _pipeOut);
			}
		}

		public override PipelineStepStatus Step(Pipeline pipeline)
		{
			if (_status > PipelineStepStatus.InProgress)
				return _status;

			var status = _build.Status;
			if (status != PipelineStepStatus.None)
			{
				if (status == _status && _idle)
					return status;

				if (status > PipelineStepStatus.InProgress)
				{
					var msg = new TraceMessage(
						status > PipelineStepStatus.InProgress && status <= PipelineStepStatus.Failed
							? TraceMessageLevel.Error
							: TraceMessageLevel.Summary,
						String.Format("'{0}' {1}", _build.Name, status.ToUpperHumanReadable()));
					msg.Links.Add(_build.Uri, _build.Name);

					Trace.Message(msg);
				}
				else if (!_idle)
				{
					var msg = new TraceMessage(
						TraceMessageLevel.Info,
						String.Format("Waiting for completion '{0}' [{1}]...", _build.Name, status.ToHumanReadable()));
					msg.Links.Add(_build.Uri, _build.Name);

					Trace.Message(msg);
				}

				_status = status;
				_idle = true;

				return _status;
			}

			var input = _pipeIn != null && _pipeIn != "_"
				? pipeline.ResolveAlias(_pipeIn)
				: null;

			if (input != null)
			{
				status = input.Status;
				if (status <= PipelineStepStatus.InProgress)
				{
					Trace.ErrorFormat("'{0}' references '{1}' which isn't yet completed. Hint: try sequential run operator '->'.", _name, input.Name);
					
					return (_status = PipelineStepStatus.Failed);
				}

				if (status == PipelineStepStatus.Failed)
				{
					Trace.ErrorFormat("'{0}' references '{1}' which has failed.", _name, input.Name);

					return (_status = PipelineStepStatus.Failed);
				}
			}

			Trace.InfoFormat("Queuing build '{0}'...", _build.Name);

			return (_status = pipeline.QueueBuild(_build, input, _parameters.ToArray()));
		}		
	}
}