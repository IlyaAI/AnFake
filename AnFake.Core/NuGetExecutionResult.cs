using AnFake.Api;

namespace AnFake.Core
{
	public sealed class NuGetExecutionResult : IToolExecutionResult
	{
		private readonly ProcessExecutionResult _result;
		private readonly FileSystemPath _packagePath;

		public NuGetExecutionResult(ProcessExecutionResult result, FileSystemPath packagePath)
		{
			_result = result;
			_packagePath = packagePath;
		}

		public int ErrorsCount
		{
			get { return _result.ErrorsCount; }
		}

		public int WarningsCount
		{
			get { return _result.WarningsCount; }
		}

		public FileSystemPath PackagePath
		{
			get { return _packagePath; }
		}
	}
}