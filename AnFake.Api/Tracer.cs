using System;

namespace AnFake.Api
{
	public static class Tracer
	{
		private static ITracer _instance;

		public static ITracer Instance
		{
			get
			{
				if (_instance == null)
					throw new InvalidOperationException("Tracer not initialized. Hint: tracer should be used inside target definition only.");

				return _instance;
			}
			set
			{
				_instance = value;
			}
		}		

		public static Uri Uri
		{
			get { return Instance.Uri; }
		}

		public static void Write(TraceMessage message)
		{
			Instance.Write(message);
		}

		public static void StartTrackExternal()
		{
			Instance.StartTrackExternal();
		}

		public static IToolExecutionResult StopTrackExternal()
		{
			return Instance.StopTrackExternal();
		}

		public static event EventHandler<TraceMessage> MessageReceived
		{
			add { Instance.MessageReceived += value; }
			remove { Instance.MessageReceived -= value; }
		}		
	}
}