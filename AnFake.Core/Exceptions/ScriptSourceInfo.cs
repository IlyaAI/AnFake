namespace AnFake.Core.Exceptions
{
	public sealed class ScriptSourceInfo
	{
		public ScriptSourceInfo(string originalName)
		{
			OriginalName = GeneratedName = originalName;
		}

		public ScriptSourceInfo(string originalName, string generatedName, int linesOffset)
		{
			OriginalName = originalName;
			GeneratedName = generatedName;
			LinesOffset = linesOffset;
		}

		public string OriginalName { get; private set; }

		public string GeneratedName { get; private set; }

		public int LinesOffset { get; private set; }
	}
}