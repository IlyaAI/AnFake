using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;

namespace AnFake.Core
{
	public static class MsBuild
	{
		private static readonly string[] Locations =
		{
			"[ProgramFilesX86]/MSBuild/12.0/Bin/MsBuild.exe",
			"[ProgramFilesX86]/MSBuild/11.0/Bin/MsBuild.exe",
			"[Windows]/Microsoft.NET/Framework/v4.0.30319/MsBuild.exe",
            "[Windows]/Microsoft.NET/Framework/v4.0.30128/MsBuild.exe"
		};

		public sealed class Params
		{
			public string[] Targets;
			public readonly IDictionary<string, string> Properties = new Dictionary<string, string>();
			public int? MaxCpuCount;
			public bool NodeReuse;
			public Verbosity Verbosity;
			public TimeSpan Timeout;
			public FileSystemPath ToolPath;
			public string ToolArguments;

			internal Params()
			{
				Targets = new[] {"Build"};
				Verbosity = Verbosity.Normal;
				Timeout = TimeSpan.MaxValue;
				ToolPath = Locations.AsFileSet().Select(x => x.Path).FirstOrDefault();
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

			MyBuild.Initialized += (s, p) =>
			{
				Defaults.Verbosity = p.Verbosity;

				foreach (var property in p.Properties)
				{
					if (!property.Key.StartsWith("MsBuild.", StringComparison.InvariantCulture))
						continue;					

					Defaults.Properties[property.Key.Substring(8)] = property.Value;
				}
			};
		}

		public static void BuildDebug(FileItem solution)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.BuildDebug(solution): solution must not be null");

			BuildDebug(new[] {solution});
		}

		public static void BuildDebug(IEnumerable<FileItem> projects)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.BuildDebug(projects): projects must not be null");

			Build(projects, p => { p.Properties["Configuration"] = "Debug"; });
		}

		public static void BuildDebug(IEnumerable<FileItem> projects, FileSystemPath output)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.BuildDebug(projects, output): projects must not be null");
			if (output == null)
				throw new ArgumentException("MsBuild.BuildDebug(projects, output): output must not be null");

			Build(projects, p =>
			{
				p.Properties["Configuration"] = "Debug";
				p.Properties["OutDir"] = output.Full;
			});
		}

		public static void BuildRelease(FileItem solution)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.BuildRelease(solution): solution must not be null");

			BuildRelease(new[] {solution});
		}

		public static void BuildRelease(IEnumerable<FileItem> projects)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.BuildRelease(projects): projects must not be null");

			Build(projects, p => { p.Properties["Configuration"] = "Release"; });
		}

		public static void BuildRelease(IEnumerable<FileItem> projects, FileSystemPath output)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.BuildRelease(projects, output): projects must not be null");
			if (output == null)
				throw new ArgumentException("MsBuild.BuildRelease(projects, output): output must not be null");

			Build(projects, p =>
			{
				p.Properties["Configuration"] = "Release";
				p.Properties["OutDir"] = output.Full;
			});
		}

		public static void Build(FileItem solution)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.Build(solution): solution must not be null");

			Build(new[] {solution}, p => { });
		}

		public static void Build(IEnumerable<FileItem> projects)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.Build(projects): projects must not be null");

			Build(projects, p => { });
		}

		public static void Build(FileItem solution, Action<Params> setParams)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.Build(solution, setParams): solution must not be null");
			if (setParams == null)
				throw new ArgumentException("MsBuild.Build(solution, setParams): setParams must not be null");

			Build(new[] {solution}, setParams);
		}

		public static void Build(IEnumerable<FileItem> projects, Action<Params> setParams)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.Build(projects, setParams): projects must not be null");
			if (setParams == null)
				throw new ArgumentException("MsBuild.Build(projects, setParams): setParams must not be null");

			var projArray = projects.ToArray();
			if (projArray.Length == 0)
				throw new ArgumentException("MsBuild.Build(projects, setParams): projects set must not be empty");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.ToolPath == null)
				throw new ArgumentException(
					String.Format(
						"MsBuild.Params.ToolPath must not be null.\nHint: probably, MsBuild.exe not found.\nSearch path:\n  {0}",
						String.Join("\n  ", Locations)));
			// TODO: check other parameters

			Trace.InfoFormat("MsBuild.Build\n => {0}", String.Join("\n => ", projArray.Select(x => x.RelPath)));
			
			foreach (var proj in projArray)
			{
				var args = new Args("/", ":")
					.Param(proj.Path.Full)
					.Option("t", parameters.Targets, ";")
					.Option("m", parameters.MaxCpuCount)
					.Option("nodeReuse", parameters.NodeReuse)
					.Option("v", parameters.Verbosity)
					.Other(parameters.ToolArguments);

				var propArgs = new Args("/", "=");
				foreach (var prop in parameters.Properties)
				{
					propArgs.Option(String.Format("p:{0}", prop.Key), prop.Value);
				}

				args.Space().NonQuotedValue(propArgs.ToString());

				var loggerT = typeof (AnFake.Integration.MsBuild.Logger);
				args.Space()
					.ValuedOption("logger")
					.NonQuotedValue(loggerT.FullName)
					.NonQuotedValue(",")
					.QuotedValue(loggerT.Assembly.Location)
					.NonQuotedValue(";")
					.QuotedValue(Trace.Uri + "#" + Target.Current.Name);

				var result = Process.Run(p =>
				{
					p.FileName = parameters.ToolPath;
					p.Timeout = parameters.Timeout;
					p.Arguments = args.ToString();					
					p.TrackExternalMessages = true;
				});

				result
					.FailIfAnyError("Target terminated due to MsBuild errors.")
					.FailIfExitCodeNonZero(
						String.Format("MsBuild failed with exit code {0}. Solution: {1}", result.ExitCode, proj));
			}
		}
	}
}