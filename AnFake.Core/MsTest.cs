using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Integration.Tests;

namespace AnFake.Core
{
	public static class MsTest
	{
		private static readonly string[] Locations =
		{
			"[ProgramFilesX86]/Microsoft Visual Studio 12.0/Common7/IDE/MsTest.exe",
			"[ProgramFilesX86]/Microsoft Visual Studio 11.0/Common7/IDE/MsTest.exe"
		};

		public sealed class Params
		{
			public string Category;
			public FileSystemPath ResultsDirectory;
			public FileSystemPath TestSettingsPath;
			public FileSystemPath WorkingDirectory;
			public TimeSpan Timeout;			
			public bool NoIsolation;			
			public FileSystemPath ToolPath;
			public string ToolArguments;

			internal Params()
			{				
				Timeout = TimeSpan.MaxValue;				
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

		public static void Run(IEnumerable<FileItem> assemblies)
		{
			Run(assemblies, p => { });
		}

		public static void Run(IEnumerable<FileItem> assemblies, Action<Params> setParams)
		{
			var assembliesArray = assemblies.ToArray();
			if (assembliesArray.Length == 0)
				throw new ArgumentException("MsTest.Run(setParams, assemblies): assemblies must not be an empty list");

			var parameters = Defaults.Clone();
			setParams(parameters);

			if (parameters.ToolPath == null)
				throw new ArgumentException(
					String.Format(
						"MsTest.Params.ToolPath must not be null.\nHint: probably, MsTest.exe not found.\nSearch path:\n  {0}",
						String.Join("\n  ", Locations)));

			// TODO: check other parameters

			//if (parameters.WorkingDirectory == null)
			//	throw new ArgumentException("MsTest.Params.WorkingDirectory must not be null");

			Trace.InfoFormat("MsTest.Run\n => {0}", String.Join("\n => ", assembliesArray.Select(x => x.RelPath)));

			var tests = new List<TestResult>();
			var postProcessor = Plugin.Find<IMsTrxPostProcessor>();
			
			foreach (var assembly in assembliesArray)
			{
				Trace.DebugFormat("{0}...", assembly.RelPath);

				var workDir = parameters.WorkingDirectory ?? assembly.Folder;
				var resultDir = parameters.ResultsDirectory ?? workDir;
				var resultPath = resultDir/assembly.NameWithoutExt.MakeUnique(".trx");

				Directory.CreateDirectory(resultDir.Full);

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

				if (postProcessor != null && File.Exists(resultPath.Full))
				{
					var currentTests = postProcessor
						.PostProcess(resultPath)
						.Trace(assembly.Name, resultPath.Full);

					tests.AddRange(currentTests);
				}
				else
				{
					result
						.FailIfAnyError("Target terminated due to MsTest errors.")
						.FailIfExitCodeNonZero(
							String.Format("MsTest failed with exit code {0}. Assembly: {1}", result.ExitCode, assembly));
				}
			}			

			tests.TraceSummary();					
		}
	}
}