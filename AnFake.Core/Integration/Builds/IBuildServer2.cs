namespace AnFake.Core.Integration.Builds
{
	/// <summary>
	///		Represents extended version of <c>IBuildServer</c> interface.
	/// </summary>
	public interface IBuildServer2 : IBuildServer
	{
		/// <summary>
		///		Gets last successful build with specified configuration name.
		///		Throws exception if no such build.
		/// </summary>
		/// <param name="configurationName">configuration name (not null)</param>
		/// <returns>IBuild instance (not null)</returns>
		IBuild GetLastGoodBuild(string configurationName);

		/// <summary>
		///		Gets last build with specified configuration name and marked with all given tags.
		///		Throws exception if no such build.
		/// </summary>
		/// <param name="configurationName">configuration name (not null)</param>
		/// <param name="tags">set of tags (not null)</param>
		/// <returns>IBuild instance (not null)</returns>
		IBuild GetLastTaggedBuild(string configurationName, string[] tags);
	}
}
