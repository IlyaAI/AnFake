using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Tests;

namespace AnFake.Core
{
	public static class MsTest
	{
		public sealed class Params
		{
			public string Category;
			public FileSystemPath ResultsDirectory;
			public FileSystemPath TestMetadataPath;
			public FileSystemPath TestSettingsPath;
			public FileSystemPath WorkingDirectory;      
			public TimeSpan Timeout;
			public FileSystemPath ToolPath;
			public bool NoIsolation;

			public Params()
			{
				WorkingDirectory = "".AsPath();
				Timeout = TimeSpan.MaxValue;
			}

			public Params Clone()
			{
				return (Params)MemberwiseClone();
			}
		}

		public static Params Defaults { get; private set; }

		static MsTest()
		{
			Defaults = new Params();
		}

		public static TestExecutionResult Run(Action<Params> setParams, IEnumerable<FileItem> assemblies)
		{
			var assembliesArray = assemblies.ToArray();
			if (assembliesArray.Length == 0)
				throw new ArgumentException("MsTest.Run(setParams, assemblies): assemblies must not be an empty list");

			var parameters = Defaults.Clone();
			setParams(parameters);

			/*if (parameters.WorkingDirectory == null)
				throw new ArgumentException("MsTest.Params.WorkingDirectory must not be null");

			var suites = new List<TestSuiteResult>();
			var errors = 0;

			Logger.DebugFormat("MsTest =>\n  {0}", String.Join("\n  ", assembliesArray.Select(x => x.RelPath)));
			
			foreach (var assembly in assembliesArray)
			{			
				var resultPath = (parameters.ResultsDirectory ?? parameters.WorkingDirectory) / assembly.NameWithoutExt.AsUnique(".trx");

				var args = Process.Args("/", ":")
					.Option("testcontainer", assembly.Path)
					.Option("category", parameters.Category)
					.Option("testmetadata", parameters.TestMetadataPath.AsFullPath())
					.Option("testsettings", parameters.TestSettingsPath.AsFullPath())
					.Option("resultsfile", resultPath.AsFullPath())
					.Option("noisolation", parameters.NoIsolation)
					.ToString();				

				var result = Process.Run(p =>
				{
					p.FileName = parameters.ToolPath;
					p.WorkingDirectory = parameters.WorkingDirectory;
					p.Timeout = parameters.Timeout;
					p.Arguments = args;
				});

				if (File.Exists(resultPath))
				{
					var suite = ParseResults(assembly, resultPath);
					TraceResults(suite, ref errors);					

					suites.Add(suite);
				}
				else if (result.ExitCode != 0)
				{
					throw new TargetFailureException(String.Format("MsTest failed with exit code {0}.\n  Assembly: {1}", result.ExitCode, assembly.Path));
				}
			}

			if (errors > 0)
				throw new TerminateTargetException("Target terminated due to test failures.");*/

			//return new TestExecutionResult(errors, suites);

			return null;
		}

		private static TestSuiteResult ParseResults(FileItem assembly, string resultPath)
		{
			throw new NotImplementedException();
		}

		private static void TraceResults(TestSuiteResult suite, ref int errors)
		{
			throw new NotImplementedException();
		}
	}
}