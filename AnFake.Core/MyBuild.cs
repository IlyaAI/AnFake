using System;
using AnFake.Api;

namespace AnFake.Core
{
	public static class MyBuild
	{
		public sealed class Params
		{
			public ITracer Tracer;

			public Params()
			{
				Tracer = new JsonFileTracer("build.log.jsx".AsPath().Full, false);
			}
		}

		public static Params Defaults { get; private set; }

		static MyBuild()
		{
			Defaults = new Params();
		}

		public static void Configure()
		{
			Tracer.Instance = Defaults.Tracer;
		}

		public static void Info(string message)
		{
			Logger.Info(message);
			Tracer.Write(new TraceMessage(TraceMessageLevel.Info, message));
		}

		public static void InfoFormat(string format, params object[] args)
		{
			var message = String.Format(format, args);
			Logger.Info(message);
			Tracer.Write(new TraceMessage(TraceMessageLevel.Info, message));
		}

		public static void Warn(string message)
		{
			Logger.Warn(message);
			Tracer.Write(new TraceMessage(TraceMessageLevel.Warning, message));
		}

		public static void WarnFormat(string format, params object[] args)
		{
			var message = String.Format(format, args);
			Logger.Warn(message);
			Tracer.Write(new TraceMessage(TraceMessageLevel.Warning, message));
		}

		public static void Error(string message)
		{
			Logger.Error(message);
			Tracer.Write(new TraceMessage(TraceMessageLevel.Error, message));
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			var message = String.Format(format, args);
			Logger.Error(message);
			Tracer.Write(new TraceMessage(TraceMessageLevel.Error, message));
		}
	}
}