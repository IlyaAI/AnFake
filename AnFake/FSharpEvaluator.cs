using System;
using AnFake.Core;
using Microsoft.FSharp.Compiler.Interactive;
using Microsoft.FSharp.Core;

namespace AnFake
{
	internal class FSharpEvaluator : IScriptEvaluator
	{
		public void Evaluate(FileItem script)
		{
			var cfg = Shell.FsiEvaluationSession.GetDefaultConfiguration();

			var fsx = Shell.FsiEvaluationSession.Create(
				cfg,
				new[] {script.Name},
				Console.In,
				Console.Out,
				Console.Error,
				FSharpOption<bool>.None);

			fsx.EvalScript(script.Path.Full);
		}
	}
}