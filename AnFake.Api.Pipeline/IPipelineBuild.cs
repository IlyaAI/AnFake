using System;

namespace AnFake.Api.Pipeline
{
	public interface IPipelineBuild
	{
		Uri Uri { get; }

		string Name { get; }

		PipelineStepStatus Status { get; }

		TimeSpan WaitTime { get; }

		TimeSpan RunTime { get; }

		void EnsureInputSupported();

		void EnsureOutputSupported();
	}
}