using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnFake.Core;

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

		class BuildOptions
		{
			public readonly IDictionary<string, string> Parameters = new Dictionary<string, string>();
			public readonly IList<string> Targets = new List<string>();
			public string Script = "build.fsx";			
		}

		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Logger.Debug("TODO: place help here");
				return 0;
			}

			var options = ParseOptions(args);			

			var script = options.Script.AsFile();
			if (!script.Exists())
			{
				Logger.ErrorFormat("Build script doesn't exist: {0}", script.Path.Spec);
				return -1;
			}

			IScriptEvaluator evaluator;
			if (!SupportedScripts.TryGetValue(script.Ext, out evaluator))
			{
				Logger.ErrorFormat("Unsupported scripting language: {0}", script.Ext);
				return -1;
			}

			Logger.DebugFormat("Script    : {0}", script.Path.Full);
			Logger.DebugFormat("BasePath  : {0}", script.Folder);
			Logger.DebugFormat("Evaluator : {0}", evaluator.GetType().FullName);
			Logger.DebugFormat("Targets   : {0}", String.Join(" ", options.Targets));
			Logger.DebugFormat("Parameters: {0}", String.Join(" ", options.Parameters.Select(x => x.Key + " = " + x.Value)));

			try
			{
				Logger.Debug("Configuring build...");
				MyBuild.Configure(script.Folder);
				evaluator.Evaluate(script);

				Logger.Debug("Running targets...");
				foreach (var target in options.Targets)
				{
					target.AsTarget().Run();
				}				
			}				
			catch (Exception e)
			{
				Logger.Error(e);
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

					options.Parameters.Add(arg.Substring(0, index).Trim(), arg.Substring(index + 1).Trim());
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