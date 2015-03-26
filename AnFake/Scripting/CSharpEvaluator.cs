using AnFake.Core;
using CSScriptLibrary;

namespace AnFake.Scripting
{
	internal class CSharpEvaluator : IScriptEvaluator
	{
		public void Evaluate(FileItem script)
		{
			CSScript.GlobalSettings.AddSearchDir("[AnFake]".AsPath().Full);
			CSScript.GlobalSettings.AddSearchDir("[AnFakePlugins]".AsPath().Full);

			var csx = (BuildScriptSkeleton) CSScript.LoadCodeFrom(script.Path.Full).CreateObject("BuildScript");
			csx.Configure();
		}
	}
}