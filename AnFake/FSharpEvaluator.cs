using System;
using System.IO;
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
				new StreamReader(Stream.Null),
				Console.Out,
				Console.Error,
				FSharpOption<bool>.None);

			fsx.EvalScript(script.Path.Full);
		}
	}
}