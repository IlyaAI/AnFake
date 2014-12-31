using System;
using AnFake.Core.Integration;

namespace AnFake.Core
{
	public static class VersionControl
	{
		private readonly static Lazy<IVersionControl> Instance
			= new Lazy<IVersionControl>(Plugin.Get<IVersionControl>);

		public static int CurrentChangesetId
		{
			get { return Instance.Value.CurrentChangesetId; }
		}
	}
}