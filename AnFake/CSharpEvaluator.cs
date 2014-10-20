using AnFake.Core;
using CSScriptLibrary;

namespace AnFake
{
	internal class CSharpEvaluator : IScriptEvaluator
	{
		public void Evaluate(FileItem script)
		{
			var csx = (BuildScriptSkeleton) CSScript.LoadCodeFrom(script.Path.Full).CreateObject("BuildScript");

			// TODO: check for null

			csx.Run();
		}
	}
}