namespace AnFake.Core.Integration.Builds
{
	/// <summary>
	///		Represents extended version of <c>IBuildServer</c> interface.
	/// </summary>
	public interface IBuildServer2 : IBuildServer
	{
		/// <summary>
		///		Current build's changeset id. Throws exception if VCS doesn't provide integer identifier.
		/// </summary>
		int CurrentChangesetId { get; }

		/// <summary>
		///		Current build's changeset hash.
		/// </summary>
		string CurrentChangesetHash { get; }

		/// <summary>
		///		Current build's counter. Throws exception if build server doesn't provide integer build counter.
		/// </summary>
		int CurrentBuildCounter { get; }

		/// <summary>
		///		Current build's number.
		/// </summary>
		string CurrentBuildNumber { get; }

		/// <summary>
		///		Sets current build's number.
		/// </summary>
		/// <param name="value">build number to be set (not null or empty)</param>
		void SetCurrentBuildNumber(string value);

		/// <summary>
		///		Current build's configuration name.
		/// </summary>
		string CurrentConfigurationName { get; }

		/// <summary>
		///		Finds last successful build with specified configuration name.
		///		Returns null if no such build.
		/// </summary>
		/// <param name="configurationName">configuration name (not null)</param>
		/// <returns>IBuild instance</returns>
		IBuild FindLastGoodBuild(string configurationName);

		/// <summary>
		///		Finds last build with specified configuration name and marked with all given tags.
		///		Returns null if no such build.
		/// </summary>
		/// <param name="configurationName">configuration name (not null)</param>
		/// <param name="tags">set of tags (not null)</param>
		/// <returns>IBuild instance</returns>
		IBuild FindLastTaggedBuild(string configurationName, string[] tags);
	}
}
