using System;
using System.Collections.Generic;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Tests;

namespace AnFake.Core
{
	/// <summary>
	///     Represents 'VSTest.Console' test runner.
	/// </summary>
	/// <remarks>
	///     Starting from VisualStudio 2012 VSTest.Console positioned as replacement of MSTest.
	/// </remarks>
	public static class VsTest
	{
		private static readonly string[] Locations =
		{
			"[ProgramFilesX86]/Microsoft Visual Studio 12.0/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"
		};

		public sealed class Params
		{
			public string TestCaseFilter;
			public string Platform;
			public string Framework;
			public bool EnableCodeCoverage;
			public bool InIsolation;
			public FileSystemPath SettingsPath;
			public FileSystemPath TestAdapterPath;
			public FileSystemPath WorkingDirectory;
			public TimeSpan Timeout;
			public ITestPostProcessor PostProcessor;
			public FileSystemPath ToolPath;
			public string ToolArguments;

			internal Params()
			{
				Timeout = TimeSpan.MaxValue;
				PostProcessor = new MsTestPostProcessor();
				ToolPath = Locations.AsFileSet().Select(x => x.Path).FirstOrDefault();
			}

			public Params Clone()
			{
				return (Params) MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static VsTest()
		{
			Defaults = new Params();
		}

		public static IEnumerable<TestResult> Run(IEnumerable<FileItem> assemblies)
		{
			return Run(assemblies, p => { });
		}

		public static IEnumerable<TestResult> Run(IEnumerable<FileItem> assemblies, Action<Params> setParams)
		{
			var assembliesToRun = assemblies.ToArray();
			if (assembliesToRun.Length == 0)
				throw new ArgumentException("VsTest.Run(assemblies[, setParams]): assemblies must not be an empty list");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.ToolPath == null)
				throw new ArgumentException(
					String.Format(
						"VsTest.Params.ToolPath must not be null.\nHint: probably, VsTest.Console.exe not found.\nSearch path:\n  {0}",
						String.Join("\n  ", Locations)));			

			var resultPath = "TestResults".AsPath();
			if (parameters.SettingsPath != null && ".runsettings".Equals(parameters.SettingsPath.Ext, StringComparison.OrdinalIgnoreCase))
			{
				var resultsDirectory = parameters.SettingsPath.AsXmlDoc()
					.ValueOf("RunSettings/RunConfiguration/ResultsDirectory");

				if (!String.IsNullOrEmpty(resultsDirectory))
				{
					resultPath = resultsDirectory.AsPath();
				}
			}

			Trace.InfoFormat("VsTest.Run\n => {0}", String.Join("\n => ", assembliesToRun.Select(x => x.RelPath)));			

			var tests = new List<TestResult>();

			foreach (var assembly in assembliesToRun)
			{
				Trace.DebugFormat("Running {0}...", assembly);

				var workDir = parameters.WorkingDirectory ?? assembly.Folder;
				var args = new Args("/", ":")
					.Param(assembly.Path.Full)
					.Option("TestCaseFilter", parameters.TestCaseFilter)					
					.Option("Platform", parameters.Platform)
					.Option("Framework", parameters.Framework)
					.Option("EnableCodeCoverage", parameters.EnableCodeCoverage)
					.Option("InIsolation", parameters.InIsolation)
					.Option("Settings", parameters.SettingsPath)
					.Option("TestAdapterPath", parameters.TestAdapterPath)
					.Option("Logger", "Trx")
					.Other(parameters.ToolArguments)
					.ToString();

				var stderr = new List<string>();

				var startTime = DateTime.UtcNow;
				Process.Run(p =>
				{
					p.FileName = parameters.ToolPath;
					p.WorkingDirectory = workDir;
					p.Timeout = parameters.Timeout;
					p.Arguments = args;
					p.OnStdErr = stderr.Add;
				});
				var endTime = DateTime.UtcNow;

				var processed = false;
				if (parameters.PostProcessor != null)
				{
					var trxPath = FindDotTrx(workDir/resultPath, startTime, endTime);
					if (trxPath != null)
					{
						var currentTests = parameters.PostProcessor
							.PostProcess(trxPath)
							.Trace()
							.ToArray();

						var summary = String.Format("{0}: {1} passed / {2} total tests",
							assembly.Name,
							currentTests.Count(x => x.Status == TestStatus.Passed),
							currentTests.Length);

						Trace.Message(
							new TraceMessage(TraceMessageLevel.Summary, summary)
							{
								LinkLabel = "Trace",
								LinkHref = trxPath.Full
							});

						tests.AddRange(currentTests);
						processed = true;
					}					
				}

				if (!processed)				
				{
					stderr.ForEach(Trace.Error);
				}
			}

			tests.TraceSummary();

			return tests;
		}

		private static FileSystemPath FindDotTrx(FileSystemPath path, DateTime start, DateTime end)
		{
			Trace.DebugFormat("Looking for results in '{0}'", path);

			var candidates = "*.trx".AsFileSetFrom(path)
				.Where(x => x.Info.LastWriteTimeUtc >= start && x.Info.LastWriteTimeUtc <= end)
				.ToArray();

			if (candidates.Length == 0)
			{
				Trace.Message(
					new TraceMessage(
						TraceMessageLevel.Warning,
						"There is no '.trx' file found, test results can not be displayed.")
					{
						File = path.Full
					});

				return null;
			}

			if (candidates.Length > 1)
			{
				Trace.Message(
					new TraceMessage(
						TraceMessageLevel.Warning,
						"There are several '.trx' files found, test results might be displayed incorrectly. Hint: possible reason is concurrent activity in the same folder.")
					{
						File = path.Full
					});				
			}

			return candidates[0].Path;
		}
	}
}