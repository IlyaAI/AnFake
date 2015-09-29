using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core.Integration.Tests;

namespace AnFake.Core
{
	/// <summary>
	///		Represents MSTest tool.
	/// </summary>
	public static class MsTest
	{
		private static readonly string[] Locations =
		{
			"[ProgramFilesX86]/Microsoft Visual Studio 14.0/Common7/IDE/MsTest.exe",
			"[ProgramFilesX86]/Microsoft Visual Studio 12.0/Common7/IDE/MsTest.exe",
			"[ProgramFilesX86]/Microsoft Visual Studio 11.0/Common7/IDE/MsTest.exe"
		};

		/// <summary>
		///		MSTest parameters.
		/// </summary>
		public sealed class Params
		{
			/// <summary>
			///		Category of tests to be run.
			/// </summary>
			public string Category;

			/// <summary>
			///		Results directory path.
			/// </summary>
			public FileSystemPath ResultsDirectory;

			/// <summary>
			///		Path to .testsettings file.
			/// </summary>
			public FileSystemPath TestSettingsPath;

			/// <summary>
			///		Working directory.
			/// </summary>
			public FileSystemPath WorkingDirectory;

			/// <summary>
			///		Timeout.
			/// </summary>
			public TimeSpan Timeout;

			/// <summary>
			///		No isolation flag.
			/// </summary>
			public bool NoIsolation;

			/// <summary>
			///		Path to MSTest.exe.
			/// </summary>
			/// <remarks>
			///		Normally, ToolPath is evaluated automatically but you could provide specific one.
			/// </remarks>
			public FileSystemPath ToolPath;

			/// <summary>
			///		Additional command line arguments for MSTest.exe
			/// </summary>
			/// <remarks>
			///		Additional arguments appended to command line as is be carefull with quotes and spaces.
			/// </remarks>
			public string ToolArguments;

			internal Params()
			{				
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

		static MsTest()
		{
			Defaults = new Params();
		}

		/// <summary>
		///		Runs tests from specified assemblies with default parameters.
		/// </summary>
		/// <param name="assemblies">set of assemblies with tests (not null)</param>
		public static void Run(IEnumerable<FileItem> assemblies)
		{
			Run(assemblies, p => { });
		}

		/// <summary>
		///		Runs tests from specified assemblies with overrided parameters.
		/// </summary>
		/// <param name="assemblies">set of assemblies with tests (not null)</param>
		/// <param name="setParams">action which overrides default parameters (not null)</param>
		public static void Run(IEnumerable<FileItem> assemblies, Action<Params> setParams)
		{
			if (assemblies == null)
				throw new ArgumentException("MsTest.Run(assemblies[, setParams]): assemblies must not be null");
			if (setParams == null)
				throw new ArgumentException("MsTest.Run(assemblies, setParams): setParams must not be null");

			var assembliesArray = assemblies.ToArray();
			if (assembliesArray.Length == 0)
				throw new ArgumentException("MsTest.Run(assemblies, setParams): assemblies must not be an empty list");

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
						.PostProcess(assembly.Name, resultPath.AsFile())
						.Trace();

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