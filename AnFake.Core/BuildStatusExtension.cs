using System.Diagnostics.CodeAnalysis;

namespace AnFake.Core
{
	public static class BuildStatusExtension
	{
		[SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
		public static string ToHumanReadable(this MyBuild.Status status)
		{
			switch (status)
			{
				case MyBuild.Status.InProgress:
					return "In Progress";

				case MyBuild.Status.PartiallySucceeded:
					return "Partially Succeeded";

				default:
					return status.ToString();
			}
		}

		public static bool IsGood(this MyBuild.Status status)
		{
			return status == MyBuild.Status.Succeeded || status == MyBuild.Status.PartiallySucceeded;
		}
	}
}