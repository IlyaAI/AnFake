using System;
using AnFake.Core;
using CSScriptLibrary;
using Microsoft.FSharp.Compiler.Interactive;
using Microsoft.FSharp.Core;

namespace AnFake
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			MyBuild.Configure();

			RunFsx(args);
			//RunCsx(args);
		}

		private static void RunFsx(string[] args)
		{
			var cfg = Shell.FsiEvaluationSession.GetDefaultConfiguration();

			var fsx = Shell.FsiEvaluationSession.Create(
				cfg,
				new[] { "experimental.fsx" },
				Console.In,
				Console.Out,
				Console.Error,
				FSharpOption<bool>.None);

			fsx.EvalScript("../../experimental.fsx");
		}

		private static void RunCsx(string[] args)
		{
			var csx = (BuildScriptSkeleton) CSScript.LoadCodeFrom("../../experimental.cs").CreateObject("BuildScript");
			csx.Run();
		}
	}
}