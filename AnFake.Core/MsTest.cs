using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Exceptions;
using AnFake.Core.Tests;

namespace AnFake.Core
{
	public static class MsTest
	{
		private static readonly string[] Locations =
		{
			"[ProgramFilesX86]/Microsoft Visual Studio 12.0/Common7/IDE/MsTest.exe"
		};

		public sealed class Params
		{
			public string Category;
			public FileSystemPath ResultsDirectory;
			public FileSystemPath TestSettingsPath;
			public FileSystemPath WorkingDirectory;
			public TimeSpan Timeout;			
			public bool NoIsolation;
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

		static MsTest()
		{
			Defaults = new Params();
		}

		public static TestExecutionResult Run(IEnumerable<FileItem> assemblies)
		{
			return Run(assemblies, p => { });
		}

		public static TestExecutionResult Run(IEnumerable<FileItem> assemblies, Action<Params> setParams)
		{
			var assembliesArray = assemblies.ToArray();
			if (assembliesArray.Length == 0)
				throw new AnFakeArgumentException("MsTest.Run(setParams, assemblies): assemblies must not be an empty list");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.ToolPath == null)
				throw new AnFakeArgumentException(
					String.Format(
						"MsTest.Params.ToolPath must not be null.\nHint: probably, MsTest.exe not found.\nSearch path:\n  {0}",
						String.Join("\n  ", Locations)));

			// TODO: check other parameters

			//if (parameters.WorkingDirectory == null)
			//	throw new AnFakeArgumentException("MsTest.Params.WorkingDirectory must not be null");

			Trace.InfoFormat("MsTest.Run\n => {0}", String.Join("\n => ", assembliesArray.Select(x => x.RelPath)));

			var tests = new List<TestResult>();
			
			foreach (var assembly in assembliesArray)
			{
				Trace.DebugFormat("{0}...", assembly.RelPath);

				var workDir = parameters.WorkingDirectory ?? assembly.Folder;
				var resultPath = (parameters.ResultsDirectory ?? workDir)/assembly.NameWithoutExt.MakeUnique(".trx");

				var args = new Args("/", ":")
					.Option("testcontainer", assembly.Path.Full)
					.Option("category", parameters.Category)
					.Option("testsettings", parameters.TestSettingsPath)
					.Option("resultsfile", resultPath.Full)
					.Option("noisolation", parameters.NoIsolation)
					.Other(parameters.ToolArguments)
					.ToString();

				var result = Process.Run(p =>
				{
					p.FileName = parameters.ToolPath;
					p.WorkingDirectory = workDir;
					p.Timeout = parameters.Timeout;
					p.Arguments = args;					
				});

				if (parameters.PostProcessor != null && File.Exists(resultPath.Full))
				{
					var currentTests = parameters.PostProcessor
						.PostProcess(resultPath)
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
								LinkHref = resultPath.ToRelative(MyBuild.Defaults.Path).Spec
							});

					tests.AddRange(currentTests);
				}
				else if (result.ExitCode != 0)
				{
					throw new TargetFailureException(String.Format("MsTest failed with exit code {0}.\n  Assembly: {1}", result.ExitCode, assembly.Path));
				}
			}			

			var testResult = tests.TraceSummary();
			testResult.FailIfAnyError("Target terminated due to test failures.");

			return testResult;
		}
	}
}