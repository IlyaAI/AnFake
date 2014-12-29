using AnFake.Core.Integration;

namespace AnFake.Core
{
	public static class VersionControl
	{
		public static int CurrentChangesetId
		{
			get { return Plugin.Get<IVersionControl>().CurrentChangesetId; }
		}
	}
}