using System;
using System.Collections.Generic;
using AnFake.Api;
using Common.Logging;
using Microsoft.Build.Framework;

namespace AnFake.Core
{
	public static class MsBuild
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (MsBuild).FullName);

		public sealed class Params
		{
			public string[] Targets;
			public readonly IDictionary<string, string> Properties = new Dictionary<string, string>();
			public int? MaxCpuCount;
			public bool NodeReuse;
			public LoggerVerbosity Verbosity;
			public TimeSpan Timeout;
			public FileSystemPath ToolPath;

			public Params()
			{
				Targets = new[] {"Build"};
				Verbosity = LoggerVerbosity.Normal;
				Timeout = TimeSpan.MaxValue;
				ToolPath = "C:/Program Files (x86)/MSBuild/12.0/Bin/MsBuild.exe".AsPath();
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static MsBuild()
		{
			Defaults = new Params();
		}

		public static ProcessExecutionResult Build(FileItem solution)
		{
			return Build(solution, p => { });
		}

		public static ProcessExecutionResult Build(FileItem solution, Action<Params> setParams)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.Build(solution, setParams): solution must not be null");

			var parameters = Defaults.Clone();
			setParams(parameters);

			// TODO: check parameters

			Logger.DebugFormat("MsBuild =>  {0}", solution.RelPath);

			var args = Process.Args("/", ":")
				.Param(solution.Path.Full)
				.Option("t", parameters.Targets, ";")
				.Option("m", parameters.MaxCpuCount)
				.Option("nodeReuse", parameters.NodeReuse)
				.Option("v", parameters.Verbosity);

			var propArgs = Process.Args("/", "=");
			foreach (var prop in parameters.Properties)
			{
				propArgs.Option(String.Format("p:{0}", prop.Key), prop.Value);
			}

			args.Space().NonQuotedValue(propArgs.ToString());

			var loggerT = typeof (Integration.MsBuild.Logger);
			args.Space()
				.ValuedOption("logger")
				.NonQuotedValue(loggerT.FullName)
				.NonQuotedValue(",")
				.QuotedValue(loggerT.Assembly.Location)
				.NonQuotedValue(";")
				.QuotedValue(Tracer.Uri.ToString());

			var result = Process.Run(p =>
			{
				p.FileName = parameters.ToolPath;
				p.Timeout = parameters.Timeout;
				p.Arguments = args.ToString();
				p.Logger = Log;
			});

			result
				.FailIfExitCodeNonZero(String.Format("MsBuild failed with exit code {0}.\n  Solution: {1}", result.ExitCode, solution.Path))
				.FailIfAnyError("Target terminated due to MsBuild errors.");

			return result;
		}
	}
}