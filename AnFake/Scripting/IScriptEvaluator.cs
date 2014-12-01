using AnFake.Core;

namespace AnFake.Scripting
{
	internal interface IScriptEvaluator
	{
		void Evaluate(FileItem script);
	}
}