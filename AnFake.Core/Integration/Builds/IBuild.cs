using System;

namespace AnFake.Core.Integration.Builds
{
	/// <summary>
	///		Represents build on build-server.
	/// </summary>
	public interface IBuild
	{
		/// <summary>
		///		URI to access build details via browser (not null, read-only).
		/// </summary>
		Uri Uri { get; }

		/// <summary>
		///		URI to identify build via API (not null, read-only).
		/// </summary>
		Uri NativeUri { get; }

		/// <summary>
		///		Build configuration name (not null, read-only).
		/// </summary>
		string ConfigurationName { get; }

		/// <summary>
		///		Numeric changeset identifier (read-only). Throws exception if not available.
		/// </summary>
		int ChangesetId { get; }

		/// <summary>
		///		String changeset identifier (not null, read-only).
		/// </summary>
		string ChangesetHash { get; }

		/// <summary>
		///		Integer build number (read-only). Throws exception if not available.
		/// </summary>
		int Counter { get; }

		/// <summary>
		///		Build start date/time (read-only).
		/// </summary>
		DateTime Started { get; }

		/// <summary>
		///		Build finish date/time (read-only).
		/// </summary>
		DateTime Finished { get; }

		/// <summary>
		///		Tags associated with a build (not null, read-only, might be empty).
		/// </summary>
		string[] Tags { get; }

		/// <summary>
		///		Adds new tag.
		/// </summary>
		/// <param name="tag">tag to be added (not null or empty)</param>
		void AddTag(string tag);

		/// <summary>
		///		Removes tag. Does nothing if no such tag.
		/// </summary>
		/// <param name="tag">tag to be removed (not null or empty)</param>
		void RemoveTag(string tag);

		/// <summary>
		///		Returns true if build has not null and non-empty property with specified name and false otherwise.
		/// </summary>
		/// <param name="name">property name (not null or empty)</param>
		/// <returns>true if property exists not null and non-empty</returns>
		bool HasProp(string name);

		/// <summary>
		///		Returns property value. If property doesn't exists or has null or empty value then exception is thrown.
		/// </summary>
		/// <param name="name">property name (not null or empty)</param>
		/// <returns>property value (not null, non-empty)</returns>
		string GetProp(string name);

		/// <summary>
		///		Downloads specified artifacts.
		/// </summary>
		/// <remarks>
		///		Artifacts will be downloaded to destination path with sub-folders against base artifacts path.
		///		By default if the same file already exists in destination folder then exception is thrown.
		/// </remarks>
		/// <param name="artifactsPath">base artifacts path (not null, might be empty)</param>		
		/// <param name="dstPath">destination path (not null)</param>
		/// <param name="pattern">artifacts wildcard against base path (might be null)</param>
		/// <param name="overwrite">whether overwrite existing files or not (default false)</param>
		void DownloadArtifacts(string artifactsPath, FileSystemPath dstPath, string pattern = null, bool overwrite = false);
	}
}
