using System;

namespace AnFake.Api
{
	public interface ITracer
	{
		Uri Uri { get; }

		void Write(TraceMessage message);

		void StartTrackExternal();

		IToolExecutionResult StopTrackExternal();

		event EventHandler<TraceMessage> MessageReceived;
	}
}