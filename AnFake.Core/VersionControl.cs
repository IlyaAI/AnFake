using System;
using AnFake.Core.Integration;

namespace AnFake.Core
{
	/// <summary>
	///		Represents version control related tools.
	/// </summary>
	public static class VersionControl
	{
		private readonly static Lazy<IVersionControl> Instance
			= new Lazy<IVersionControl>(Plugin.Get<IVersionControl>);

		/// <summary>
		///		Current changeset id of build directory.
		/// </summary>
		public static int CurrentChangesetId
		{
			get { return Instance.Value.CurrentChangesetId; }
		}

		/// <summary>
		///		Gets full (4-component) version.
		/// </summary>
		/// <remarks>
		///		Major and Minor components are always taken from given base version. 
		///		If base version contains Build component then it also copied to full one otherwise Build component of full version is set to 0.
		///		The Revision component is always set to <c>CurrentChangesetId</c>.
		/// </remarks>
		/// <param name="baseVersion">base version (not null)</param>
		/// <returns>full (4-component) version</returns>
		public static Version GetFullVersion(Version baseVersion)
		{
			if (baseVersion == null)
				throw new ArgumentException("VersionControl.GetFullVersion(baseVersion): baseVersion must not be null");

			return 
				new Version(
					baseVersion.Major,
					baseVersion.Minor,
					baseVersion.Build >= 0 ? baseVersion.Build : 0,
					CurrentChangesetId);
		}
	}
}