using System;

namespace AnFake.Api
{
	public interface ITracer
	{
		Uri Uri { get; }

		TraceMessageLevel Threshold { get; set; }		

		void Write(TraceMessage message);

		void StartTrackExternal();

		void StopTrackExternal();

		event EventHandler<TraceMessage> MessageReceiving;

		event EventHandler<TraceMessage> MessageReceived;
	}
}