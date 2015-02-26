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

		public bool TrackExternal(Action externalStart, Func<TimeSpan, bool> externalWait, TimeSpan timeout)
		{
			throw new NotSupportedException("NoopTracer.TrackExternal() not supported.");
		}
		
		public event EventHandler<TraceMessage> MessageReceiving
		{
			add
			{				
			}
			remove
			{			
			}
		}

		public event EventHandler<TraceMessage> MessageReceived
		{
			add
			{			
			}
			remove
			{			
			}
		}

		public event EventHandler Idle
		{
			add
			{
			}
			remove
			{
			}
		}
	}
}