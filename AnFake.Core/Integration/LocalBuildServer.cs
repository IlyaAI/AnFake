using System;
using System.Text;

namespace AnFake.Core.Integration
{
	internal sealed class LocalBuildServer : IBuildServer
	{
		public bool IsLocal
		{
			get { return true; }
		}

		public bool CanExposeArtifacts
		{
			get { return true; }
		}

		public Uri ExposeArtifact(FileItem file, string type)
		{
			return new UriBuilder(Uri.UriSchemeFile, "") {Path = file.Path.Full}.Uri;
		}

		public Uri ExposeArtifact(FolderItem folder, string type)
		{
			return new UriBuilder(Uri.UriSchemeFile, "") {Path = folder.Path.Full}.Uri;
		}

		public Uri ExposeArtifact(string name, string content, Encoding encoding, string type)
		{
			return new Uri("about:blank");
		}

		public void ExposeArtifacts(FileSet files, string type)
		{
			// do nothing
		}
	}
}