using System;

namespace AnFake.Api
{
	public sealed class BypassTracer : ITracer
	{
		private TraceMessageLevel _threshold = TraceMessageLevel.Info;

		public Uri Uri
		{
			get { return new Uri("urn:tracer:bypass"); }
		}

		public TraceMessageLevel Threshold
		{
			get { return _threshold; }
			set { _threshold = value; }
		}

		public void Write(TraceMessage message)
		{
			if (message == null)
				throw new ArgumentException("ITracer.Write(message): message must not be null");

			if (message.Level < _threshold)
				return;

			if (MessageReceiving != null)
			{
				MessageReceiving.Invoke(this, message);
			}

			if (MessageReceived != null)
			{
				MessageReceived.Invoke(this, message);
			}
		}

		public bool TrackExternal(Action externalStart, Func<TimeSpan, bool> externalWait, TimeSpan timeout)
		{
			throw new NotSupportedException("BypassTracer.TrackExternal() not supported.");
		}		

		public event EventHandler<TraceMessage> MessageReceiving;

		public event EventHandler<TraceMessage> MessageReceived;

		public event EventHandler Idle
		{
			add { }
			remove { }
		}
	}
}