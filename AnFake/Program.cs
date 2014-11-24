using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.log4net;

namespace AnFake
{
	internal class Program
	{
		private static readonly IDictionary<string, IScriptEvaluator> SupportedScripts =
			new Dictionary<string, IScriptEvaluator>(StringComparer.InvariantCultureIgnoreCase)
			{
				{".fsx", new FSharpEvaluator()},
				{".csx", new CSharpEvaluator()}
			};

		private class BuildOptions
		{
			public readonly IDictionary<string, string> Properties = new Dictionary<string, string>();
			public readonly IList<string> Targets = new List<string>();
			public string Script = "build.fsx";
		}

		public static int Main(string[] args)
		{
			// It's impossible to use cool domain model here because it references ILog
			// so for log initialization the plain API is used
			var currentDir = Directory.GetCurrentDirectory();
			//BuildLogPattern.LogFile = Path.Combine(currentDir, "build.log");
			///////////////////////////////////////////////////////////////////////////
			BuildLogPattern.Touch();

			Console.SetWindowSize((int) (Console.LargestWindowWidth*0.75), (int) (Console.LargestWindowHeight*0.75));

			if (args.Length == 0)
			{
				Logger.Debug("Usage: AnFake.exe [<script>] [<target>] ... [<name>=<value>] ...");
				Logger.Debug("  <script>       Path to build script. Optional. Default 'build.fsx'");
				Logger.Debug("                 Script must have either .fsx (F#) or .csx (C#) extension.");
				Logger.Debug("                 If relative path is specified it's evaluated against current directory.");
				Logger.Debug("  <target>       Target to run. Optional. Multiple. Default 'Build'");
				Logger.Debug("                 Multiple targets might be specified via space separator.");
				Logger.Debug("");
				Logger.Debug("  <name>=<value> Additional build parameters. Optional. Multiple.");
				Logger.Debug("                 Multiple pairs might be specified via space separator.");
				Logger.Debug("");
				Logger.Debug("All arguments are optional but at least one must be specified.");
				Logger.Debug("");
				Logger.Debug("Example:");
				Logger.Debug("  AnFake.exe Compile");
				Logger.Debug("    runs target 'Compile' defined in 'build.fsx' script with no additional parameters");
				Logger.Debug("");
				Logger.Debug("  AnFake.exe build.csx");
				Logger.Debug("    runs target 'Build' defined in 'build.csx' script with no additional parameters");
				Logger.Debug("");
				Logger.Debug("  AnFake.exe build.csx Compile MsBuild.Configuration=Debug");
				Logger.Debug("    runs target 'Compile' defined in 'build.csx' script with additional parameter 'MsBuild.Configuration' set to 'Debug'");

				return 0;
			}

			var buildPath = currentDir.AsPath();
			var logFile = new FileItem(BuildLogPattern.LogFile.AsPath(), buildPath);

			var options = ParseOptions(args);

			var scriptFile = new FileItem(buildPath/options.Script, buildPath);
			if (!scriptFile.Exists())
			{
				Logger.ErrorFormat("Build script doesn't exist: {0}", scriptFile.RelPath);
				return -1;
			}

			IScriptEvaluator evaluator;
			if (!SupportedScripts.TryGetValue(scriptFile.Ext, out evaluator))
			{
				Logger.ErrorFormat("Unsupported scripting language: {0}", scriptFile.Ext);
				return -1;
			}

			Logger.DebugFormat("BuildPath : {0}", buildPath);
			Logger.DebugFormat("LogFile   : {0}", logFile);
			Logger.DebugFormat("ScriptFile: {0}", scriptFile);
			Logger.DebugFormat("Evaluator : {0}", evaluator.GetType().FullName);
			Logger.DebugFormat("Targets   : {0}", String.Join(" ", options.Targets));
			Logger.DebugFormat("Parameters:\n  {0}", String.Join("\n  ", options.Properties.Select(x => x.Key + " = " + x.Value)));

			try
			{
				MyBuild.Initialize(
					new MyBuild.Params(
						buildPath,
						logFile,
						scriptFile,
						options.Targets.ToArray(),
						options.Properties));
				Logger.DebugFormat("BasePath  : {0}", FileSystemPath.Base);

				Logger.Debug("Configuring build...");
				evaluator.Evaluate(scriptFile);

				Logger.Debug("Running targets...");
				foreach (var target in options.Targets)
				{
					Target.Get(target).Run();
				}
			}
			catch (TerminateTargetException)
			{
				// just skip, its already processed
			}
			catch (EvaluationAbortedException)
			{
				// just skip, its already processed
			}
			catch (Exception e)
			{
				var error = (e as AnFakeException) ?? new AnFakeWrapperException(e);
				
				Logger.Error(error);

				// Do the best efforts to notify observers via Tracer
				if (Tracer.IsInitialized)
				{
					try
					{
						Tracer.Error(error);
					}
						// ReSharper disable once EmptyGeneralCatchClause
					catch (Exception)
					{
						// ignore
					}
				}

				return 1;
			}

			return 0;
		}

		private static BuildOptions ParseOptions(IEnumerable<string> args)
		{
			var options = new BuildOptions();

			foreach (var arg in args)
			{
				if (arg.Contains(".") && SupportedScripts.ContainsKey(Path.GetExtension(arg)))
				{
					options.Script = arg;
					continue;
				}

				if (arg.Contains("="))
				{
					var index = arg.IndexOf("=", StringComparison.InvariantCulture);

					options.Properties.Add(arg.Substring(0, index).Trim(), arg.Substring(index + 1).Trim());
					continue;
				}

				options.Targets.Add(arg);
			}

			if (options.Targets.Count == 0)
			{
				options.Targets.Add("Build");
			}

			return options;
		}		
	}
}