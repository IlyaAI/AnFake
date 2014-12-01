using System;

namespace AnFake.Api
{
	public sealed class NoopTracer : ITracer
	{
		public Uri Uri
		{
			get { return new Uri("urn:tracer:noop"); }
		}

		public TraceMessageLevel Threshold
		{
			get { return TraceMessageLevel.Debug; }
			set { }
		}

		public void Write(TraceMessage message)
		{			
		}

		public void StartTrackExternal()
		{
			throw new NotSupportedException("NoopTracer.StartTrackExternal() not supported.");
		}

		public IToolExecutionResult StopTrackExternal()
		{
			throw new NotSupportedException("NoopTracer.StopTrackExternal() not supported.");
		}

		public event EventHandler<TraceMessage> MessageReceiving
		{
			add
			{
				throw new NotSupportedException("NoopTracer.MessageReceiving not supported.");
			}
			remove
			{
				throw new NotSupportedException("NoopTracer.MessageReceiving not supported.");
			}
		}

		public event EventHandler<TraceMessage> MessageReceived
		{
			add
			{
				throw new NotSupportedException("NoopTracer.MessageReceived not supported.");
			}
			remove
			{
				throw new NotSupportedException("NoopTracer.MessageReceived not supported.");
			}
		}
	}
}