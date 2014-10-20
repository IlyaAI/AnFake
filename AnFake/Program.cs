using System;
using System.Collections.Generic;
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

		private static int Main(string[] args)
		{
			if (args.Length < 2)
			{
				Logger.Debug("TODO: place help here");
				return 0;
			}

			var script = args[0].AsFile();
			if (!script.Exists())
			{
				Logger.ErrorFormat("Build script doesn't exist: {0}", script.Path.Full);
				return -1;
			}

			IScriptEvaluator evaluator;
			if (!SupportedScripts.TryGetValue(script.Ext, out evaluator))
			{
				Logger.ErrorFormat("Unsupported scripting language: {0}", script.Ext);
				return -1;
			}

			var target = args[1];

			try
			{
				Logger.Debug("Configuring build...");
				MyBuild.Configure(script.Folder);
				evaluator.Evaluate(script);

				Logger.DebugFormat("Starting target: {0}", target);
				target.AsTarget().Run();
			}
			catch (Exception e)
			{
				Logger.Error(e);
				return 1;
			}

			return 0;
		}
	}
}