using AnFake.Core;
using Microsoft.TeamFoundation.Build.Client;

namespace AnFake.Plugins.Tfs2012
{
	internal static class BuildStatusConverter
	{
		public static BuildStatus AsTfsBuildStatus(this MyBuild.Status status)
		{
			switch (status)
			{
				case MyBuild.Status.Succeeded:
					return BuildStatus.Succeeded;

				case MyBuild.Status.PartiallySucceeded:
					return BuildStatus.PartiallySucceeded;

				case MyBuild.Status.Failed:
					return BuildStatus.Failed;

				default:
					return BuildStatus.None;
			}
		}

		public static MyBuild.Status AsMyBuildStatus(this BuildStatus status)
		{
			if ((status & BuildStatus.InProgress) == BuildStatus.InProgress)
				return MyBuild.Status.InProgress;

			if ((status & BuildStatus.Succeeded) == BuildStatus.Succeeded)
				return MyBuild.Status.Succeeded;

			if ((status & BuildStatus.PartiallySucceeded) == BuildStatus.PartiallySucceeded)
				return MyBuild.Status.PartiallySucceeded;

			if ((status & BuildStatus.Failed) == BuildStatus.Failed)
				return MyBuild.Status.Failed;

			return MyBuild.Status.Unknown;
		}
	}
}