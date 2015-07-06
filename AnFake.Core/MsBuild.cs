using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;

namespace AnFake.Core
{
	/// <summary>
	///		Represents MSBuild tool.
	/// </summary>
	public static class MsBuild
	{
		private static readonly string[] MsLocations =
		{
			"[ProgramFilesX86]/MSBuild/12.0/Bin/MsBuild.exe",
			"[ProgramFilesX86]/MSBuild/11.0/Bin/MsBuild.exe",
			"[Windows]/Microsoft.NET/Framework/v4.0.30319/MsBuild.exe",
            "[Windows]/Microsoft.NET/Framework/v4.0.30128/MsBuild.exe"
		};

		private static readonly string[] MonoLocations =
		{
			"[MonoBin]/xbuild.bat",
			"[MonoBin]/xbuild"
		};

		private static readonly string[] Locations = Runtime.IsMono
			? MonoLocations
			: MsLocations;
			
		/// <summary>
		///		MSBuild parameters.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Requested targtes. Default 'Build'.
			/// </summary>
			public string[] Targets;

			/// <summary>
			///		Build properties. E.g. 'Configuration', 'Platform', etc.
			/// </summary>
			public readonly IDictionary<string, string> Properties = new Dictionary<string, string>();

			/// <summary>
			///		Max CPU count participated in build. Default equals to <c>Environment.ProcessorCount</c>.
			/// </summary>
			public int? MaxCpuCount;

			/// <summary>
			///		Node reuse flag.
			/// </summary>
			public bool NodeReuse;

			/// <summary>
			///		Output verbosity. Default Normal.
			/// </summary>
			public Verbosity Verbosity;

			/// <summary>
			///		Timeout.
			/// </summary>
			public TimeSpan Timeout;

			/// <summary>
			///		Path to MSBuild.exe.
			/// </summary>
			/// <remarks>
			///		Normally, ToolPath is evaluated automatically but you could provide specific one.
			/// </remarks>
			public FileSystemPath ToolPath;

			/// <summary>
			///		Additional command line arguments for MSBuild.exe
			/// </summary>
			/// <remarks>
			///		Additional arguments appended to command line as is be carefull with quotes and spaces.
			/// </remarks>
			public string ToolArguments;

			internal Params()
			{
				Targets = new[] {"Build"};
				Verbosity = Verbosity.Normal;
				MaxCpuCount = Environment.ProcessorCount;
				Timeout = TimeSpan.MaxValue;
				ToolPath = Locations.AsFileSet().Select(x => x.Path).FirstOrDefault();
			}

			/// <summary>
			///		Clones Params structure.
			/// </summary>
			/// <returns></returns>
			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		/// <summary>
		///		Default parameters.
		/// </summary>
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

		/// <summary>
		///		Builds solution with 'Configuration=Debug'.
		/// </summary>
		/// <param name="solution">solution to be built (not null)</param>
		public static void BuildDebug(FileItem solution)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.BuildDebug(solution): solution must not be null");

			BuildDebug(new[] {solution});
		}

		/// <summary>
		///		Builds set of projects with 'Configuration=Debug'.
		/// </summary>
		/// <param name="projects">projects to be built (not null)</param>
		public static void BuildDebug(IEnumerable<FileItem> projects)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.BuildDebug(projects): projects must not be null");

			Build(projects, p => { p.Properties["Configuration"] = "Debug"; });
		}

		/// <summary>
		///		Builds set of projects with 'Configuration=Debug' and 'OutDir=&lt;output&gt;'.
		/// </summary>
		/// <param name="projects">projects to be built (not null)</param>
		/// <param name="output">output path (not null)</param>
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

		/// <summary>
		///		Builds solution with 'Configuration=Release'.
		/// </summary>
		/// <param name="solution">solution to be built (not null)</param>
		public static void BuildRelease(FileItem solution)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.BuildRelease(solution): solution must not be null");

			BuildRelease(new[] {solution});
		}

		/// <summary>
		///		Builds set of projects with 'Configuration=Release'.
		/// </summary>
		/// <param name="projects">projects to be built (not null)</param>
		public static void BuildRelease(IEnumerable<FileItem> projects)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.BuildRelease(projects): projects must not be null");

			Build(projects, p => { p.Properties["Configuration"] = "Release"; });
		}

		/// <summary>
		///		Builds set of projects with 'Configuration=Release' and 'OutDir=&lt;output&gt;'.
		/// </summary>
		/// <param name="projects">projects to be built (not null)</param>
		/// <param name="output">output path (not null)</param>
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

		/// <summary>
		///		Builds solution with default parameters.
		/// </summary>
		/// <param name="solution">solution to be built (not null)</param>
		public static void Build(FileItem solution)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.Build(solution): solution must not be null");

			Build(new[] {solution}, p => { });
		}

		/// <summary>
		///		Builds set of projects with default parameters.
		/// </summary>
		/// <param name="projects">projects to be built (not null)</param>
		public static void Build(IEnumerable<FileItem> projects)
		{
			if (projects == null)
				throw new ArgumentException("MsBuild.Build(projects): projects must not be null");

			Build(projects, p => { });
		}

		/// <summary>
		///		Builds solution with overrided parameters.
		/// </summary>
		/// <param name="solution">solution to be built (not null)</param>
		/// <param name="setParams">action which override default parameters (not null)</param>
		public static void Build(FileItem solution, Action<Params> setParams)
		{
			if (solution == null)
				throw new ArgumentException("MsBuild.Build(solution, setParams): solution must not be null");
			if (setParams == null)
				throw new ArgumentException("MsBuild.Build(solution, setParams): setParams must not be null");

			Build(new[] {solution}, setParams);
		}

		/// <summary>
		///		Builds set of projects with overrided parameters.
		/// </summary>
		/// <param name="projects">projects to be built (not null)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
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
						String.Join("\n  ", MsLocations)));
			// TODO: check other parameters

			Trace.InfoFormat("MsBuild.Build\n => {0}", String.Join("\n => ", projArray.Select(x => x.RelPath)));
			
			foreach (var proj in projArray)
			{
				var args = new Args("/", ":")
					.Param(proj.Path.Full)
					.Option("t", parameters.Targets, ";")					
					.Option("nodeReuse", parameters.NodeReuse)
					.Option("v", parameters.Verbosity);

				if (!Runtime.IsMono)
				{
					args.Option("m", parameters.MaxCpuCount);
				}
					
				args.Other(parameters.ToolArguments);

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