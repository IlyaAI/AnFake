using AnFake.Core;

namespace AnFake
{
	internal interface IScriptEvaluator
	{
		void Evaluate(FileItem script);
	}
}