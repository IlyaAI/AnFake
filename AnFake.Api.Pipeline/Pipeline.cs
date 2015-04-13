using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace AnFake.Api.Pipeline
{
	public sealed class Pipeline
	{		
		private readonly IDictionary<string, IPipelineBuild> _aliases = new Dictionary<string, IPipelineBuild>();
		private readonly IPipelineImplementor _impl;
		private readonly PipelineStep _initialStep;

		private readonly List<IPipelineBuild> _triggeredBuilds = new List<IPipelineBuild>();
		private PipelineStepStatus _status;	

		public Pipeline(string pipelineDef, IPipelineImplementor impl)
		{
			if (String.IsNullOrEmpty(pipelineDef))
				throw new ArgumentException("Pipeline(pipelineDef, impl): pipelineDef must not be null or empty");

			if (impl == null)
				throw new ArgumentException("Pipeline(pipelineDef, impl): impl must not be null");

			Trace.Summary(pipelineDef);

			_initialStep = PipelineCompiler.Compile(pipelineDef);
			_impl = impl;

			_initialStep.Prepare(this);
		}

		internal Pipeline(PipelineStep initialStep, IPipelineImplementor impl)
		{
			_initialStep = initialStep;
			_impl = impl;

			_initialStep.Prepare(this);
		}

		public IEnumerable<IPipelineBuild> TriggeredBuilds
		{
			get { return _triggeredBuilds; }
		}

		public PipelineStepStatus Status
		{
			get { return _status; }
		}

		public PipelineStepStatus Step()
		{
			if (_status > PipelineStepStatus.InProgress)
				return _status;

			_status = _initialStep.Step(this);
			Debug.Assert(_status != PipelineStepStatus.None);

			return _status;
		}

		public PipelineStepStatus Run(TimeSpan spinTime, TimeSpan timeout, CancellationToken cancellationToken)
		{
			if (_status > PipelineStepStatus.InProgress)
				return _status;

			var startTime = DateTime.UtcNow;

			while (true)
			{
				var status = Step();
				if (status > PipelineStepStatus.InProgress)
					return status;

				if (DateTime.UtcNow - startTime > timeout)
					throw new TimeoutException(String.Format("Pipeline execution time has reached the limit {0}.", timeout));

				if (cancellationToken.IsCancellationRequested)
					throw new OperationCanceledException("Pipeline execution has been cancelled.");

				Thread.Sleep(spinTime);
			}
		}		
		
		internal IPipelineBuild GetBuild(string name)
		{
			return _impl.GetBuild(name);
		}

		internal PipelineStepStatus QueueBuild(IPipelineBuild build, IPipelineBuild input)
		{
			var status = _impl.QueueBuild(build, input);
			Debug.Assert(status != PipelineStepStatus.None);

			_triggeredBuilds.Add(build);

			return status;
		}

		internal IPipelineBuild ResolveAlias(string alias)
		{
			IPipelineBuild build;
			if (!_aliases.TryGetValue(alias, out build))
				throw new InvalidOperationException(String.Format("Build alias '{0}' is undefined.", alias));

			return build;
		}

		internal void RegisterAlias(IPipelineBuild build, string alias)
		{
			if (_aliases.ContainsKey(alias))
				throw new InvalidOperationException(String.Format("Build alias '{0}' is already defined.", alias));

			_aliases.Add(alias, build);
		}		
	}
}