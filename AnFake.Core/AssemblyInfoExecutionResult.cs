using AnFake.Api;

namespace AnFake.Core
{
	public sealed class AssemblyInfoExecutionResult : IToolExecutionResult
	{
		private readonly Snapshot _snapshot;
		private readonly int _warningsCount;

		internal AssemblyInfoExecutionResult(Snapshot snapshot, int warningsCount)
		{
			_snapshot = snapshot;
			_warningsCount = warningsCount;
		}

		public int ErrorsCount
		{
			get { return 0; }
		}

		public int WarningsCount
		{
			get { return _warningsCount; }
		}

		public void Revert()
		{
			_snapshot.Revert();
		}
	}
}