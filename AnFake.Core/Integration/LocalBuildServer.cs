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

		public bool HasPipeIn
		{
			get { return false; }
		}

		public int PipeInChangesetId
		{
			get { throw new NotSupportedException("Local build server doesn't support pipelined builds."); }
		}

		public Uri ExposeArtifact(FileItem file, ArtifactType type)
		{
			return new UriBuilder(Uri.UriSchemeFile, "") {Path = file.Path.Full}.Uri;
		}

		public Uri ExposeArtifact(FolderItem folder, ArtifactType type)
		{
			return new UriBuilder(Uri.UriSchemeFile, "") {Path = folder.Path.Full}.Uri;
		}

		public Uri ExposeArtifact(string name, string content, Encoding encoding, ArtifactType type)
		{
			return new Uri("about:blank");
		}

		public void ExposeArtifacts(FileSet files, ArtifactType type)
		{
			// do nothing
		}		

		public void GetPipeInArtifact(FileItem file, FileSystemPath dstPath, ArtifactType type)
		{
			throw new NotSupportedException("Local build server doesn't support pipelined builds.");
		}

		public void GetPipeInArtifact(FolderItem folder, FileSystemPath dstPath, ArtifactType type)
		{
			throw new NotSupportedException("Local build server doesn't support pipelined builds.");
		}

		public void GetPipeInArtifacts(FileSet files, FileSystemPath dstPath, ArtifactType type)
		{
			throw new NotSupportedException("Local build server doesn't support pipelined builds.");
		}		
	}
}