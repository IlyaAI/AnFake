﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Logging;
using AnFake.Scripting;

namespace AnFake
{
	internal class Runner
	{
		private static readonly IDictionary<string, IScriptEvaluator> SupportedScripts =
			new Dictionary<string, IScriptEvaluator>(StringComparer.OrdinalIgnoreCase)
			{
				{".fsx", new FSharpEvaluator()},
				{".csx", new CSharpEvaluator()}
			};

		private class AbortException : Exception
		{
		}

		[STAThread]
		public static int Main(string[] args)
		{
			// Console might be unavailable if executing on server, 
			// so we use SafeOp.Try to skip exception if any.
			SafeOp.Try(ConfigureConsole);
			
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: AnFake.exe [<script>] [<target>] ... [<name>=<value>] ... [-dbg] [-stack]");
				Console.WriteLine("  <script>       Path to build script. Optional. Default 'build.fsx'");
				Console.WriteLine("                 Script must have either .fsx (F#) or .csx (C#) extension.");
				Console.WriteLine("                 If relative path is specified it's evaluated against current directory.");
				Console.WriteLine("  <target>       Target to run. Optional. Multiple. Default 'Build'");
				Console.WriteLine("                 Multiple targets might be specified via space separator.");
				Console.WriteLine("");
				Console.WriteLine("  <name>=<value> Additional build parameters. Optional. Multiple.");
				Console.WriteLine("                 Multiple pairs might be specified via space separator.");
				Console.WriteLine("  -dbg           Attach debugger just after AnFake start.");
				Console.WriteLine("");
				Console.WriteLine("  -stack         Enables full stack traces for thrown exception.");
				Console.WriteLine("                 By default only position in build.fsx is available.");
				Console.WriteLine("                 This option might be useful when debugging plugins.");
				Console.WriteLine("");
				Console.WriteLine("All arguments are optional but at least one must be specified.");
				Console.WriteLine("");
				Console.WriteLine("Example:");
				Console.WriteLine("  AnFake.exe Compile");
				Console.WriteLine("    runs target 'Compile' defined in 'build.fsx' script with no additional parameters");
				Console.WriteLine("");
				Console.WriteLine("  AnFake.exe build.csx");
				Console.WriteLine("    runs target 'Build' defined in 'build.csx' script with no additional parameters");
				Console.WriteLine("");
				Console.WriteLine("  AnFake.exe build.csx Compile MsBuild.Configuration=Debug");
				Console.WriteLine("    runs target 'Compile' defined in 'build.csx' script with additional parameter 'MsBuild.Configuration' set to 'Debug'");

				return 0;
			}

			var options = new RunOptions
			{
				BuildPath = Directory.GetCurrentDirectory()
			};

			try
			{
				ParseConfig(options);
				ParseCommandLine(args, options);

				ConfigureLogger(options);
				ConfigureTracer(options);
			}
			catch (AbortException)
			{
				// just skip
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("AnFake failed in initiation phase. See details below.");
				Console.WriteLine(e);
			}			

			return Run(options);
		}

		private static void ConfigureConsole()
		{
			Console.Title = "AnFake: Another F# Make";

			var consoleWidth = (int)(Console.LargestWindowWidth * 0.75);
			var consoleHeight = (int)(Console.LargestWindowHeight * 0.75);
			if (consoleWidth > Console.WindowWidth || consoleHeight > Console.WindowHeight)
			{
				Console.SetWindowSize(consoleWidth, consoleHeight);
			}
		}

		private static void ParseConfig(RunOptions options)
		{
			try
			{
				var localSettings = options.BuildPath.AsPath() / Settings.LocalPath;
				if (localSettings.AsFile().Exists())
				{
					foreach (var setting in new Settings(localSettings))
					{
						options.Properties.Add(setting.Key, setting.Value);
					}
				}
			}
			catch (Exception e)
			{
				ConsoleLog(String.Format("Failed to parse local settings '{0}'", Settings.LocalPath.AsPath().Full), e);
				throw new AbortException();
			}

			try
			{
				foreach (var setting in Settings.Current)
				{
					options.Properties.Add(setting.Key, setting.Value);
				}
			}
			catch (Exception e)
			{
				ConsoleLog(String.Format("Failed to parse user settings '{0}'", Settings.UserPath.AsPath().Full), e);
				throw new AbortException();
			}
		}

		private static void ParseCommandLine(IEnumerable<string> args, RunOptions options)
		{
			var propMode = false;
			var propIndex = 1;

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

				if (arg == "-p" || arg == "/p")
				{
					propMode = true;
					continue;
				}

				if (arg == "-dbg" || arg == "/dbg")
				{
					Debugger.Launch();
					continue;
				}

				if (arg == "-stack" || arg == "/stack")
				{
					AnFakeException.StackTraceMode = StackTraceMode.Full;
					continue;
				}

				if (propMode)
				{
					options.Properties.Add("__" + propIndex++, arg.Trim());
					continue;
				}

				options.Targets.Add(arg);
			}

			if (options.Targets.Count == 0)
			{
				options.Targets.Add("Build");
			}
		}

		private static void ConfigureLogger(RunOptions options)
		{
			options.Verbosity = Verbosity.Normal;
			options.LogPath = Path.Combine(options.BuildPath, "AnFake.log");

			string value;
			if (options.Properties.TryGetValue("Verbosity", out value))
			{
				if (!Enum.TryParse(value, true, out options.Verbosity))
					throw new ArgumentException(
						String.Format(
							"Unrecognized value '{0}'. Verbosity = {{{1}}}",
							value,
							String.Join("|", Enum.GetNames(typeof (Verbosity)))));

				options.Properties.Remove("Verbosity");
			}

			if (options.Properties.TryGetValue("LogPath", out value) && !String.IsNullOrWhiteSpace(value))
			{
				options.LogPath = Path.Combine(options.BuildPath, value);
				options.Properties.Remove("Verbosity");
			}

			var maxWidth = 180;
			SafeOp.Try(() => maxWidth = Math.Min(Console.WindowWidth, maxWidth));

			var logger = new Log4NetLogger(options.LogPath, maxWidth);
			switch (options.Verbosity)
			{
				case Verbosity.Quiet:
					logger.Threshold = LogMessageLevel.Warning;
					break;
				case Verbosity.Minimal:
					logger.Threshold = LogMessageLevel.Summary;
					break;
				case Verbosity.Detailed:
				case Verbosity.Diagnostic:
					logger.Threshold = LogMessageLevel.Debug;
					break;
				default:
					logger.Threshold = LogMessageLevel.Info;
					break;
			}			

			Log.Set(logger);
		}

		private static void ConfigureTracer(RunOptions options)
		{
			var tracer = new JsonFileTracer(Path.Combine(options.BuildPath, "AnFake.trace.jsx"), false);
			switch (options.Verbosity)
			{
				case Verbosity.Quiet:
					tracer.Threshold = TraceMessageLevel.Warning;
					break;
				case Verbosity.Minimal:
					tracer.Threshold = TraceMessageLevel.Summary;
					break;
				case Verbosity.Detailed:
				case Verbosity.Diagnostic:
					tracer.Threshold = TraceMessageLevel.Debug;
					break;
				default:
					tracer.Threshold = TraceMessageLevel.Info;
					break;
			}

			Api.Trace.Set(tracer);
		}

		private static int Run(RunOptions options)
		{
			SafeOp.Try(() =>
			{
				Console.Title += String.Format(" [{0}]", String.Join(", ", options.Targets));
			});

			try
			{
				var buildPath = options.BuildPath.AsPath();
				var scriptFile = new FileItem(buildPath / options.Script, buildPath);
				var logFile = new FileItem(options.LogPath.AsPath(), buildPath);

				FileSystemPath.Base = scriptFile.Folder;

				Api.Trace.Info("Configuring build...");
				Api.Trace.InfoFormat("BuildPath    : {0}", options.BuildPath);
				Api.Trace.InfoFormat("LogFile      : {0}", logFile);
				Api.Trace.InfoFormat("ScriptFile   : {0}", scriptFile);
				Api.Trace.InfoFormat("Targets      : {0}", String.Join(" ", options.Targets));
				Api.Trace.InfoFormat("Parameters   :\n  {0}", String.Join("\n  ", options.Properties.Select(x => x.Key + " = " + x.Value)));

				if (!scriptFile.Exists())
				{
					Api.Trace.ErrorFormat("Build script doesn't exist: {0}", scriptFile.Path.Full);
					return -1;
				}

				IScriptEvaluator evaluator;
				if (!SupportedScripts.TryGetValue(scriptFile.Ext, out evaluator))
				{
					Api.Trace.ErrorFormat("Unsupported scripting language: {0}", scriptFile.Ext);
					return -1;
				}

				MyBuild.Initialize(
						buildPath,
						logFile,
						scriptFile,
						options.Verbosity,
						options.Targets.ToArray(),
						options.Properties);

				Api.Trace.InfoFormat("AnFakeVersion: {0}", MyBuild.Current.AnFakeVersion);

				evaluator.Evaluate(scriptFile);

				Api.Trace.Info("Configuring plugins...");
				Plugin.Configure();
				
				var status = MyBuild.Run();
				var exitCode = status - MyBuild.Status.Succeeded;

				return exitCode;
			}
			catch (Exception e)
			{				
				Log.Error(AnFakeException.Wrap(e));
				return (int) MyBuild.Status.Unknown;
			}
		}

		private static void ConsoleLog(string message, Exception exception)
		{
			var prevColor = Console.ForegroundColor;

			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.WriteLine(exception.Message);
			Console.ForegroundColor = prevColor;
		}		
	}
}