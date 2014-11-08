using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AnFake.Api;

namespace AnFake.Core
{
	public static class MyBuild
	{
		public sealed class Params
		{
			public readonly IDictionary<string, string> Properties;
			public readonly string[] Targets;
			public readonly FileItem Script;
			public ITracer Tracer;

			public Params(IDictionary<string, string> properties, string[] targets, FileItem script)
			{
				Properties = new ReadOnlyDictionary<string, string>(properties);
				Targets = targets;
				Script = script;

				Tracer = new JsonFileTracer("build.log.jsx".AsPath().Full, false);
			}
		}

		public static Params Defaults { get; private set; }		

		private static bool _isInitialized;
		private static event EventHandler<Params> InitializedHandlers;

		internal static void Initialize(FileSystemPath basePath, Params @params)
		{
			if (_isInitialized)
				throw new InvalidOperationException("MyBuild already initialized.");

			Defaults = @params;
			FileSystemPath.Base = basePath;			
			Tracer.Instance = Defaults.Tracer;

			_isInitialized = true;
			if (InitializedHandlers != null)
			{
				InitializedHandlers.Invoke(null, Defaults);
			}
		}

		public static event EventHandler<Params> Initialized
		{
			add
			{
				InitializedHandlers += value;
				if (_isInitialized)
				{
					value.Invoke(null, Defaults);
				}
			}
			remove { InitializedHandlers -= value; }
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