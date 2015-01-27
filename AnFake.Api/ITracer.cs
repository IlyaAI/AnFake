using System;

namespace AnFake.Api
{
	public interface ITracer
	{
		Uri Uri { get; }

		TraceMessageLevel Threshold { get; set; }		

		void Write(TraceMessage message);

		bool TrackExternal(Func<TimeSpan, bool> externalWait, TimeSpan timeout);

		event EventHandler<TraceMessage> MessageReceiving;

		event EventHandler<TraceMessage> MessageReceived;

		event EventHandler Idle;
	}
}