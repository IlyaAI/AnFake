using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using AnFake.Api;
using Microsoft.Build.Framework;

namespace AnFake.Core
{
	public static class MyBuild
	{
		public sealed class Params
		{
			public readonly IDictionary<string, string> Properties;
			public readonly FileSystemPath Path;
			public readonly FileItem LogFile;
			public readonly FileItem ScriptFile;
			public readonly string[] Targets;
			public ITracer Tracer;
			public LoggerVerbosity Verbosity;

			internal Params(FileSystemPath path, FileItem logFile, FileItem scriptFile,
				string[] targets, IDictionary<string, string> properties)
			{
				Path = path;
				LogFile = logFile;
				ScriptFile = scriptFile;
				Targets = targets;
				Properties = new ReadOnlyDictionary<string, string>(properties);
				Verbosity = LoggerVerbosity.Normal;

				ParseWellknownProperties(properties);

				Tracer = new JsonFileTracer("trace.jsx".AsPath().Full, false)
				{
					Threshold = Verbosity > LoggerVerbosity.Normal
						? TraceMessageLevel.Debug
						: TraceMessageLevel.Info
				};
			}

			private void ParseWellknownProperties(IDictionary<string, string> properties)
			{
				string value;

				if (properties.TryGetValue("Verbosity", out value))
				{
					Verbosity = (LoggerVerbosity) Enum.Parse(typeof (LoggerVerbosity), value, true);
					properties.Remove("Verbosity");
				}
			}
		}

		public static Params Defaults { get; private set; }

		private static bool _isInitialized;
		private static event EventHandler<Params> InitializedHandlers;

		internal static void Initialize(Params @params)
		{
			if (_isInitialized)
				throw new InvalidOperationException("MyBuild already initialized.");

			Debug.Assert(Path.IsPathRooted(@params.LogFile.Path.Spec), "LogFile must have absolute path.");
			Debug.Assert(Path.IsPathRooted(@params.ScriptFile.Path.Spec), "ScriptFile must have absolute path.");

			Defaults = @params;
			FileSystemPath.Base = @params.ScriptFile.Folder;
			Logger.LogFile = @params.LogFile;
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