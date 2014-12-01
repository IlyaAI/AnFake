using System;
using System.IO;
using AnFake.Core;
using AnFake.Core.Exceptions;
using AnFake.Logging;
using Common.Logging;
using Microsoft.FSharp.Compiler.Interactive;
using Microsoft.FSharp.Core;

namespace AnFake.Scripting
{
	internal class FSharpEvaluator : IScriptEvaluator
	{
		private static readonly ILog Log = LogManager.GetLogger<FSharpEvaluator>();

		public void Evaluate(FileItem script)
		{
			var cfg = Shell.FsiEvaluationSession.GetDefaultConfiguration();

			var fsx = Shell.FsiEvaluationSession.Create(
				cfg,
				new[] {script.Name},
				new StreamReader(Stream.Null),
				new LogTraceWriter(Log), 
				new LogErrorWriter(Log),
				FSharpOption<bool>.None);

			try
			{
				fsx.EvalScript(script.Path.Full);
			}
			catch (Exception e)
			{				
				throw new TerminateTargetException("Evaluation aborted.", e);
			}			
		}
	}
}