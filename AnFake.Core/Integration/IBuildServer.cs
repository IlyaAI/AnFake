using System;
using System.Text;

namespace AnFake.Core.Integration
{
	/// <summary>
	///		Represents extension point for build server integration.
	/// </summary>
	public interface IBuildServer
	{
		/// <summary>
		///		Is this local build?
		/// </summary>
		bool IsLocal { get; }

		/// <summary>
		///		Can this build expose artifacts?
		/// </summary>
		bool CanExposeArtifacts { get; }		

		/// <summary>
		///		Exposes given file as build artifact of specified type and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <remarks>
		///		<para>
		///			Method throws if build server unable to expose artifact in any reason including: 
		///			public location isn't configured; artifact with the same name already exists; 
		///			any i/o error occured.
		///		</para>
		/// </remarks>
		/// <param name="file">file to be exposed</param>
		/// <param name="type"><see cref="ArtifactType"/></param>
		/// <returns>URI of exposed artifact (not null)</returns>
		Uri ExposeArtifact(FileItem file, ArtifactType type);

		/// <summary>
		///		Exposes given folder (with all content) as build artifact of specified type and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <param name="folder">folder to be exposed</param>
		/// <param name="type"><see cref="ArtifactType"/></param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,AnFake.Core.ArtifactType)"/>
		Uri ExposeArtifact(FolderItem folder, ArtifactType type);

		/// <summary>
		///		Exposes given text content as build artifact of specified type with givent name and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <param name="name">artifact name</param>
		/// <param name="content">text content to be exposed</param>
		/// <param name="encoding">content encoding</param>
		/// <param name="type"><see cref="ArtifactType"/></param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,AnFake.Core.ArtifactType)"/>
		Uri ExposeArtifact(string name, string content, Encoding encoding, ArtifactType type);

		/// <summary>
		///		Exposes files as build artifact of specified type.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <param name="files">files to be exposed</param>
		/// <param name="type"><see cref="ArtifactType"/></param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,AnFake.Core.ArtifactType)"/>
		void ExposeArtifacts(FileSet files, ArtifactType type);

		//
		// Experimental Stuff
		//

		/// <summary>
		///		Is in pipeline mode?
		/// </summary>
		bool HasPipeIn { get; }

		int PipeInChangesetId { get; }

		void GetPipeInArtifact(FileItem file, FileSystemPath dstPath, ArtifactType type);

		void GetPipeInArtifact(FolderItem folder, FileSystemPath dstPath, ArtifactType type);

		void GetPipeInArtifacts(FileSet files, FileSystemPath dstPath, ArtifactType type);
	}	
}