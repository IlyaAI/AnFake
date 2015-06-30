using AnFake.Core;
using AnFake.Core.Exceptions;
using CSScriptLibrary;

namespace AnFake.Scripting
{
	internal class CSharpEvaluator : IScriptEvaluator
	{
		public void Evaluate(FileItem script)
		{
			CSScript.GlobalSettings.AddSearchDir("[AnFake]".AsPath().Full);

			AnFakeException.ScriptSource = new ScriptSourceInfo(script.Name);

			var csx = (BuildScriptSkeleton) CSScript.LoadCodeFrom(script.Path.Full).CreateObject("BuildScript");
			csx.Configure();
		}
	}
}