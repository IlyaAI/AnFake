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

		public void StartTrackExternal()
		{
			throw new NotSupportedException("BypassTracer.StartTrackExternal() not supported.");
		}

		public void StopTrackExternal()
		{
			throw new NotSupportedException("BypassTracer.StopTrackExternal() not supported.");
		}

		public event EventHandler<TraceMessage> MessageReceiving;

		public event EventHandler<TraceMessage> MessageReceived;
	}
}