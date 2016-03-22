using System;
using System.Text;
using AnFake.Core.Exceptions;
using AnFake.Core.Integration.Builds;

namespace AnFake.Core.Integration
{
	internal sealed class LocalBuildServer : IBuildServer2
	{
		public bool IsLocal
		{
			get { return true; }
		}

		public int CurrentChangesetId
		{
			get { return 0; }
		}

		public string CurrentChangesetHash
		{
			get { return ""; }
		}

		public int CurrentBuildCounter
		{
			get { return 0; }
		}

		public string CurrentBuildNumber
		{
			get { return "local"; }
		}

		public string CurrentConfigurationName
		{
			get { throw new InvalidConfigurationException("LocalBuildServer.CurrentConfigurationName: not supported"); }
		}

		public bool CanExposeArtifacts
		{
			get { return true; }
		}

		public Uri ExposeArtifact(FileItem file, string targetFolder)
		{
			return new UriBuilder(Uri.UriSchemeFile, "") {Path = file.Path.Full}.Uri;
		}

		public Uri ExposeArtifact(FolderItem folder, string targetFolder)
		{
			return new UriBuilder(Uri.UriSchemeFile, "") {Path = folder.Path.Full}.Uri;
		}

		public Uri ExposeArtifact(string name, string content, Encoding encoding, string targetFolder)
		{
			return new Uri("about:blank");
		}

		public void ExposeArtifacts(FileSet files, string targetFolder)
		{
			// do nothing
		}

		public void SetCurrentBuildNumber(string value)
		{
			// do nothing
		}

		public void TagCurrentBuild(string tag)
		{
			// do nothing
		}

		public IBuild FindLastGoodBuild(string configurationName)
		{
			throw new InvalidConfigurationException("LocalBuildServer.FindLastGoodBuild: not supported");
		}

		public IBuild FindLastTaggedBuild(string configurationName, string[] tags)
		{
			throw new InvalidConfigurationException("LocalBuildServer.FindLastTaggedBuild: not supported");
		}
	}
}