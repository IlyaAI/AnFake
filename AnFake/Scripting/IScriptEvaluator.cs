using AnFake.Core;

namespace AnFake.Scripting
{
	internal interface IScriptEvaluator
	{
		FileSystemPath GetBasePath(FileItem script);

		void Evaluate(FileItem script, bool debug);
	}
}