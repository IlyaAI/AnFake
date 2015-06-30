using System;
using System.IO;
using AnFake.Api;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Logging;
using Microsoft.FSharp.Compiler.Interactive;
using Microsoft.FSharp.Core;

namespace AnFake.Scripting
{
	internal class FSharpFsiEvaluator : IScriptEvaluator
	{
		public void Evaluate(FileItem script)
		{
			var cfg = Shell.FsiEvaluationSession.GetDefaultConfiguration();			

			var fsx = Shell.FsiEvaluationSession.Create(
				cfg,
				new[] {script.Name},
				new StreamReader(Stream.Null),
				new LogWriter(LogMessageLevel.Debug),
				new LogWriter(LogMessageLevel.Error),
				FSharpOption<bool>.None);

			AnFakeException.ScriptSource = new ScriptSourceInfo(script.Name);

			try
			{
				fsx.EvalScript(script.Path.Full);
			}
			catch (Exception)
			{
				throw new EvaluationException("Evaluation aborted.");
			}			
		}
	}	
}